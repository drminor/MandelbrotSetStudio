using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MapSectionProviderLib.Support
{
    internal class JobsProducerConsumerQueue<T> : IDisposable where T : IWorkRequest
	{
        private readonly JobQueues<T> _imp;
        private readonly BlockingCollection<T> _bc;

        public JobsProducerConsumerQueue(JobQueues<T> imp, int boundedCapacity)
        {
            _imp = imp;
            _bc = new BlockingCollection<T>(_imp, boundedCapacity);
        }

        #region Public Properties

        public bool IsAddingCompleted => _bc.IsAddingCompleted;

        public bool IsCompleted => _bc.IsCompleted;

        public int Count => _bc.Count;

		#endregion

		//#region Public Methods - Extended

		//public void Add(int jobNumber, List<T> items)
		//{
  //          CreateJobListIfNew(jobNumber);

  //          foreach(var item in items)
  //          {
		//		_bc.Add(item);
		//	}
		//}

		//#endregion

		#region Public Methods - Standard Blocking Collection

		public void Add(T item)
        {
            //CreateJobListIfNew(item.JobId);
			_bc.Add(item);
        }

		public void Add(T item, CancellationToken ct)
		{
			_bc.Add(item, ct);
		}

        /// <summary>
        /// Return all work items belonging to a cancelled job
        /// and return the first work item belonging to a job not cancelled if one exists.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
		public List<T> Take(CancellationToken ct)
        {
            var result = new List<T>();

            var workItem = _bc.Take(ct);

            result.Add(workItem);

            if (workItem.JobIsCancelled)
            {
                while (_bc.TryTake(out workItem))
                {
                    result.Add(workItem);
                    if (!workItem.JobIsCancelled)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public void CompleteAdding() => _bc.CompleteAdding();

		#endregion

		//#region Private Methods

  //      private void CreateJobListIfNew(int jobNumber)
  //      {
  //          if (!_imp.JobNumbers.Contains(jobNumber))
  //          {
  //              _imp.JobNumbers.Add(jobNumber);
  //              _imp.JobLists.Add(new KeyValuePair<int, List<T>>(jobNumber, new List<T>()));
  //          }
  //      }

		//#endregion


		#region IDisposable Support

		public void Dispose()
        {
            ((IDisposable)_bc).Dispose();
        }

        #endregion
    }
}
