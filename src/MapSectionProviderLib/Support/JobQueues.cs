using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MapSectionProviderLib.Support
{
	internal class JobQueues<T> : IProducerConsumerCollection<T> where T : IWorkRequest
	{
		#region Private Members

		private int _count;

		private List<int> _jobNumbers;
		private List<KeyValuePair<int, List<T>>> _jobLists;
		private int _currentJobIndex;
		private bool _currentJobIsCancelled;

		private int? _lastJobNumberForAddition;
		private List<T>? _lastListForAdd;

		private object _stateLock;

		#endregion

		#region Constructor

		public JobQueues()
		{
			_count = 0;

			_jobNumbers = new List<int>();
			_jobLists = new List<KeyValuePair<int, List<T>>>();

			_currentJobIndex = 0;
			_currentJobIsCancelled = false;

			_lastJobNumberForAddition = null;
			_lastListForAdd = null;

			_stateLock = new object();
		}

		#endregion

		#region Public Properties

		public int Count => _count;

		public bool IsSynchronized => throw new NotSupportedException();

		public object SyncRoot => throw new NotSupportedException();

		internal List<int> JobNumbers => _jobNumbers;
		internal List<KeyValuePair<int, List<T>>> JobLists => _jobLists;

		#endregion

		#region Public Methods

		public bool TryAdd(T request)
		{
			var jobNumber = request.JobId;

			lock (_stateLock)
			{
				var requestsForJob = GetListForAddition(jobNumber);
				requestsForJob.Add(request);

				System.Threading.Interlocked.Increment(ref _count);

				return true;
			}
		}

		public bool TryTake([MaybeNullWhen(false)] out T request)
		{
			lock (_stateLock)
			{
				if (_count == 0)
				{
					request = default;
					return false;
				}

				_currentJobIndex = GetNextJobListIndex(_currentJobIndex, _currentJobIsCancelled, _jobNumbers);

				var kvp = _jobLists[_currentJobIndex];
				var jobNumber = kvp.Key;
				var requestsForJob = kvp.Value;

				Debug.Assert(jobNumber == _jobNumbers[_currentJobIndex], "The JobNumber Key is not the same as the JobNumber from the currentJobIndex of the JobNumbers Array."); 

				if (requestsForJob.Count > 0)
				{
					request = requestsForJob[0];
					_currentJobIsCancelled = request.JobIsCancelled;

					requestsForJob.RemoveAt(0);
					System.Threading.Interlocked.Decrement(ref _count);

					if (requestsForJob.Count == 0)
					{
						_jobLists.RemoveAt(_currentJobIndex);
						_jobNumbers.RemoveAt(_currentJobIndex);

						if (_lastListForAdd == requestsForJob)
						{
							_lastJobNumberForAddition = null;
							_lastListForAdd = null;
						}

						_currentJobIsCancelled = false;
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

		private int GetNextJobListIndex(int currentIndex, bool currentJobIsCancelled, List<int> jobNumbers)
		{
			if (!currentJobIsCancelled)
			{
				currentIndex++;
			}

			if (currentIndex > jobNumbers.Count - 1)
			{
				currentIndex = 0;
			}

			return currentIndex;
		}

		private List<T> GetListForAddition(int jobNumber)
		{
			lock (_stateLock)
			{
				if (jobNumber != _lastJobNumberForAddition)
				{
					if (!_jobNumbers.Contains(jobNumber))
					{
						_jobNumbers.Add(jobNumber);

						_lastListForAdd = new List<T>();
						_jobLists.Add(new KeyValuePair<int, List<T>>(jobNumber, _lastListForAdd));
					}
					else
					{
						var kvp = _jobLists.Find(x => x.Key == jobNumber);
						_lastListForAdd = kvp.Value;
					}

					_lastJobNumberForAddition = jobNumber;
				}

				return _lastListForAdd!;
			}
		}

		#endregion

		#region ICollection Support

		// Required for ICollection
		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo(array as T[], index);
		}

		// CopyTo is problematic in a producer-consumer.
		// The destination array might be shorter or longer than what
		// we get from ToArray due to adds or takes after the destination array was allocated.
		// Therefore, all we try to do here is fill up destination with as much
		// data as we have without running off the end.
		public void CopyTo(T[]? destination, int destStartingIndex)
		{
			if (destination == null) throw new ArgumentNullException();
			if (destStartingIndex < 0) throw new ArgumentOutOfRangeException();

			var temp = ToArray();

			for (int i = 0; i < destination.Length && i < temp.Length; i++)
			{
				destination[i] = temp[i];
			}
		}

		public T[] ToArray()
		{
			T[] result;

			lock (_stateLock)
			{
				result = new T[Count];
				int index = 0;

				foreach (var kvp in _jobLists)
				{
					var requestsForJob = kvp.Value;

					if (requestsForJob.Count > 0)
					{
						requestsForJob.CopyTo(result, index);
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

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < 1; i++)
			{
				foreach (var kvp in _jobLists)
				{
					var requestsForJob = kvp.Value;
					foreach (var item in requestsForJob)
					{
						yield return item;
					}
				}
			}
		}

		#endregion

	}
}
