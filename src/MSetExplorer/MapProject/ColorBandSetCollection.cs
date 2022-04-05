using MongoDB.Bson;
using MSetRepo;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSetExplorer
{
	public class ColorBandSetCollection : IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly Collection<ColorBandSet> _colorsCollection;
		private readonly ReaderWriterLockSlim _colorsLock;

		private int _colorsPointer;

		#region Constructor

		public ColorBandSetCollection(ProjectAdapter projectAdapter)
		{
			_projectAdapter = projectAdapter;
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

		//public IEnumerable<ColorBandSet> ColorBandSets => DoWithReadLock(() => { return new ReadOnlyCollection<ColorBandSet>(_colorsCollection); });

		public bool IsDirty => _colorsCollection.Any(x => !x.OnFile);

		#endregion

		#region Public Methods
		
		public void UpdateItem(int index, ColorBandSet job)
		{
			DoWithWriteLock(() => { _colorsCollection[index] = job; });
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
		
		public void Load(IEnumerable<ColorBandSet> colorBandSets, ObjectId? currentId)
		{
			DoWithWriteLock(() =>
			{
				_colorsCollection.Clear();

				foreach(var cbs in colorBandSets)
				{
					_colorsCollection.Add(cbs);
				}

				if (currentId.HasValue)
				{
					var cbs = _colorsCollection.FirstOrDefault(x => x.Id == currentId.Value);
					if (cbs != null)
					{
						var idx = _colorsCollection.IndexOf(cbs);
						_colorsPointer = idx;
					}
					else
					{
						_colorsPointer = _colorsCollection.Count - 1;
					}
				}
				else
				{
					_colorsPointer = _colorsCollection.Count - 1;
				}
			});
		}

		public void Push(ColorBandSet colorBandSet)
		{
			DoWithWriteLock(() =>
			{
				_colorsCollection.Add(colorBandSet);
				_colorsPointer = _colorsCollection.Count - 1;
			});
		}

		public void Save(ObjectId projectId)
		{
			DoWithWriteLock(() =>
			{
				for (var i = 0; i < _colorsCollection.Count; i++)
				{
					var cbs = _colorsCollection[i];
					if (!cbs.OnFile)
					{
						cbs.ProjectId = projectId;
						var updatedCbs = _projectAdapter.CreateColorBandSet(cbs);
						_colorsCollection[i] = updatedCbs;
						UpdateColorBandSet(cbs, updatedCbs);
					}
				}
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

		public bool GoBack()
		{
			_colorsLock.EnterUpgradeableReadLock();
			try
			{
				var parentCbsId = CurrentColorBandSet?.ParentId;

				if (!(parentCbsId is null))
				{
					if (TryFindByCbsId(parentCbsId.Value, out var colorBandSet))
					{
						var cbsIndex = _colorsCollection.IndexOf(colorBandSet);
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

		private void UpdateColorBandSet(ColorBandSet oldCbs, ColorBandSet newCbs)
		{
			foreach (var cbs in _colorsCollection)
			{
				if (cbs?.ParentId == oldCbs.Id)
				{
					cbs.ParentId = newCbs.Id;
					_projectAdapter.UpdateColorBandSetParentId(cbs.Id, cbs.ParentId);
				}
			}
		}

		#endregion

		#region Job Collection Management 

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
					var dt = thisParentCbsId.CreationTime;
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

		private bool TryFindByCbsId(ObjectId id, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			colorBandSet = _colorsCollection.FirstOrDefault(x => x.Id == id);
			return colorBandSet != null;
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
