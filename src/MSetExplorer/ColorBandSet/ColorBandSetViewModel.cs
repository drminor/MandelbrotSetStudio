using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetViewModel : INotifyPropertyChanged
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private Project? _currentProject;
		private ColorBandSet? _colorBandSet;
		private ListCollectionView? _colorBandsView;

		private bool _isDirty;

		private readonly object _histLock;

		#region Constructor

		public ColorBandSetViewModel(ObservableCollection<MapSection> mapSections)
		{
			_mapSections = mapSections;
			_synchronizationContext = SynchronizationContext.Current;
			Histogram = new HistogramA(0);
			_mapSectionHistogramProcessor = new MapSectionHistogramProcessor(Histogram);

			_rowHeight = 60;
			_itemWidth = 180;
			CurrentProject = null;
			_colorBandSet = new ColorBandSet();
			_colorBandsView = null;
			_isDirty = false;
			_histLock = new object();

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public double RowHeight
		{
			get => _rowHeight;
			set { _rowHeight = value; OnPropertyChanged(nameof(RowHeight)); }
		}

		public double ItemWidth
		{
			get => _itemWidth;
			set { _itemWidth = value; OnPropertyChanged(nameof(ItemWidth)); }
		}

		public Project? CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;

					// Clone this to keep changes made here from updating the Project's copy.
					if (value != null)
					{
						ColorBandSet = value.CurrentColorBandSet?.Clone();
					}

					OnPropertyChanged(nameof(CurrentProject));
				}
			}
		}

		public ListCollectionView ColorBandsView
		{
			get
			{
				if (_colorBandsView == null)
				{
					_colorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet?.ColorBands);
					_colorBandsView.SortDescriptions.Add(new SortDescription("CutOff", ListSortDirection.Ascending));
				}

				return _colorBandsView;
			}

			set
			{
				_colorBandsView = value;
				OnPropertyChanged();
			}
		}

		public ColorBandSet? ColorBandSet
		{
			get => _colorBandSet;

			private set
			{
				//Debug.WriteLine($"ColorBandViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.SerialNumber}, New = {value?.SerialNumber}");
				if (value == null)
				{
					if (_colorBandSet != null)
					{
						Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");

						lock (_histLock)
						{
							_mapSectionHistogramProcessor.ProcessingEnabled = false;
							_colorBandSet = value;
							Histogram.Reset();
						}

						_colorBandsView = null;
						IsDirty = false;

						OnPropertyChanged(nameof(ColorBandSet));
						OnPropertyChanged(nameof(ColorBandsView));
					}
				}
				else
				{
					if (_colorBandSet == null || _colorBandSet != value)
					{
						var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
						Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}. The new ColorBandSet has SerialNumber: {value.SerialNumber}.");

						lock (_histLock)
						{
							_mapSectionHistogramProcessor.ProcessingEnabled = false;
							_colorBandSet = value;

							Histogram.Reset(value.HighCutOff + 1);
							PopulateHistorgram(_mapSections, Histogram);
							_mapSectionHistogramProcessor.ProcessingEnabled = true;
						}

						_colorBandsView = null;
						var view = ColorBandsView;
						_ = view.MoveCurrentTo(_colorBandSet.FirstOrDefault());

						IsDirty = false;
						OnPropertyChanged(nameof(ColorBandSet));
						OnPropertyChanged(nameof(ColorBandsView));
					}
				}
			}
		}

		public int? HighCutOff
		{
			get => _colorBandSet?.HighCutOff;
			set
			{
				if (value.HasValue)
				{
					if (_colorBandSet != null)
					{
						lock (_histLock)
						{
							Histogram.Reset(value.Value + 1);
						}

						_colorBandSet.HighCutOff = value.Value;
						OnPropertyChanged(nameof(HighCutOff));
					}
				}
			}
		}

		public IHistogram Histogram { get; private set; }

		public bool IsDirty
		{
			get => _isDirty;

			private set
			{
				if (value != _isDirty)
				{
					_isDirty = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Event Handlers

		private void MapSections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_colorBandSet != null && _colorBandSet.Count == 0)
			{
				return;
			}

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				Histogram.Reset();
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items
				var cutOffs = GetCutOffs();
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.Add, cutOffs, mapSection.Histogram, HandleHistogramUpdate));
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var cutOffs = GetCutOffs();
				var mapSections = e.OldItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.Remove, cutOffs, mapSection.Histogram, HandleHistogramUpdate));
				}
			}

			//Debug.WriteLine($"There are {Histogram[Histogram.UpperBound - 1]} points that reached the target iterations.");
		}

		private int[] GetCutOffs()
		{
			IEnumerable<int>? t;

			lock (_histLock)
			{
				t = _colorBandSet?.Select(x => x.CutOff);
			}

			return t?.ToArray() ?? Array.Empty<int>();
		}

		#endregion

		#region Public Methods

		public void InsertItem(int index, ColorBand newItem)
		{
			//Debug.WriteLine($"At InsertItem, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			_colorBandSet?.Insert(index, newItem);
			var cutOffs = GetCutOffs();
			_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutOffs, null, HandleHistogramUpdate));

			IsDirty = true;
		}

		public void ItemWasUpdated()
		{
			var cutOffs = GetCutOffs();
			_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutOffs, null, HandleHistogramUpdate));

			IsDirty = true;
		}

		public void DeleteSelectedItem()
		{
			var view = ColorBandsView;

			//Debug.WriteLine($"Getting ready to remove an item. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			if (view != null && _colorBandSet != null)
			{
				if (view.CurrentItem is ColorBand curItem)
				{
					var idx = _colorBandSet.IndexOf(curItem);

					if (idx >= _colorBandSet.Count - 1)
					{
						idx = _colorBandSet.Count - 1;
					}

					if (!view.MoveCurrentToPosition(idx))
					{
						Debug.WriteLine("Could not position view to next item.");
					}

					if (!_colorBandSet.Remove(curItem))
					{
						Debug.WriteLine("Could not remove the item.");
					}

					var idx1 = _colorBandSet.ColorBands.IndexOf((ColorBand)view.CurrentItem);

					Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {idx1}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

					var cutOffs = GetCutOffs();
					_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutOffs, null, HandleHistogramUpdate));

					IsDirty = true;
				}
			}
		}

		public void ApplyChanges()
		{
			if (ColorBandSet != null)
			{
				//Debug.WriteLine($"Applying changes, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

				// Create a new copy with a new serial number to load a new ColorMap.
				var newSet = ColorBandSet.CreateNewCopy();
				//CheckThatColorBandsWereUpdatedProperly(_colorBandSet, newSet, throwOnMismatch: false);

				Debug.WriteLine($"The ColorBandSetViewModel is Applying changes. The new SerialNumber is {newSet.SerialNumber}, name: {newSet.Name}, version: {newSet.VersionNumber}.");

				ColorBandSet = newSet;
			}
		}

		#endregion

		#region Private Methods

		private void PopulateHistorgram(IEnumerable<MapSection> mapSections, IHistogram histogram)
		{
			foreach(var ms in mapSections)
			{
				histogram.Add(ms.Histogram);
			}

			var cutOffs = GetCutOffs();
			_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutOffs, null, HandleHistogramUpdate));
		}

		private void HandleHistogramUpdate(ValueTuple<int, double>[] newPercentages)
		{
			_synchronizationContext?.Post(o => HistogramChanged(o), newPercentages);
		}

		private void HistogramChanged(object? hwr)
		{
			if (hwr is ValueTuple<int, double>[] newPercentages)
			{
				lock (_histLock)
				{
					var colorBands = _colorBandSet?.ColorBands.ToArray() ?? Array.Empty<ColorBand>();

					var len = Math.Min(newPercentages.Length, colorBands.Length);

					var total = 0d;

					for(var i = 0; i < len; i++)
					{
						var cb = colorBands[i];
						cb.Percentage = newPercentages[i].Item2;
						total += newPercentages[i].Item2;
					}

					Debug.WriteLine($"CBS received new percentages top: {newPercentages[^1]}, total: {total}.");
				}
			}
		}

		private string GetViewAsString()
		{
			var view = ColorBandsView;
			var colorBands = view.SourceCollection.Cast<ColorBand>().ToList();
			var result = GetString(colorBands);

			return result;
		}

		private string GetModelAsString()
		{
			var result = GetString(_colorBandSet?.ColorBands);

			return result;
		}

		private string GetString(ICollection<ColorBand>? colorBands)
		{
			if (colorBands != null)
			{
				var sb = new StringBuilder();

				foreach (var cb in colorBands)
				{
					_ = sb.AppendLine(cb.ToString());
				}

				return sb.ToString();
			}
			else
			{
				return "Empty";
			}
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
