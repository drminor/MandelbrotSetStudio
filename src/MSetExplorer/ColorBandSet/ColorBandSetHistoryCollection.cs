using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace MSetExplorer
{
	public class ColorBandSetHistoryCollection : IDisposable
	{
		private readonly Collection<ColorBandSet> _colorsCollection;
		private readonly ReaderWriterLockSlim _colorsLock;

		private int _colorsPointer;

		#region Constructor

		public ColorBandSetHistoryCollection(IEnumerable<ColorBandSet> colorBandSets)
		{
			_colorsCollection = new Collection<ColorBandSet>(colorBandSets.ToList());
			_colorsPointer = _colorsCollection.Count - 1;

			_colorsLock = new ReaderWriterLockSlim();
		}

		#endregion

		#region Public Properties

		public ColorBandSet CurrentColorBandSet => DoWithReadLock(() => { return _colorsCollection[_colorsPointer]; });

		public int CurrentIndex => DoWithReadLock(() =>  { return _colorsPointer; });

		public int Count => DoWithReadLock(() => { return _colorsCollection.Count; });

		//public IEnumerable<ColorBandSet> GetColorBandSets() => DoWithReadLock(() => { return new ReadOnlyCollection<ColorBandSet>(_colorsCollection); });

		#endregion

		#region Public Methods

		public ColorBandSet this[int index]
		{
			get => DoWithReadLock(() => { return _colorsCollection[index]; });
			set => DoWithWriteLock(() => { _colorsCollection[index] = value; });
		}

		public bool MoveCurrentTo(int index)
		{
			_colorsLock.EnterUpgradeableReadLock();

			try
			{
				if (index < 0 || index > _colorsCollection.Count - 1)
				{
					return false;
				}
				else
				{
					DoWithWriteLock(() => { _colorsPointer = index; });
					return true;
				}
			}
			finally
			{
				_colorsLock.ExitUpgradeableReadLock();
			}
		}

		public void Load(ColorBandSet? colorBandSet)
		{
			var colorBandSets = new List<ColorBandSet>();

			if (colorBandSet != null)
			{
				colorBandSets.Add(colorBandSet);
			}

			Load(colorBandSets);
		}
		
		public bool Load(IEnumerable<ColorBandSet> colorBandSets)
		{
			var result = true;

			DoWithWriteLock(() =>
			{
				_colorsCollection.Clear();

				foreach(var cbs in colorBandSets)
				{
					_colorsCollection.Add(cbs);
				}

				if (_colorsCollection.Count == 0)
				{
					_colorsCollection.Add(new ColorBandSet());
				}

				_colorsPointer = _colorsCollection.Count - 1;
			});

			return result;
		}

		public void Push(ColorBandSet colorBandSet)
		{
			DoWithWriteLock(() =>
			{
				_colorsCollection.Add(colorBandSet);
				_colorsPointer = _colorsCollection.Count - 1;
			});
		}

		public void Trim(int index) 
		{
			_colorsLock.EnterUpgradeableReadLock();

			try
			{
				if (index < 0 || index > _colorsCollection.Count - 2)
				{
					return;
				}
				else
				{
					DoWithWriteLock(() =>
					{
						var indexOfLastItem = _colorsCollection.Count - 1;
						while(index < _colorsCollection.Count - 1)
						{
							_colorsCollection.RemoveAt(indexOfLastItem);
							indexOfLastItem = _colorsCollection.Count - 1;
						}

						_colorsPointer = _colorsCollection.Count - 1;
					});
				}
			}
			finally
			{
				_colorsLock.ExitUpgradeableReadLock();
			}
		}

		#endregion

		#region Lock Helpers

		private T DoWithReadLock<T>(Func<T> function)
		{
			_colorsLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_colorsLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_colorsLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_colorsLock.ExitWriteLock();
			}
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			((IDisposable)_colorsLock).Dispose();
		}

		#endregion
	}
}
