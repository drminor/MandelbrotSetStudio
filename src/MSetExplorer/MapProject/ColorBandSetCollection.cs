using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSetExplorer
{
	public class ColorBandSetCollection : IDisposable
	{
		private readonly Collection<ColorBandSet> _colorsCollection;
		private readonly ReaderWriterLockSlim _colorsLock;

		private int _colorsPointer;

		#region Constructor

		public ColorBandSetCollection()
		{
			_colorsCollection = new Collection<ColorBandSet>() { new ColorBandSet() };
			_colorsLock = new ReaderWriterLockSlim();
			_colorsPointer = 0;
		}

		#endregion

		#region Public Properties

		public ColorBandSet CurrentColorBandSet => DoWithReadLock(() => { return _colorsCollection[_colorsPointer]; });
		public bool CanGoBack => !(CurrentColorBandSet?.ParentId is null);
		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextCBSInStack(_colorsPointer, out var _); });

		public int CurrentIndex => DoWithReadLock(() =>  { return _colorsPointer; });

		public int Count => DoWithReadLock(() => { return _colorsCollection.Count; });

		//public IEnumerable<ColorBandSet> ColorBandSets => DoWithReadLock(() => { return new ReadOnlyCollection<ColorBandSet>(_colorsCollection); });

		//public bool IsDirty => _colorsCollection.Any(x => !x.OnFile);

		#endregion

		#region Public Methods


		public ColorBandSet this[int index]
		{
			get => DoWithReadLock(() => { return _colorsCollection[index]; });
			set => DoWithWriteLock(() => { _colorsCollection[index] = value; });
		}

		public void UpdateItem(int index, ColorBandSet colorBandSet)
		{
			DoWithWriteLock(() => { _colorsCollection[index] = colorBandSet; });
		}

		public bool MoveCurrentTo(ObjectId colorBandSetId)
		{
			_colorsLock.EnterUpgradeableReadLock();

			try
			{
				if (TryGetIndexFromId(colorBandSetId, out var index))
				{ 
					DoWithWriteLock(() => { _colorsPointer = index; });
					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_colorsLock.ExitUpgradeableReadLock();
			}
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

			Load(colorBandSets, null);
		}
		
		public bool Load(IEnumerable<ColorBandSet> colorBandSets, ObjectId? currentId)
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

				if (currentId.HasValue)
				{
					if (TryGetIndexFromId(currentId.Value, out var idx)) 
					{
						_colorsPointer = idx;
					}
					else
					{
						Debug.WriteLine($"WARNING: There is no ColorBandSet with Id: {currentId} in the list of ColorBandSets being loaded into the ColorBandSetCollection.");
						_colorsPointer = _colorsCollection.Count - 1;
						result = false;
					}
				}
				else
				{
					_colorsPointer = _colorsCollection.Count - 1;
				}
			});

			if (!CheckJobStackIntegrity())
			{
				Debug.WriteLine("The ColorBandSet Collection is not integral.");
			}

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

		public void Clear()
		{
			DoWithWriteLock(() =>
			{
				_colorsCollection.Clear();
				_colorsCollection.Add(new ColorBandSet());
				_colorsPointer = 0;
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

		public bool GoBack()
		{
			_colorsLock.EnterUpgradeableReadLock();
			try
			{
				var parentCbsId = CurrentColorBandSet?.ParentId;

				if (!(parentCbsId is null))
				{
					if (TryGetIndexFromId(parentCbsId.Value, out var cbsIndex))
					{
						DoWithWriteLock(() => UpdateColorsPtr(cbsIndex));
						return true;
					}
				}

				return false;
			}
			finally
			{
				_colorsLock.ExitUpgradeableReadLock();
			}
		}

		public bool GoForward()
		{
			_colorsLock.EnterUpgradeableReadLock();
			try
			{
				if (TryGetNextCBSInStack(_colorsPointer, out var nextCbsIndex))
				{
					DoWithWriteLock(() => UpdateColorsPtr(nextCbsIndex));
					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_colorsLock.ExitUpgradeableReadLock();
			}
		}

		#endregion

		#region Private Methods

		private void UpdateColorsPtr(int newCbsIndex)
		{
			if (newCbsIndex < 0 || newCbsIndex > _colorsCollection.Count - 1)
			{
				throw new ArgumentException($"The newCbsIndex with value: {newCbsIndex} is not valid.", nameof(newCbsIndex));
			}

			_colorsPointer = newCbsIndex;
		}

		#endregion

		#region Collection Management 

		private bool TryGetNextCBSInStack(int cbsIndex, out int nextCbsIndex)
		{
			if (TryGetCbsFromStack(cbsIndex, out var colorBandSet))
			{
				if (TryGetLatestChildCbsIndex(colorBandSet, out var childCbsIndex))
				{
					nextCbsIndex = childCbsIndex;
					return true;
				}
			}

			nextCbsIndex = -1;
			return false;
		}

		private bool TryGetCbsFromStack(int cbsIndex, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			if (cbsIndex < 0 || cbsIndex > _colorsCollection.Count - 1)
			{
				colorBandSet = null;
				return false;
			}
			else
			{
				colorBandSet = _colorsCollection[cbsIndex];
				return true;
			}
		}

		/// <summary>
		/// Finds the most recently ran child colorBandSet of the given parentJob.
		/// </summary>
		/// <param name="parentCbs"></param>
		/// <param name="childCbsIndex">If successful, the index of the most recent child colorBandSet of the given parentJob</param>
		/// <returns>True if there is any child of the specified colorBandSet.</returns>
		private bool TryGetLatestChildCbsIndex(ColorBandSet parentCbs, out int childCbsIndex)
		{
			childCbsIndex = -1;
			var lastestDtFound = DateTime.MinValue;

			for (var i = 0; i < _colorsCollection.Count; i++)
			{
				var colorBandSet = _colorsCollection[i];
				var thisParentCbsId = colorBandSet.ParentId ?? ObjectId.Empty;

				if (thisParentCbsId.Equals(parentCbs.Id))
				{
					var dt = colorBandSet.DateCreated;
					if (dt > lastestDtFound)
					{
						childCbsIndex = i;
						lastestDtFound = dt;
					}
				}
			}

			var result = childCbsIndex != -1;
			return result;
		}

		public bool Contains(ObjectId id)
		{
			var result = _colorsCollection.Any(x => x.Id == id);
			return result;
		}

		private bool TryGetIndexFromId(ObjectId id, out int index)
		{
			var colorBandSet = _colorsCollection.FirstOrDefault(x => x.Id == id);
			if (colorBandSet != null)
			{
				index = _colorsCollection.IndexOf(colorBandSet);
			}
			else
			{
				index = -1;
			}

			return colorBandSet != null;
		}

		private bool CheckJobStackIntegrity()
		{
			var result = DoWithReadLock(() => {
				foreach (var cbs in _colorsCollection)
				{
					if (cbs.ParentId.HasValue && !Contains(cbs.ParentId.Value))
					{
						return false;
					}
				}

				return true;
			});

			return result;
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
