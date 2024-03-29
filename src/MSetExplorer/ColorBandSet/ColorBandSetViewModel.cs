﻿using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
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
		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;
		private readonly HistogramD _topValues;
		private double _averageMapSectionTargetIteration;

		private double _rowHeight;
		private double _itemWidth;
		private ColorBandSetEditMode _editMode;

		private ColorBandSet? _colorBandSet;	// The value assigned to this model
		private bool _useEscapeVelocities;
		private bool _useRealTimePreview;
		private bool _highlightSelectedBand;

		private readonly ColorBandSetHistoryCollection _colorBandSetHistoryCollection;

		private ColorBandSet _currentColorBandSet; // The value which is currently being edited.

		private ListCollectionView _colorBandsView;
		private ColorBand? _currentColorBand;

		private bool _isDirty;
		private readonly object _histLock;

		private bool _useDetailedDebug = false;

		#region Constructor

		public ColorBandSetViewModel(ObservableCollection<MapSection> mapSections, IMapSectionHistogramProcessor mapSectionHistogramProcessor)
		{
			_useEscapeVelocities = true;
			_mapSections = mapSections;
			_synchronizationContext = SynchronizationContext.Current;
			//_histogram = histogram;
			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;
			_topValues = new HistogramD();

			_rowHeight = 60;
			_itemWidth = 180;
			_editMode = ColorBandSetEditMode.Bands;

			_colorBandSet = null;

			_colorBandSetHistoryCollection = new ColorBandSetHistoryCollection(new List<ColorBandSet> { new ColorBandSet() } );
			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.Clone();
			_colorBandsView = BuildColorBandsView(null);

			_isDirty = false;
			_histLock = new object();
			BeyondTargetSpecs = null;

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

		public ColorBandSetEditMode EditMode
		{
			get => _editMode;
			set
			{
				if (value != _editMode)
				{
					Debug.WriteLine($"ColorBandSetViewModel: The Edit mode is now {value}");
					_editMode = value;
					OnPropertyChanged();
				}
			}
		}

		public ColorBandSet? ColorBandSet
		{
			get => _colorBandSet;

			set
			{
				if (value != _colorBandSet)
				{
					UpdateColorBandSet(value);
					OnPropertyChanged();
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel's ColorBandSet property is being set to the same value already present. The new ColorBandSet has Id: {value?.Id ?? ObjectId.Empty}.");
				}
			}
		}

		private void UpdateColorBandSet(ColorBandSet? value)
		{
			//Debug.WriteLine($"ColorBandSetViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.Id}, New = {value?.Id}");
			if (value == null)
			{
				Debug.WriteLineIf(_useDetailedDebug, "ColorBandSetViewModel is clearing its collection. (non-null => null.)");
			}
			else
			{
				var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
				Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel is updating its collection. {upDesc}. The new ColorBandSet has Id: {value.Id}.");
			}

			lock (_histLock)
			{
				_topValues.Clear();
				_mapSectionHistogramProcessor.ProcessingEnabled = false;
				_colorBandSet = value;
				_colorBandSetHistoryCollection.Load(value?.CreateNewCopy());

				if (value != null)
				{
					//_histogram.Reset(value.HighCutoff);
					_mapSectionHistogramProcessor.Reset(value.HighCutoff); // TODO: Make Reset disable processing, ColorBandSetViewModel
					//PopulateHistorgram(_mapSections, _histogram);
					_mapSectionHistogramProcessor.LoadHistogram(_mapSections.Select(x => x.Histogram)); // TODO: Make LoadHistogram enable processing, ColorBandSetViewModel
					_mapSectionHistogramProcessor.ProcessingEnabled = true;

					UpdatePercentages();
				}
				else
				{
					//_histogram.Reset();
					_mapSectionHistogramProcessor.Reset();
					AverageMapSectionTargetIteration = 0;
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
					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetViewModel is turning {strState} the use of EscapeVelocities.");
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
					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetViewModel is turning {strState} the use of RealTimePreview. IsDirty = {IsDirty}.");
					_useRealTimePreview = value;

					if (IsDirty)
					{
						var colorBandSet = _useRealTimePreview ? _currentColorBandSet : _colorBandSetHistoryCollection[0];
						ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(colorBandSet, isPreview: true));
					}

					OnPropertyChanged(nameof(UseRealTimePreview));
				}
			}
		}

		public bool HighlightSelectedBand
		{
			get => _highlightSelectedBand;
			set
			{
				if (value != _highlightSelectedBand)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetViewModel is turning {strState} the High Lighting the Selected Color Band.");
					_highlightSelectedBand = value;

					OnPropertyChanged(nameof(HighlightSelectedBand));
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

		public PercentageBand? BeyondTargetSpecs { get; private set; }

		public double AverageMapSectionTargetIteration
		{
			get => _averageMapSectionTargetIteration;
			private set
			{
				if (value != _averageMapSectionTargetIteration)
				{
					_averageMapSectionTargetIteration = value;
					OnPropertyChanged();
				}
			}
		}

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
			get => _colorBandSetHistoryCollection.CurrentIndex;
			set	{ } 
		}

		public bool CanGoBack => _colorBandSetHistoryCollection.CurrentIndex > 0;
		public bool CanGoForward =>  _colorBandSetHistoryCollection.CurrentIndex < _colorBandSetHistoryCollection.Count - 1;

		public bool GoBack()
		{
			if (_colorBandSetHistoryCollection.MoveCurrentTo(_colorBandSetHistoryCollection.CurrentIndex - 1))
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
			if (_colorBandSetHistoryCollection.MoveCurrentTo(_colorBandSetHistoryCollection.CurrentIndex + 1))
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
			if (sender is ColorBand cb)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:CurrentColorBand Prop: {e.PropertyName} is changing.");

				var foundUpdate = false;

				if (e.PropertyName == nameof(ColorBand.StartColor))
				{
					if (TryGetPredeccessor(_currentColorBandSet, cb, out var colorBand))
					{
						colorBand.SuccessorStartColor = cb.StartColor;
					}

					foundUpdate = true;
				}
				else if (e.PropertyName == nameof(ColorBand.Cutoff))
				{
					if (TryGetSuccessor(_currentColorBandSet, cb, out var successorColorBand))
					{
						successorColorBand.PreviousCutoff = cb.Cutoff;
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
					if (e.PropertyName == nameof(ColorBand.EndColor))
					{
						if (cb.BlendStyle == ColorBandBlendStyle.Next)
						{
							if (TryGetSuccessor(_currentColorBandSet, cb, out var successorColorBand))
							{
								successorColorBand.StartColor = cb.EndColor;
							}
						}

						foundUpdate = true;
					}
				}

				if (foundUpdate)
				{
					PushCurrentColorBandOnToHistoryCollection();
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
					Debug.WriteLine($"ColorBandSetViewModel: The ColorBandSet is null while handling a CurrentColorBand_PropertyChanged event.");
				}
				else
				{
					Debug.WriteLine($"ColorBandSetViewModel: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");
				}
			}
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			if (ColorBandSet != null)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:ColorBandsView_CurrentChanged. Setting the SelectedColorBandIndex from: {ColorBandSet.SelectedColorBandIndex} to the ColorBandsView's CurrentPosition: {ColorBandsView.CurrentPosition}.");

				ColorBandSet.SelectedColorBandIndex = ColorBandsView.CurrentPosition;
			}

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
				//_histogram.Reset();
				_mapSectionHistogramProcessor.Reset();
				_topValues.Clear();
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items
				var cutoffs = GetCutoffs();
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.Add, cutoffs, mapSection.Histogram, HandleHistogramUpdate));
					_topValues.Increment(mapSection.TargetIterations);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var cutoffs = GetCutoffs();
				var mapSections = e.OldItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.Remove, cutoffs, mapSection.Histogram, HandleHistogramUpdate));
					_topValues.Decrement(mapSection.TargetIterations);
				}
			}

			//Debug.WriteLine($"There are {Histogram[Histogram.UpperBound - 1]} points that reached the target iterations.");
		}

		#endregion

		#region Public Methods

		public bool UpdateCutoff(int colorBandIndex, int newCutoff)
		{
			if (colorBandIndex < 0 | colorBandIndex > _currentColorBandSet.Count - 1)
			{
				throw new ArgumentOutOfRangeException(nameof(colorBandIndex), $"Cannot update the Cutoff for ColorBand at index: {colorBandIndex}. That value is out of range.");
			}

			if (colorBandIndex == _currentColorBandSet.Count - 1)
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel:TryUpdateCutoff is updating the ColorBandSet's High Cutoff.");
			}

			//ColorBandSet.ReportBucketWidthsAndCutoffs(_currentColorBandSet);

			//var cb = _currentColorBandSet[colorBandIndex];
			//cb.Cutoff = newCutoff;

			CurrentColorBand = _currentColorBandSet[colorBandIndex];
			CurrentColorBand.Cutoff = newCutoff;

			//ColorBandSet.ReportBucketWidthsAndCutoffs(_currentColorBandSet);

			//if (TryGetSuccessor(_currentColorBandSet, cb, out var successorColorBand))
			//{
			//	successorColorBand.PreviousCutoff = cb.Cutoff;
			//}

			//ColorBandSet.ReportBucketWidthsAndCutoffs(_currentColorBandSet);

			UpdatePercentages();

			PushCurrentColorBandOnToHistoryCollection();
			IsDirty = true;

			//ColorBandSet.ReportBucketWidthsAndCutoffs(_currentColorBandSet);

			if (UseRealTimePreview)
			{
				ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: false));
			}

			//ColorBandSet.ReportBucketWidthsAndCutoffs(_currentColorBandSet);

			return true;
		}

		public bool TryInsertNewItem(out int index)
		{
			if (ColorBandsView.CurrentItem is ColorBand curItem)
			{
				bool result;
				switch (EditMode)
				{
					case ColorBandSetEditMode.Offsets:
						result = TryInsertOffset(curItem, out index);
						break;
					case ColorBandSetEditMode.Colors:
						result = TryInsertColor(curItem, out index);
						break;
					case ColorBandSetEditMode.Bands:
						result = TryInsertColorBand(curItem, out index);
						break;
					default:
						throw new InvalidOperationException($"{EditMode} is not recognized.");
				}

				if (result)
				{
					PushCurrentColorBandOnToHistoryCollection();
					IsDirty = true;
					UpdatePercentages();

					if (UseRealTimePreview)
					{
						ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
					}
				}

				return result;
			}
			else
			{
				index = -1;
				return false;
			}
		}

		private bool TryInsertOffset(ColorBand curItem, out int index)
		{
			if (curItem.Cutoff - curItem.StartingCutoff < 1)
			{
				Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
				index = -1;
				return false;
			}

			var prevCutoff = curItem.PreviousCutoff ?? 0;
			var newCutoff = prevCutoff + (curItem.Cutoff - prevCutoff) / 2;

			var currentSet = _currentColorBandSet;
			index = currentSet.IndexOf(curItem);

			CurrentColorBand = null;
			_currentColorBandSet.InsertCutoff(index, newCutoff);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(curItem);

			return true;
		}

		private bool TryInsertColor(ColorBand curItem, out int index)
		{
			var currentSet = _currentColorBandSet;
			index = currentSet.IndexOf(curItem);

			var colorBand = new ColorBand(0, ColorBandColor.White, ColorBandBlendStyle.Next, curItem.StartColor);

			CurrentColorBand = null;
			_currentColorBandSet.InsertColor(index, colorBand);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(curItem);

			return true;
		}

		private bool TryInsertColorBand(ColorBand curItem, out int index)
		{
			if (curItem.Cutoff - curItem.StartingCutoff < 1)
			{
				Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
				index = -1;
				return false;
			}

			var prevCutoff = curItem.PreviousCutoff ?? 0;
			var newCutoff = prevCutoff + (curItem.Cutoff - prevCutoff) / 2;
			var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.Next, curItem.StartColor, curItem.PreviousCutoff, curItem.StartColor, double.NaN);

			var currentSet = _currentColorBandSet;
			index = currentSet.IndexOf(curItem);

			//Debug.WriteLine($"At InsertItem, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			lock (_histLock)
			{
				_currentColorBandSet.Insert(index, newItem);
			}

			if (!ColorBandsView.MoveCurrentTo(newItem))
			{
				Debug.WriteLine("ColorBandSetViewModel:Could not position the view to the new item.");
			}

			return true;
		}

		public bool TryDeleteSelectedItem()
		{
			if (ColorBandsView.CurrentItem is ColorBand curItem)
			{
				bool result;
				switch (EditMode)
				{
					case ColorBandSetEditMode.Offsets:
						result = TryDeleteOffset(curItem);
						break;
					case ColorBandSetEditMode.Colors:
						result = TryDeleteColor(curItem);
						break;
					case ColorBandSetEditMode.Bands:
						result = TryDeleteColorBand(curItem);
						break;
					default:
						throw new InvalidOperationException($"{EditMode} is not recognized.");
				}

				if (result)
				{
					PushCurrentColorBandOnToHistoryCollection();
					IsDirty = true;
					UpdatePercentages();

					if (UseRealTimePreview)
					{
						ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
					}
				}

				return result;
			}
			else
			{
				return false;
			}
		}

		private bool TryDeleteOffset(ColorBand curItem)
		{
			var currentSet = _currentColorBandSet;
			var index = currentSet.IndexOf(curItem);

			if (index > currentSet.Count - 2)
			{
				// Cannot delete the last entry
				return false;
			}

			CurrentColorBand = null;
			_currentColorBandSet.DeleteCutoff(index);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(curItem);

			return true;
		}

		private bool TryDeleteColor(ColorBand curItem)
		{
			var currentSet = _currentColorBandSet;
			var index = currentSet.IndexOf(curItem);

			if (index > currentSet.Count - 2)
			{
				// Cannot delete the last entry
				return false;
			}

			CurrentColorBand = null;
			_currentColorBandSet.DeleteColor(index);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(curItem);

			return true;
		}

		private bool TryDeleteColorBand(ColorBand curItem)
		{
			var currentSet = _currentColorBandSet;
			var index = currentSet.IndexOf(curItem);

			if (index >= currentSet.Count - 1)
			{
				// Cannot delete the last entry
				return false;
			}

			bool colorBandWasRemoved;
			lock (_histLock)
			{
				colorBandWasRemoved = currentSet.Remove(curItem);
			}

			if (!colorBandWasRemoved)
			{
				Debug.WriteLine("ColorBandSetViewModel:Could not remove the item.");
			}

			var newIndex = currentSet.IndexOf((ColorBand)ColorBandsView.CurrentItem);
			//Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {newIndex}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:Removed item at former index: {index}. The new index is: {newIndex}.");

			return true;
		}

		public void ApplyChanges(int newTargetIterations)
		{
			if (newTargetIterations != _currentColorBandSet.HighCutoff)
			{
				var newSet = ColorBandSetHelper.AdjustTargetIterations(_currentColorBandSet, newTargetIterations);
				ApplyChangesInt(newSet);
			}
		}

		public void ApplyChanges()
		{
			Debug.Assert(IsDirty, "ColorBandSetViewModel:ApplyChanges is being called, but we are not dirty.");
			var newSet = _currentColorBandSet.CreateNewCopy();

			ApplyChangesInt(newSet);
		}

		private void ApplyChangesInt(ColorBandSet newSet)
		{
			Debug.WriteLine($"The ColorBandSetViewModel is Applying changes. The new Id is {newSet.Id}, name: {newSet.Name}. The old Id is {ColorBandSet?.Id ?? ObjectId.Empty}");

			_colorBandSet = newSet;

			var curPos = ColorBandsView.CurrentPosition;

			// Clear all existing items from history and add the new set.
			_colorBandSetHistoryCollection.Load(_colorBandSet);

			BuildViewAndRaisePropertyChangeEvents(curPos);
			IsDirty = false;

			ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_colorBandSet, isPreview: false));
		}

		public void RevertChanges()
		{
			// Remove all but the first entry from the History Collection
			_colorBandSetHistoryCollection.Trim(0);

			var curPos = ColorBandsView.CurrentPosition;
			BuildViewAndRaisePropertyChangeEvents(curPos);
			IsDirty = false;

			UpdatePercentages();

			if (UseRealTimePreview)
			{
				ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
			}
		}

		public IDictionary<int, int> GetHistogramForColorBand(int index)
		{
			var currentColorBand = CurrentColorBand;

			if (currentColorBand == null)
			{
				return new Dictionary<int, int>();
			}
			else
			{
				var previousCutoff = currentColorBand.PreviousCutoff ?? 0;
				var cutoff = currentColorBand.Cutoff;

				//var kvpsForBand = _histogram.GetKeyValuePairs().Where(x => x.Key >= previousCutoff && x.Key < cutoff);
				var kvpsForBand = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(previousCutoff, cutoff, includeCatchAll: true);

				return new Dictionary<int, int>(kvpsForBand);
			}
		}

		public void RefreshPercentages()
		{
			UpdatePercentages();
		}

		#endregion

		#region Private Methods

		private void BuildViewAndRaisePropertyChangeEvents(int? selectedIndex = null)
		{
			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();

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
			//result.SortDescriptions.Add(new SortDescription("Cutoff", ListSortDirection.Ascending));

			return result;
		}

		private void PushCurrentColorBandOnToHistoryCollection()
		{
			_colorBandSetHistoryCollection.Push(_currentColorBandSet.CreateNewCopy());
			OnPropertyChanged(nameof(IUndoRedoViewModel.CurrentIndex));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoBack));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoForward));
		}

		private void UpdatePercentages()
		{
			var cutoffs = GetCutoffs();
			_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.Refresh, cutoffs, null, HandleHistogramUpdate));
		}

		//private void PopulateHistorgram(IEnumerable<MapSection> mapSections, IHistogram histogram)
		//{
		//	foreach (var ms in mapSections)
		//	{
		//		histogram.Add(ms.Histogram);
		//	}

		//	//var cutoffs = GetCutoffs();
		//	//_mapSectionHistogramProcessor.AddWork(new HistogramWorkRequest(HistogramWorkRequestType.BucketsUpdated, cutoffs, null, HandleHistogramUpdate));
		//}

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
					if (_currentColorBandSet.UpdatePercentages(newPercentages))
					{
						BeyondTargetSpecs = newPercentages[^1];
						AverageMapSectionTargetIteration = _topValues.GetAverage();
						//Debug.WriteLine($"CBS received new percentages top: {newPercentages[^1]}, total: {total}.");
					}
					else
					{
						BeyondTargetSpecs = null;
						AverageMapSectionTargetIteration = 0;
					}
				}
			}
		}

		private int[] GetCutoffs()
		{
			IEnumerable<int>? t;

			lock (_histLock)
			{
				t = _currentColorBandSet.Select(x => x.Cutoff);
			}

			return t.ToArray();
		}

		private bool TryGetPredeccessor(IList<ColorBand> colorBands, ColorBand cb, [NotNullWhen(true)] out ColorBand? colorBand)
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

		private bool TryGetSuccessor(IList<ColorBand> colorBands, ColorBand cb, [NotNullWhen(true)] out ColorBand? colorBand)
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
			var result = GetString(_colorBandSet);

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

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)

					_mapSectionHistogramProcessor.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
