using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MapSectionProviderLib.Support
{
    internal class MapSectionProducerConsumerQueueCmd<T> : IDisposable
    {
        private readonly BlockingCollection<KeyValuePair<int, T>> _bc;

        public MapSectionProducerConsumerQueueCmd(IProducerConsumerCollection<KeyValuePair<int, T>> imp, int boundedCapacity)
        {
            _bc = new BlockingCollection<KeyValuePair<int, T>>(imp, boundedCapacity);
        }

        #region Public Properties

        public bool IsAddingCompleted => _bc.IsAddingCompleted;

        public bool IsCompleted => _bc.IsCompleted;

        public int Count => _bc.Count;

        #endregion

        #region Public Methods - Standard Blocking Collection

        public void Add(T item)
        {
            var itemWrapper = new KeyValuePair<int, T>(0, item);
            _bc.Add(itemWrapper);
        }

		public void Add(T item, CancellationToken ct)
		{
			var itemWrapper = new KeyValuePair<int, T>(0, item);
			_bc.Add(itemWrapper, ct);
		}

		public T Take(CancellationToken ct)
        {
            var item = _bc.Take(ct);

            return item.Value;
        }

        public void CompleteAdding() => _bc.CompleteAdding();

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            ((IDisposable)_bc).Dispose();
        }

        #endregion
    }
}
