using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSS.Types.MSet
{
	public class MapSectionCollection : Collection<MapSection>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		//public event NotifyCollectionChangedEventHandler? CollectionChanged;

		//public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
		/// </summary>
		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
		{
			add => PropertyChanged += value;
			remove => PropertyChanged -= value;
		}

		/// <summary>
		/// Occurs when the collection changes, either by adding or removing an item.
		/// </summary>
		/// <remarks>
		/// see <seealso cref="INotifyCollectionChanged"/>
		/// </remarks>
		[field: NonSerialized]
		public virtual event NotifyCollectionChangedEventHandler? CollectionChanged;

		/// <summary>
		/// Called by base class Collection&lt;T&gt; when the list is being cleared;
		/// raises a CollectionChanged event to any listeners.
		/// </summary>
		protected override void ClearItems()
		{
			//CheckReentrancy();
			base.ClearItems();
			OnCountPropertyChanged();
			OnIndexerPropertyChanged();
			OnCollectionReset();
		}

		/// <summary>
		/// Called by base class Collection&lt;T&gt; when an item is removed from list;
		/// raises a CollectionChanged event to any listeners.
		/// </summary>
		protected override void RemoveItem(int index)
		{
			//CheckReentrancy();
			MapSection removedItem = this[index];

			base.RemoveItem(index);

			OnCountPropertyChanged();
			OnIndexerPropertyChanged();
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
		}

		/// <summary>
		/// Called by base class Collection&lt;T&gt; when an item is added to list;
		/// raises a CollectionChanged event to any listeners.
		/// </summary>
		protected override void InsertItem(int index, MapSection item)
		{
			//CheckReentrancy();
			base.InsertItem(index, item);

			OnCountPropertyChanged();
			OnIndexerPropertyChanged();
			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		}

		/// <summary>
		/// Called by base class Collection&lt;T&gt; when an item is set in list;
		/// raises a CollectionChanged event to any listeners.
		/// </summary>
		protected override void SetItem(int index, MapSection item)
		{
			//CheckReentrancy();
			MapSection originalItem = this[index];
			base.SetItem(index, item);

			OnIndexerPropertyChanged();
			OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item, index);
		}

		/// <summary>
		/// Called by base class ObservableCollection&lt;T&gt; when an item is to be moved within the list;
		/// raises a CollectionChanged event to any listeners.
		/// </summary>
		protected virtual void MoveItem(int oldIndex, int newIndex)
		{
			//CheckReentrancy();

			MapSection removedItem = this[oldIndex];

			base.RemoveItem(oldIndex);
			base.InsertItem(newIndex, removedItem);

			OnIndexerPropertyChanged();
			OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
		}

		/// <summary>
		/// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
		/// </summary>
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			PropertyChanged?.Invoke(this, e);
		}

		/// <summary>
		/// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
		/// </summary>
		[field: NonSerialized]
		protected virtual event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raise CollectionChanged event to any listeners.
		/// Properties/methods modifying this ObservableCollection will raise
		/// a collection changed event through this virtual method.
		/// </summary>
		/// <remarks>
		/// When overriding this method, either call its base implementation
		/// or call <see cref="BlockReentrancy"/> to guard against reentrant collection changes.
		/// </remarks>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler? handler = CollectionChanged;
			if (handler != null)
			{
				// Not calling BlockReentrancy() here to avoid the SimpleMonitor allocation.
				//_blockReentrancyCount++;
				try
				{
					handler(this, e);
				}
				finally
				{
					//_blockReentrancyCount--;
				}
			}
		}

		/// <summary>
		/// Helper to raise CollectionChanged event to any listeners
		/// </summary>
		private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
		}

		/// <summary>
		/// Helper to raise CollectionChanged event to any listeners
		/// </summary>
		private void OnCollectionChanged(NotifyCollectionChangedAction action, object? oldItem, object? newItem, int index)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		/// <summary>
		/// Helper to raise a PropertyChanged event for the Count property
		/// </summary>
		private void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

		/// <summary>
		/// Helper to raise a PropertyChanged event for the Indexer property
		/// </summary>
		private void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

		/// <summary>
		/// Helper to raise CollectionChanged event with action == Reset to any listeners
		/// </summary>
		private void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
	}

	internal static class EventArgsCache
	{
		internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
		internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
		internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
	}
}
