using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MapSectionProviderLib.Support
{
	using kvpJobsListType = KeyValuePair<int, List<MapSectionGenerateRequest>>;
	using kvpResultType = KeyValuePair<int, MapSectionGenerateRequest>;
	using msrType = MapSectionGenerateRequest;

	// Implementation of a priority queue that has bounding and blocking functionality.
	internal class JobQueues : IProducerConsumerCollection<kvpResultType>
	{
		#region Private Members

		private int _count;

		private List<int> _jobNumbers;
		private List<kvpJobsListType> _jobLists;
		private int _currentJobIndex;

		private object _stateLock;

		#endregion

		#region Constructor

		public JobQueues()
		{
			_count = 0;

			_jobNumbers = new List<int>();
			_jobLists = new List<kvpJobsListType>();

			_currentJobIndex = 0;

			_stateLock = new object();
		}

		#endregion

		#region Public Properties

		public int Count => _count;

		public bool IsSynchronized => throw new NotSupportedException();

		public object SyncRoot => throw new NotSupportedException();

		#endregion

		#region Public Methods

		public bool TryAdd(kvpResultType item)
		{
			var queueCmd = item.Key;

			var jobNumber = item.Value.JobId;

			var request = item.Value;

			lock (_stateLock)
			{
				List<msrType> requestsForJob;

				if (!_jobNumbers.Contains(jobNumber))
				{
					_jobNumbers.Add(jobNumber);

					requestsForJob = new List<msrType>();
					_jobLists.Add(new kvpJobsListType(jobNumber, requestsForJob));
				}
				else
				{
					var kvp = _jobLists.Find(x => x.Key == jobNumber);
					requestsForJob = kvp.Value;
				}

				requestsForJob.Add(request);
				System.Threading.Interlocked.Increment(ref _count);

				return true;
			}
		}

		public bool TryTake(out kvpResultType item)
		{
			lock (_stateLock)
			{
				if (_count == 0)
				{
					item = default(kvpResultType);
					return false;
				}

				_currentJobIndex = GetNextJobListIndex(_currentJobIndex, _jobNumbers);

				var kvp = _jobLists[_currentJobIndex];
				var jobNumber = kvp.Key;
				var requestsForJob = kvp.Value;

				Debug.Assert(jobNumber == _jobNumbers[_currentJobIndex], "The JobNumber Key is not the same as the JobNumber from the currentJobIndex of the JobNumbers Array."); 

				if (requestsForJob.Count > 0)
				{
					msrType request = requestsForJob[0];
					requestsForJob.RemoveAt(0);
					System.Threading.Interlocked.Decrement(ref _count);

					item = new kvpResultType(jobNumber, request);

					if (requestsForJob.Count == 0)
					{
						_jobLists.RemoveAt(_currentJobIndex);
						_jobNumbers.RemoveAt(_currentJobIndex);
					}

					return true;
				}
				else
				{
					throw new InvalidOperationException("Found an empty job list.");
				}
			}
		}

		#endregion

		#region Private Methods

		private int GetNextJobListIndex(int currentIndex, List<int> jobNumbers)
		{
			currentIndex++;

			if (currentIndex > jobNumbers.Count - 1)
			{
				currentIndex = 0;
			}

			return currentIndex;
		}

		#endregion

		#region ICollection Support

		// Required for ICollection
		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo(array as kvpResultType[], index);
		}

		// CopyTo is problematic in a producer-consumer.
		// The destination array might be shorter or longer than what
		// we get from ToArray due to adds or takes after the destination array was allocated.
		// Therefore, all we try to do here is fill up destination with as much
		// data as we have without running off the end.
		public void CopyTo(kvpResultType[]? destination, int destStartingIndex)
		{
			if (destination == null) throw new ArgumentNullException();
			if (destStartingIndex < 0) throw new ArgumentOutOfRangeException();

			var temp = ToArray();

			for (int i = 0; i < destination.Length && i < temp.Length; i++)
			{
				destination[i] = temp[i];
			}
		}

		public kvpResultType[] ToArray()
		{
			kvpResultType[] result;

			lock (_stateLock)
			{
				result = new kvpResultType[Count];
				int index = 0;

				foreach (var kvp in _jobLists)
				{
					var requestsForJob = kvp.Value;

					if (requestsForJob.Count > 0)
					{
						var kvpsForJob = requestsForJob.Select((x, i) => new kvpResultType(i, x));
						kvpsForJob.ToList().CopyTo(result, index);
						index += requestsForJob.Count;
					}
				}
				return result;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<kvpResultType> GetEnumerator()
		{
			for (int i = 0; i < 1; i++)
			{
				foreach (var kvp in _jobLists)
				{
					var jobNumber = kvp.Key;
					var requestsForJob = kvp.Value;
					foreach (var item in requestsForJob)
					{
						yield return new kvpResultType(jobNumber, item);
					}
				}
			}
		}

		#endregion

	}
}
