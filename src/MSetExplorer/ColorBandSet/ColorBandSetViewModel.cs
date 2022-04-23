using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetViewModel : INotifyPropertyChanged, IDisposable, IUndoRedoViewModel
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private ColorBandSet? _colorBandSet;	// The value assigned to this model
		private bool _useEscapeVelocities;
		private bool _useRealTimePreview;

		private readonly ColorBandSetCollection _colorBandSetCollection;

		private ColorBandSet _currentColorBandSet; // => _colorBandSetCollection.CurrentColorBandSet; // The value which is currently being edited.
		//private ColorBandSet _currentBeforeEdits;

		private ListCollectionView _colorBandsView;
		private ColorBand? _currentColorBand;

		private bool _isDirty;

		private readonly object _histLock;

		private PercentageBand? _beyondTargetSpecs;

		#region Constructor

		public ColorBandSetViewModel(ObservableCollection<MapSection> mapSections)
		{
			_useEscapeVelocities = true;
			_mapSections = mapSections;
			_synchronizationContext = SynchronizationContext.Current;
			Histogram = new HistogramA(0);
			_mapSectionHistogramProcessor = new MapSectionHistogramProcessor(Histogram);

			_rowHeight = 60;
			_itemWidth = 180;

			_colorBandSet = null;

			_colorBandSetCollection = new ColorBandSetCollection();
			_currentColorBandSet = _colorBandSetCollection.CurrentColorBandSet.Clone();
			_colorBandsView = BuildColorBandsView(null);

			_isDirty = false;
			_histLock = new object();
			_beyondTargetSpecs = null;

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public event EventHandler<ColorBandSetUpdateRequestedEventArgs>? ColorBandSetUpdateRequested;

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

		public ColorBandSet? ColorBandSet
		{
			get => _colorBandSet;

			set
			{
				if (value != _colorBandSet)
				{
					UpdateColorBandSet(value);
				}
			}
		}

		private void UpdateColorBandSet(ColorBandSet? value)
		{
			//Debug.WriteLine($"ColorBandViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.Id}, New = {value?.Id}");
			if (value == null)
			{
				Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");
			}
			else
			{
				var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
				Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}. The new ColorBandSet has Id: {value.Id}.");
			}

			lock (_histLock)
			{
				_mapSectionHistogramProcessor.ProcessingEnabled = false;
				_colorBandSet = value;
				_colorBandSetCollection.Load(value?.Clone());

				if (value != null)
				{
					Histogram.Reset(value.HighCutOff);
					PopulateHistorgram(_mapSections, Histogram);
					_mapSectionHistogramProcessor.ProcessingEnabled = true;

					UpdatePercentages();
				}
				else
				{
					Histogram.Reset();
				}
			}

			IsDirty = false;
			BuildViewAndRaisePropertyChangeEvents();
		}

		public bool UseEscapeVelocities
		{
			get => _useEscapeVelocities;
			set
			{
				if (value != _useEscapeVelocities)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLine($"The ColorBandSetViewModel is turning {strState} the use of EscapeVelocities.");
					_useEscapeVelocities = value;
					OnPropertyChanged(nameof(UseEscapeVelocities));
				}
			}
		}

		public bool UseRealTimePreview
		{
			get => _useRealTimePreview;
			set
			{
				if (value != _useRealTimePreview)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLine($"The ColorBandSetViewModel is turning {strState} the use of RealTimePreview.");
					_useRealTimePreview = value;

					var colorBandSet = _useRealTimePreview ? _currentColorBandSet : _colorBandSetCollection[0];
					ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(colorBandSet, isPreview: true));

					OnPropertyChanged(nameof(UseRealTimePreview));
				}
			}
		}

		public ListCollectionView ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				if (_colorBandsView != null)
				{
					_colorBandsView.CurrentChanged -= ColorBandsView_CurrentChanged;
				}

				_colorBandsView = value;

				if (_colorBandsView != null)
				{
					_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
				}

				OnPropertyChanged();
			}
		}

		public ColorBand? CurrentColorBand
		{
			get => _currentColorBand;
			set
			{
				if (_currentColorBand != null)
				{
					_currentColorBand.PropertyChanged -= CurrentColorBand_PropertyChanged;
				}

				_currentColorBand = value;

				if (_currentColorBand != null)
				{
					_currentColorBand.PropertyChanged += CurrentColorBand_PropertyChanged;
				}

				OnPropertyChanged();
			}
		}

		public IHistogram Histogram { get; private set; }

		public PercentageBand? BeyondTargetSpecs => _beyondTargetSpecs;

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

		#region UndoRedoPile Properties / Methods

		public int CurrentIndex
		{
			get => _colorBandSetCollection.CurrentIndex;
			set	{ } 
		}

		public bool CanGoBack => _colorBandSetCollection.CurrentIndex > 0;
		public bool CanGoForward =>  _colorBandSetCollection.CurrentIndex < _colorBandSetCollection.Count - 1;

		public bool GoBack()
		{
			if (_colorBandSetCollection.MoveCurrentTo(_colorBandSetCollection.CurrentIndex - 1))
			{
				BuildViewAndRaisePropertyChangeEvents();
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool GoForward()
		{
			if (_colorBandSetCollection.MoveCurrentTo(_colorBandSetCollection.CurrentIndex + 1))
			{
				BuildViewAndRaisePropertyChangeEvents();
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Event Handlers

		private void CurrentColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb && _currentColorBandSet != null)
			{
				Debug.WriteLine($"Prop: {e.PropertyName} is changing.");

				var foundUpdate = false;

				if (e.PropertyName == nameof(ColorBand.StartColor))
				{
					if (TryGetPredeccessor(_currentColorBandSet, cb, out var colorBand))
					{
						colorBand.SuccessorStartColor = cb.StartColor;
					}

					foundUpdate = true;
				}
				else if (e.PropertyName == nameof(ColorBand.CutOff))
				{
					if (TryGetSuccessor(_currentColorBandSet, cb, out var colorBand))
					{
						colorBand.PreviousCutOff = cb.CutOff;
					}

					foundUpdate = true;
					UpdatePercentages();
				}
				else if (e.PropertyName == nameof(ColorBand.BlendStyle))
				{
					//cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? cb.SuccessorStartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;
					foundUpdate = true;
				}
				else
				{
					if (e.PropertyName == nameof(ColorBand.ActualEndColor))
					{
						foundUpdate = true;
					}
				}

				if (foundUpdate)
				{
					PushCopyOfCurrent();
					IsDirty = true;

					if (UseRealTimePreview)
					{
						ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
					}
				}
			}
			else
			{
				if (sender is ColorBand)
				{
					Debug.WriteLine($"The ColorBandSet is null while handling a CurrentColorBand_PropertyChanged event.");
				}
				else
				{
					Debug.WriteLine($"A sender of type {sender?.GetType()} is sending is raising the CurrentColorBand_PropertyChanged event.");
				}
			}
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			CurrentColorBand = (ColorBand)ColorBandsView.CurrentItem;
		}

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

		#endregion

		#region Public Methods

		public void InsertItem(int index, ColorBand newItem)
		{
			//Debug.WriteLine($"At InsertItem, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			lock (_histLock)
			{
				_currentColorBandSet.Insert(index, newItem);
			}

			PushCopyOfCurrent();
			IsDirty = true;

			UpdatePercentages();

			if (UseRealTimePreview)
			{
				ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
			}
		}

		public void DeleteSelectedItem()
		{
			var view = ColorBandsView;

			//Debug.WriteLine($"Getting ready to remove an item. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			if (view != null)
			{
				if (view.CurrentItem is ColorBand curItem)
				{
					var currentSet = _currentColorBandSet;
					var idx = currentSet.IndexOf(curItem);

					if (idx >= currentSet.Count - 1)
					{
						// Cannot delete the last entry
						return;
					}

					bool colorBandWasRemoved;
					lock (_histLock)
					{
						colorBandWasRemoved = currentSet.Remove(curItem);
					}

					PushCopyOfCurrent();

					if (!colorBandWasRemoved)
					{
						Debug.WriteLine("Could not remove the item.");
					}

					var idx1 = currentSet.IndexOf((ColorBand)view.CurrentItem);
					Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {idx1}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

					IsDirty = true;

					UpdatePercentages();

					if (UseRealTimePreview)
					{
						ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
					}
				}
			}
		}

		public void ApplyChanges(int? newTargetIterations = null)
		{
			// Create a new ColorBandSet with a new id, having the same values of this instance.
			var newSet = _currentColorBandSet.CreateNewCopy();

			if (newTargetIterations.HasValue)
			{
				newSet.HighCutOff = newTargetIterations.Value;
			}

			Debug.WriteLine($"The ColorBandSetViewModel is Applying changes. The new Id is {newSet.Id}, name: {newSet.Name}. The old Id is {ColorBandSet?.Id ?? ObjectId.Empty}");

			_colorBandSet = newSet;

			var curPos = ColorBandsView.CurrentPosition;
			_colorBandSetCollection.Load(newSet);

			BuildViewAndRaisePropertyChangeEvents(curPos);
			IsDirty = false;

			ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(newSet, isPreview: false));
		}

		public void RevertChanges()
		{
			_colorBandSetCollection.Trim(0);

			var curPos = ColorBandsView.CurrentPosition;
			BuildViewAndRaisePropertyChangeEvents(curPos);
			IsDirty = false;

			if (UseRealTimePreview)
			{
				ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
			}
		}

		#endregion

		#region Private Methods

		private void BuildViewAndRaisePropertyChangeEvents(int? selectedIndex = null)
		{
			_currentColorBandSet = _colorBandSetCollection.CurrentColorBandSet.Clone();

			ColorBandsView = BuildColorBandsView(_currentColorBandSet);

			_ = selectedIndex.HasValue ? ColorBandsView.MoveCurrentToPosition(selectedIndex.Value) : ColorBandsView.MoveCurrentToFirst();

			OnPropertyChanged(nameof(IUndoRedoViewModel.CurrentIndex));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoBack));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoForward));
		}

		private ListCollectionView BuildColorBandsView(ObservableCollection<ColorBand>? colorBands)
		{
			if (colorBands == null)
			{
				colorBands = new ObservableCollection<ColorBand>();
			}

			var result = (ListCollectionView)CollectionViewSource.GetDefaultView(colorBands);
			//result.SortDescriptions.Add(new SortDescription("CutOff", ListSortDirection.Ascending));

			return result;
		}

		private void PushCopyOfCurrent()
		{
			_colorBandSetCollection.Push(_currentColorBandSet);
			OnPropertyChanged(nameof(IUndoRedoViewModel.CurrentIndex));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoBack));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoForward));
		}

		private void UpdatePercentages()
		{
			var cutOffs = GetCutOffs();
			_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutOffs, null, HandleHistogramUpdate));
		}

		private void PopulateHistorgram(IEnumerable<MapSection> mapSections, IHistogram histogram)
		{
			foreach (var ms in mapSections)
			{
				histogram.Add(ms.Histogram);
			}

			//var cutOffs = GetCutOffs();
			//_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutOffs, null, HandleHistogramUpdate));
		}

		private void HandleHistogramUpdate(PercentageBand[] newPercentages)
		{
			_synchronizationContext?.Post(o => HistogramChanged(o), newPercentages);
		}

		private void HistogramChanged(object? hwr)
		{
			if (hwr is PercentageBand[] newPercentages)
			{
				lock (_histLock)
				{
					var colorBands = _currentColorBandSet.ToArray();

					var len = Math.Min(newPercentages.Length, colorBands.Length);

					//var total = 0d;

					var allMatched = true;
					for (var i = 0; i < len; i++)
					{
						if (colorBands[i].CutOff != newPercentages[i].CutOff)
						{
							allMatched = false;
							break;
						}
					}

					if (!allMatched)
					{
						return;
					}

					for (var i = 0; i < len; i++)
					{
						var cb = colorBands[i];
						cb.Percentage = newPercentages[i].Percentage;
						//total += newPercentages[i].Item2;
					}

					_beyondTargetSpecs = newPercentages[^1];

					//Debug.WriteLine($"CBS received new percentages top: {newPercentages[^1]}, total: {total}.");
				}
			}
		}

		private int[] GetCutOffs()
		{
			IEnumerable<int>? t;

			lock (_histLock)
			{
				t = _currentColorBandSet.Select(x => x.CutOff);
			}

			return t.ToArray();
		}

		private bool TryGetPredeccessor(IList<ColorBand> colorBands, ColorBand cb, [MaybeNullWhen(false)] out ColorBand colorBand)
		{
			colorBand = GetPredeccessor(colorBands, cb);
			return !(colorBand is null);
		}

		private ColorBand? GetPredeccessor(IList<ColorBand> colorBands, ColorBand cb)
		{
			var index = colorBands.IndexOf(cb);
			var result = index < 1 ? null : colorBands[index - 1];
			return result;
		}

		private bool TryGetSuccessor(IList<ColorBand> colorBands, ColorBand cb, [MaybeNullWhen(false)] out ColorBand colorBand)
		{
			colorBand = GetSuccessor(colorBands, cb);
			return !(colorBand is null);
		}

		private ColorBand? GetSuccessor(IList<ColorBand> colorBands, ColorBand cb)
		{
			var index = colorBands.IndexOf(cb);
			var result = index > colorBands.Count - 2 ? null : colorBands[index + 1];
			return result;
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

		#region IDisposable Support

		public void Dispose()
		{
			((IDisposable)_mapSectionHistogramProcessor).Dispose();
		}

		#endregion
	}
}
