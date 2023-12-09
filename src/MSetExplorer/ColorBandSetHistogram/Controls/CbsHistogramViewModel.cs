using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MSetExplorer
{
	public class CbsHistogramViewModel : ViewModelBase, IDisposable, IUndoRedoViewModel, ICbsHistogramViewModel
	{
		#region Private Fields

		private readonly object _paintLocker;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;
		private ColorBandSetEditMode _editMode;

		private ColorBandSet _colorBandSet;			// The value assigned to this model
		private bool _useEscapeVelocities;
		private bool _useRealTimePreview;
		private bool _highlightSelectedBand;

		private readonly ColorBandSetHistoryCollection _colorBandSetHistoryCollection;

		private ColorBandSet _currentColorBandSet;	// The value which is currently being edited.

		private ListCollectionView _colorBandsView;
		private ColorBand? _currentColorBand;

		private bool _isDirty;
		private readonly object _histLock;

		private bool _isEnabled;
		private Visibility _windowVisibility;


		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Private Fields - Ploting

		// Used by the HistogramColorBandControl
		private HPlotSeriesData _seriesData;

		private SizeDbl _unscaledExtent;            // Size of entire content at max zoom (i.e, 4 x Target Iterations)
		private SizeDbl _viewportSize;              // Size of display area in device independent pixels.
		private SizeDbl _contentViewportSize;       // Size of visible content

		private VectorDbl _displayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;
		private double _maximumDisplayZoom;

		private ScrollBarVisibility _horizontalScrollBarVisibility;

		#endregion

		#region Constructor

		public CbsHistogramViewModel(IMapSectionHistogramProcessor mapSectionHistogramProcessor)
		{
			_paintLocker = new object();

			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;
			_mapSectionHistogramProcessor.HistogramUpdated += HistogramUpdated;

			_colorBandSet = new ColorBandSet();
			_editMode = ColorBandSetEditMode.Bands;

			_colorBandSetHistoryCollection = new ColorBandSetHistoryCollection(new List<ColorBandSet> { new ColorBandSet() });
			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.Clone();

			//_colorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);
			_colorBandsView = BuildColorBandsView(null);
			_currentColorBand = null;

			_isDirty = false;
			_histLock = new object();
			BeyondTargetSpecs = null;

			_isEnabled = true;
			_windowVisibility = Visibility.Visible;

			// Plotting
			_seriesData = HPlotSeriesData.Empty;

			_unscaledExtent = new SizeDbl();
			_viewportSize = new SizeDbl(500, 300);
			_contentViewportSize = new SizeDbl();

			_displayPosition = new VectorDbl();

			_minimumDisplayZoom = RMapConstants.DEFAULT_MINIMUM_DISPLAY_ZOOM; // 0.0625;
			_maximumDisplayZoom = 4.0;
			_displayZoom = 1;

			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		}

		#endregion

		#region Public Events

		public event EventHandler<ColorBandSetUpdateRequestedEventArgs>? ColorBandSetUpdateRequested;

		public event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;

		#endregion

		#region Public Properties - Content

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

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					// Temporarily set the view using a empty ColorBandSet.
					// Presumably the call to ResetView requires that the ColorBandsView have some value.
					ColorBandsView = BuildColorBandsView(null);

					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is processing a new ColorBandSet. Id = {value.Id}.");

					_colorBandSet = value;

					var unscaledWidth = GetExtent(_colorBandSet);

					if (unscaledWidth > 10)
					{
						ResetView(unscaledWidth, DisplayPosition, DisplayZoom);
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is not resetting the view -- the unscaled width <= 10.");
					}

					lock (_histLock)
					{
						_colorBandSetHistoryCollection.Load(value.CreateNewCopy());

						if (IsEnabled)
						{
							_mapSectionHistogramProcessor.Reset(value.HighCutoff);
							UpdatePercentages();
						}
					}

					IsDirty = false;

					// This sets the ColorBandsView
					UpdateViewAndRaisePropertyChangeEvents();
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is NOT processing the new ColorBandSet. The Id already = {value.Id}.");
				}
			}
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

						colorBandSet = colorBandSet.CreateNewCopy();
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
				_colorBandsView.CurrentChanged -= ColorBandsView_CurrentChanged;

				_colorBandsView = value;
				var testItem = _colorBandsView.CurrentItem;
				if (testItem is ColorBand cb)
				{
					CurrentColorBand = cb;
				}
				else
				{
					if (_colorBandsView.Count > 0 && _colorBandsView.GetItemAt(0) is ColorBand cb2)
					{
						CurrentColorBand = cb2;
					}
					else
					{
						CurrentColorBand = new ColorBand();
					}
				}

				_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;

				OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsView));
				OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBandIndex));
				OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBandNumber));
				OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsCount));
			}
		}

		public ColorBand? CurrentColorBand
		{
			get => _currentColorBand;
			set
			{
				if (value != _currentColorBand)
				{
					if (_currentColorBand != null)
					{
						_currentColorBand.PropertyChanged -= CurrentColorBand_PropertyChanged;
						_currentColorBand.EditEnded -= CurrentColorBand_EditEnded;
					}

					_currentColorBand = value;

					if (_currentColorBand != null)
					{
						_currentColorBand.PropertyChanged += CurrentColorBand_PropertyChanged;
						_currentColorBand.EditEnded += CurrentColorBand_EditEnded;
					}

					OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBand));
					OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBandIndex));
					OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBandNumber));
				}
			}
		}

		public int CurrentColorBandIndex => ColorBandsView.CurrentPosition;
		public int CurrentColorBandNumber => ColorBandsView.CurrentPosition + 1;

		public int ColorBandsCount => ColorBandsView.Count;

		public PercentageBand? BeyondTargetSpecs { get; private set; }

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

		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				if (value != _isEnabled)
				{
					_isEnabled = value;

					// TODO: Subscribe / Unsubscribe Histogram Updates here.
					if (_isEnabled)
					{
						//_mapSections.CollectionChanged += MapSections_CollectionChanged;
					}
					else
					{
						//_mapSections.CollectionChanged -= MapSections_CollectionChanged;
					}

					WindowVisibility = _isEnabled ? Visibility.Visible : Visibility.Collapsed;

					OnPropertyChanged();
				}
			}
		}

		public Visibility WindowVisibility
		{
			get => _windowVisibility;
			set
			{
				if (value != _windowVisibility)
				{
					_windowVisibility = value;
					IsEnabled = _windowVisibility == Visibility.Visible ? true : false;
					OnPropertyChanged();
				}
			}
		}

		//public int PlotExtent => SeriesData.Length;

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (!value.IsNAN() && value != _viewportSize)
				{
					if (value.Width <= 2 || value.Height <= 2)
					{
						Debug.WriteLine($"WARNING: CbsHistogramViewModel is having its ViewportSize set to {value}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}.");
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ViewportSize set to {value}. Previously it was {_viewportSize}. Updating it's ContainerSize.");

						_viewportSize = value;

						OnPropertyChanged(nameof(ICbsHistogramViewModel.ViewportSize));
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ViewportSize set to {value}.The current value is aleady: {_viewportSize}, not updating the ContainerSize.");
				}
			}
		}

		public SizeDbl ContentViewportSize
		{
			get => _contentViewportSize;
			set
			{
				if (!value.IsNAN() && value != _contentViewportSize)
				{
					if (value.Width <= 2 || value.Height <= 2)
					{
						Debug.WriteLine($"WARNING: CbsHistogramViewModel is having its ContentViewportSize set to {value}, which is very small. Update was aborted. The ContentViewportSize remains: {_contentViewportSize}.");
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ContentViewportSize set to {value}. Previously it was {_contentViewportSize}.");

						_contentViewportSize = value;

						OnPropertyChanged(nameof(ICbsHistogramViewModel.ContentViewportSize));
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ContentViewportSize set to {value}.The current value is aleady: {_contentViewportSize}.");
				}
			}
		}

		public HPlotSeriesData SeriesData
		{
			get => _seriesData;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's Series is being set. There are {value.LongLength} entries.");
				_seriesData = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.SeriesData));
			}
		}

		#endregion

		#region Public Properties - Scroll

		public SizeDbl UnscaledExtent
		{
			get => _unscaledExtent;
			set
			{
				if (_unscaledExtent != value)
				{
					_unscaledExtent = value;

					Debug.WriteLineIf(_useDetailedDebug, $"\nThe ColorBandSetHistogram's UnscaledExtent is being set to {value}.");

					//UpdateFoundationRectangle(_foundationRectangle, value);

					OnPropertyChanged(nameof(ICbsHistogramViewModel.UnscaledExtent));
				}
			}
		}

		public VectorDbl DisplayPosition
		{
			get => _displayPosition;
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(value, DisplayPosition))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's DisplayPosition is being updated from {_displayPosition} to {value}.");

					_displayPosition = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.DisplayPosition));
				}
			}
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (ScreenTypeHelper.IsDoubleChanged(value, _displayZoom, 0.00001))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's DisplayZoom is being updated from {_displayZoom} to {value}.");
					
					_displayZoom = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.DisplayZoom));
				}
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\nThe CbsHistogramViewModel's MinimumDisplayZoom is being updated from {_minimumDisplayZoom} to {value}.");

				_minimumDisplayZoom = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.MinimumDisplayZoom));
			}
		}

		public double MaximumDisplayZoom
		{
			get => _maximumDisplayZoom;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\nThe CbsHistogramViewModel's MaximumDisplayZoom is being updated from {_maximumDisplayZoom} to {value}.");

				_maximumDisplayZoom = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.MaximumDisplayZoom));
			}
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => _horizontalScrollBarVisibility;
			set
			{
				_horizontalScrollBarVisibility = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.HorizontalScrollBarVisibility));
			}
		}

		#endregion

		#region Public Methods

		public bool TryMoveCurrentColorBandToNext()
		{
			if (CurrentColorBandIndex > ColorBandsCount - 2) return false;
			var result = _colorBandsView.MoveCurrentToNext();
			return result;
		}

		public bool TryMoveCurrentColorBandToPrevious()
		{
			if (CurrentColorBandIndex < 1) return false;

			var result = _colorBandsView.MoveCurrentToPrevious();
			return result;
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

			// Clear all existing items from history and add the new set.
			_colorBandSetHistoryCollection.Load(_colorBandSet);

			//var curPos = ColorBandsView.CurrentPosition;
			var curPos = CurrentColorBandIndex;

			UpdateViewAndRaisePropertyChangeEvents(curPos);
			IsDirty = false;

			ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_colorBandSet, isPreview: false));
		}

		public void RevertChanges()
		{
			// Remove all but the first entry from the History Collection
			_colorBandSetHistoryCollection.Trim(0);

			var curPos = CurrentColorBandIndex;
			UpdateViewAndRaisePropertyChangeEvents(curPos);

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

		#region Public Methods - Plotting

		public bool RefreshDisplay()
		{
			bool result;

			lock (_paintLocker)
			{
				var values = _mapSectionHistogramProcessor.Histogram.Values;

				var anyNonzero = values.Any(x => x > 0);
				result = !anyNonzero;

				BuildSeriesData(SeriesData, values);
			}

			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's SerieData is being updated There are {SeriesData.LongLength} entries.");

			// TODO: Implement some way to have the HistogramPlotControl be notfied of an update without creating a new instance.
			SeriesData = new HPlotSeriesData(SeriesData);

			//OnPropertyChanged(nameof(ICbsHistogramViewModel.SeriesData));

			return result;
		}

		public int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"\nCbsHistogramViewModel is having its ViewportSizePosAndScale set to size:{contentViewportSize}, offset:{contentOffset}, scale:{contentScale}.");

			//_displayZoom uses a binding to stay curent with contentScale;	
			Debug.Assert(_displayZoom == contentScale, "The DisplayZoom does not equal the new ContentScale on the call to UpdateViewportSizePosAndScale.");

			ContentViewportSize = contentViewportSize;
			DisplayPosition = contentOffset;

			return null;
		}

		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"\nCbsHistogramViewModel is having its ViewportSizeAndPos set to size:{contentViewportSize}, offset:{contentOffset}.");

			ContentViewportSize = contentViewportSize;
			DisplayPosition = contentOffset;

			return null;
		}

		public int? MoveTo(VectorDbl displayPosition)
		{
			if (UnscaledExtent.IsNearZero())
			{
				throw new InvalidOperationException("Cannot call MoveTo, if the UnscaledExtent is zero.");
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is moving to position:{displayPosition}.");

			DisplayPosition = displayPosition;

			return null;
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				SeriesData.Clear();
			}
		}

		#endregion

		#region UndoRedoPile Properties / Methods

		public int CurrentIndex
		{
			get => _colorBandSetHistoryCollection.CurrentIndex;
			set { }
		}

		public bool CanGoBack => _colorBandSetHistoryCollection.CurrentIndex > 0;
		public bool CanGoForward => _colorBandSetHistoryCollection.CurrentIndex < _colorBandSetHistoryCollection.Count - 1;

		public bool GoBack()
		{
			if (_colorBandSetHistoryCollection.MoveCurrentTo(_colorBandSetHistoryCollection.CurrentIndex - 1))
			{
				var curPos = CurrentColorBandIndex;
				UpdateViewAndRaisePropertyChangeEvents(curPos);
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
				var curPos = CurrentColorBandIndex;
				UpdateViewAndRaisePropertyChangeEvents(curPos);
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Event Handlers

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			if (ColorBandsView != null)
			{
				if (ColorBandSet != null)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:ColorBandsView_CurrentChanged. Setting the SelectedColorBandIndex from: {ColorBandSet.SelectedColorBandIndex} to the ColorBandsView's CurrentPosition: {ColorBandsView.CurrentPosition}.");

					ColorBandSet.SelectedColorBandIndex = ColorBandsView.CurrentPosition;
				}

				CurrentColorBand = (ColorBand)ColorBandsView.CurrentItem;
			}
		}

		private void CurrentColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			ColorBand cb;

			if (sender is ColorBand)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:CurrentColorBand Prop: {e.PropertyName} is changing.");
				cb = (ColorBand)sender;
			}
			else
			{
				Debug.WriteLine($"ColorBandSetViewModel: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");
				return;
			}

			bool foundUpdate;

			// StartColor is being updated.
			if (e.PropertyName == nameof(ColorBand.StartColor))
			{
				if (TryGetPredeccessor(_currentColorBandSet, cb, out var colorBand))
				{
					colorBand.SuccessorStartColor = cb.StartColor;
				}

				foundUpdate = true;
			}

			// Cutoff is being updated
			else if (e.PropertyName == nameof(ColorBand.Cutoff))
			{
				if (TryGetSuccessor(_currentColorBandSet, cb, out var successorColorBand))
				{
					successorColorBand.PreviousCutoff = cb.Cutoff;
				}

				foundUpdate = true;
				UpdatePercentages();
			}

			// BlendStyle is being updated
			else if (e.PropertyName == nameof(ColorBand.BlendStyle))
			{
				//cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? cb.SuccessorStartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;
				foundUpdate = true;
			}

			// EndColor is being updated
			else if (e.PropertyName == nameof(ColorBand.EndColor))
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
			else
			{
				// Some other property is being updated.
				foundUpdate = false;
			}

			if (foundUpdate)
			{
				// Don't include each property change when the Current ColorBand is being edited.
				if (!cb.IsInEditMode)
				{
					PushCurrentColorBandOnToHistoryCollection();
					IsDirty = true;
				}

				if (UseRealTimePreview)
				{
					var newColorBandSet = _currentColorBandSet.CreateNewCopy();

					ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(newColorBandSet, isPreview: true));
				}
			}

		}

		private void CurrentColorBand_EditEnded(object? sender, EventArgs e)
		{
			PushCurrentColorBandOnToHistoryCollection();
			IsDirty = true;
		}

		private void HistogramUpdated(object? sender, HistogramUpdateType e)
		{
			// TODO: Consider handling BlockAdd and BlockRemoves

			switch (e)
			{
				case HistogramUpdateType.BlockAdded:
					//Debug.WriteLine($"Not handling HistogramUpdated. The update type is {e}.");
					break;
				case HistogramUpdateType.BlockRemoved:
					//Debug.WriteLine($"Not handling HistogramUpdated. The update type is {e}.");
					break;
				case HistogramUpdateType.Clear:
					{
						lock (_paintLocker)
						{
							SeriesData.ClearYValues();
							SeriesData = new HPlotSeriesData(SeriesData);
						}
						break;
					}
				case HistogramUpdateType.Refresh:
					{
						RefreshDisplay();
						break;
					}
				default:
					break;
			}
		}

		#endregion

		#region Private Methods - Series Data

		private void BuildSeriesData(HPlotSeriesData hPlotSeriesData, int[] values)
		{
			var newLength = values.LongLength;
			
			if (newLength < 1)
			{
				Debug.WriteLine($"WARNING: The Histogram is empty (BuildSeriesData).");

				hPlotSeriesData.Clear();
				return;
			}


			var existingLength = hPlotSeriesData.LongLength;
			hPlotSeriesData.SetYValues(values, out var bufferWasPreserved);

			ReportSeriesBufferAllocation(existingLength, newLength, bufferWasPreserved);
		}

		private int GetExtent(ColorBandSet colorBandSet/*, out int endPtr*/)
		{
			var result = colorBandSet.Count < 2 ? 0 : colorBandSet.HighCutoff;
			//endPtr = colorBandSet.Count < 2 ? 0 : colorBandSet.Count - 1;
			return result;
		}

		private void ResetView(double extentWidth, VectorDbl displayPosition, double displayZoom)
		{
			if (ScreenTypeHelper.IsDoubleChanged(extentWidth, UnscaledExtent.Width))
			{
				// The ExtentWidth is changing -- reset the position and scale.
				UnscaledExtent = new SizeDbl();

				var extent = new SizeDbl(extentWidth, _viewportSize.Height);
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is raising the DisplaySettingsInitialized Event and resetting the Position and Scale.");

				//var initialSettingsF = new DisplaySettingsInitializedEventArgs(extent, displayPosition, displayZoom);

				// Override the position
				var positionZero = new VectorDbl();
				// Override the zoom setting
				var zoomUnity = 1d;
				var initialSettings = new DisplaySettingsInitializedEventArgs(extent, positionZero, zoomUnity);

				DisplaySettingsInitialized?.Invoke(this, initialSettings);

				// Trigger a ViewportChanged event on the PanAndZoomControl -- this will result in our UpdateViewportSizeAndPos method being called.
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is setting the Unscaled Extent to complete the process of initializing the Histogram Display.");
				UnscaledExtent = extent;
			}
			else
			{
				Debug.WriteLine("The CbsHistogramViewModel is skipping ResetView: the current and new extent (i.e., Target Iterations) are the same.");

				UnscaledExtent = new SizeDbl();

				var extent = new SizeDbl(extentWidth, _viewportSize.Height);
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is raising the DisplaySettingsInitialized Event and keeping the Position and Scale.");

				var initialSettings = new DisplaySettingsInitializedEventArgs(extent, displayPosition, displayZoom);

				DisplaySettingsInitialized?.Invoke(this, initialSettings);

				// Trigger a ViewportChanged event on the PanAndZoomControl -- this will result in our UpdateViewportSizeAndPos method being called.
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is setting the Unscaled Extent to complete the process of initializing the Histogram Display.");
				UnscaledExtent = extent;

			}
		}

		#endregion

		#region Private Methods - ColorBandsView

		private void UpdateViewAndRaisePropertyChangeEvents(int? selectedIndex = null)
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
			ListCollectionView result;

			if (colorBands == null)
			{
				var newCollection = new ObservableCollection<ColorBand>();
				result = (ListCollectionView)CollectionViewSource.GetDefaultView(newCollection);
			}
			else
			{
				result = (ListCollectionView)CollectionViewSource.GetDefaultView(colorBands);
			}

			return result;
		}

		private void PushCurrentColorBandOnToHistoryCollection()
		{
			_colorBandSetHistoryCollection.Push(_currentColorBandSet.CreateNewCopy());
			OnPropertyChanged(nameof(IUndoRedoViewModel.CurrentIndex));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoBack));
			OnPropertyChanged(nameof(IUndoRedoViewModel.CanGoForward));
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


		private int GetColorBandIndex(IList<ColorBand> colorBands, ColorBand cb)
		{
			var index = colorBands.IndexOf(cb);
			return index;
		}

		#endregion

		#region Private Methods - Percentages

		private void UpdatePercentages()
		{
			var cutoffs = GetCutoffs();
			var newPercentages = BuildNewPercentages(cutoffs, _mapSectionHistogramProcessor.Histogram);
			ApplyNewPercentages(newPercentages);
		}

		private PercentageBand[] BuildNewPercentages(int[] cutoffs, IHistogram histogram)
		{
			var pbList = cutoffs.Select(x => new PercentageBand(x)).ToList();
			pbList.Add(new PercentageBand(int.MaxValue));

			var bucketCnts = pbList.ToArray();

			var curBucketPtr = 0;
			var curBucketCut = cutoffs[curBucketPtr];

			long runningSum = 0;

			var kvps = histogram.GetKeyValuePairs();

			var i = 0;

			for (; i < kvps.Length && curBucketPtr < bucketCnts.Length; i++)
			{
				var idx = kvps[i].Key;
				var amount = kvps[i].Value;

				while (curBucketPtr < bucketCnts.Length && idx > curBucketCut)
				{
					curBucketPtr++;
					curBucketCut = bucketCnts[curBucketPtr].Cutoff;
				}

				runningSum += amount;

				if (idx == curBucketCut)
				{
					bucketCnts[curBucketPtr].ExactCount = amount;
				}

				bucketCnts[curBucketPtr].Count += amount;
				bucketCnts[curBucketPtr].RunningSum = runningSum;
			}

			for (; i < kvps.Length; i++)
			{
				var amount = kvps[i].Value;
				runningSum += amount;

				bucketCnts[^1].Count += amount;
				bucketCnts[^1].RunningSum = runningSum;
			}

			runningSum += histogram.UpperCatchAllValue;
			bucketCnts[^1].Count += histogram.UpperCatchAllValue;
			bucketCnts[^1].RunningSum = runningSum;

			// For now, include all of the cnts above the target in the last bucket.
			bucketCnts[^2].Count += bucketCnts[^1].Count;

			//var total = (double)histogram.Values.Select(x => Convert.ToInt64(x)).Sum();
			var total = (double)runningSum;

			foreach (var pb in bucketCnts)
			{
				pb.Percentage = Math.Round(100 * (pb.Count / total), 2);
			}

			return bucketCnts;
		}

		private void ApplyNewPercentages(PercentageBand[] newPercentages)
		{
			lock (_histLock)
			{
				if (_currentColorBandSet.UpdatePercentages(newPercentages))
				{
					BeyondTargetSpecs = newPercentages[^1];
					//Debug.WriteLine($"CBS received new percentages top: {newPercentages[^1]}, total: {total}.");
				}
				else
				{
					BeyondTargetSpecs = null;
				}
			}
		}

		private int[] GetCutoffs()
		{
			IEnumerable<int>? cutoffs;

			lock (_histLock)
			{
				cutoffs = _currentColorBandSet.Select(x => x.Cutoff);
			}

			return cutoffs.ToArray();
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportSeriesBufferAllocation(long existingLength, long newLength, bool bufferWasPreserved)
		{
			if (!bufferWasPreserved)
			{
				Debug.WriteLine($"WARNING: Allocating new buffer to hold the Y Values. New length:{newLength}, existing length:{existingLength}.");
			}
			else
			{
				Debug.WriteLine($"Updating SeriesData. Using existing buffer. Length:{newLength}.");
			}

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

		#region UN USED

		//public ScaledImageViewInfo ViewportSizePositionAndScale
		//{
		//	get => _viewportSizePositionAndScale;
		//	set
		//	{
		//		lock (_paintLocker)
		//		{
		//			_viewportSize = value.ContentViewportSize;
		//			var offset = new VectorDbl(value.ContentOffset.X * _scaleTransform.ScaleX, 0);
		//			ImageOffset = offset;
		//		}
		//	}
		//}

		//private HPlotSeriesData BuildSeriesDataOld()
		//{
		//	var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
		//	var endingIndex = _colorBandSet[EndPtr].Cutoff;
		//	//var highCutoff = _colorBandSet.HighCutoff;

		//	var hEntries = GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

		//	if (hEntries.Length < 1)
		//	{
		//		Debug.WriteLine($"WARNING: The Histogram is empty (BuildSeriesData).");
		//		return HPlotSeriesData.Zero;
		//	}

		//	var dataX = new double[hEntries.Length];
		//	var dataY = new double[hEntries.Length];

		//	var extent = Math.Min(hEntries.Length, PlotExtent);

		//	for (var hPtr = 0; hPtr < extent; hPtr++)
		//	{
		//		var hEntry = hEntries[hPtr];

		//		dataX[hPtr] = hEntry.Key;
		//		dataY[hPtr] = hEntry.Value;
		//	}

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

		//private KeyValuePair<int, int>[] GetKeyValuePairsForBand(int startingIndex, int endingIndex, bool includeCatchAll)
		//{
		//	var hEntries = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true);

		//	return hEntries;

		//}

		//private IEnumerable<KeyValuePair<int, int>> GetKeyValuePairsForBand(int previousCutoff, int cutoff)
		//{
		//	return _mapSectionHistogramProcessor.GetKeyValuePairsForBand(previousCutoff, cutoff);
		//}

		//private int[] GetACopyOfTheValuesArray()
		//{
		//	return _mapSectionHistogramProcessor.Histogram.Values;
		//}

		//private (double[] dataX, double[] dataY) GetPlotData1()
		//{
		//	//ClearHistogramItems();

		//	var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
		//	var endingIndex = _colorBandSet[EndPtr].Cutoff;
		//	var highCutoff = _colorBandSet.HighCutoff;

		//	var hEntries = GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

		//	if (hEntries.Length < 1)
		//	{
		//		Debug.WriteLine($"WARNING: The Histogram is empty (BuildSeriesData).");
		//		return (new double[0], new double[0]);
		//	}

		//	var dataX = new double[hEntries.Length];
		//	var dataY = new double[hEntries.Length];

		//	for (var hPtr = 0; hPtr < hEntries.Length; hPtr++)
		//	{
		//		var hEntry = hEntries[hPtr];
		//		dataX[hPtr] = hEntry.Key;
		//		dataY[hPtr] = hEntry.Value;
		//	}

		//	return (dataX, dataY);
		//}


		//private int _histElevation = 2;
		//private int _histDispHeight = 165;

		//private void DrawHistogram()
		//{
		//	ClearHistogramItems();

		//	var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
		//	var endingIndex = _colorBandSet[EndPtr].Cutoff;
		//	var highCutoff = _colorBandSet.HighCutoff;

		//	//var rn = 1 + endingIndex - startingIndex;
		//	//if (Math.Abs(LogicalDisplaySize.Width - rn) > 20)
		//	//{
		//	//	Debug.WriteLineIf(_useDetailedDebug, $"The range of indexes does not match the Logical Display Width. Range: {endingIndex - startingIndex}, Width: {LogicalDisplaySize.Width}.");
		//	//	return;
		//	//}

		//	//LogicalDisplaySize = new SizeInt(rn + 10, _canvasSize.Height);

		//	var w = (int) Math.Round(UnscaledExtent.Width);

		//	DrawHistogramBorder(w, _histDispHeight);

		//	var hEntries = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

		//	if (hEntries.Length < 1)
		//	{
		//		Debug.WriteLine($"WARNING: The Histogram is empty. (DrawHistogram)");
		//		return;
		//	}

		//	var maxV = hEntries.Max(x => x.Value) + 5; // Add 5 to reduce the height of each line.
		//	var vScaleFactor = _histDispHeight / (double)maxV;

		//	var geometryGroup = new GeometryGroup();

		//	foreach (var hEntry in hEntries)
		//	{
		//		var x = 1 + hEntry.Key - startingIndex;
		//		var height = hEntry.Value * vScaleFactor;
		//		geometryGroup.Children.Add(BuildHLine(x, height));
		//	}

		//	var hTestEntry = hEntries[^1];

		//	var lineGroupDrawing = new GeometryDrawing(Brushes.IndianRed, new Pen(Brushes.DarkRed, 0.75), geometryGroup);

		//	_historgramItems.Add(lineGroupDrawing);
		//	_drawingGroup.Children.Add(lineGroupDrawing);
		//}

		//private LineGeometry BuildHLine(int x, double height)
		//{
		//	//var result = new LineGeometry(new Point(x, _histDispHeight + _histElevation), new Point(x, _histDispHeight - height + _histElevation));

		//	//var lineTop = _histDispHeight + _histElevation - height;
		//	//var result = new LineGeometry(new Point(x, lineTop), new Point(x, lineTop + height));

		//	// Top of the display is when y = 0, y increases as you move from top to bottom
		//	var lineBottom = 1 + _histDispHeight + _histElevation;
		//	var lineTop = lineBottom - height;

		//	var result = new LineGeometry(new Point(x, lineBottom), new Point(x, lineTop));


		//	return result;
		//}

		//private GeometryDrawing DrawHistogramBorder(int width, int height)
		//{
		//	Debug.WriteLineIf(_useDetailedDebug, $"Drawing the Histogram Border with width: {width}.");

		//	var borderSize = width > 1 && height > 1 ? new Size(width - 1, height - 1) : new Size(1, 1);

		//	var histogramBorder = new GeometryDrawing
		//	(
		//		Brushes.Transparent,
		//		new Pen(Brushes.DarkGray, 0.5),
		//		new RectangleGeometry(new Rect(new Point(2, _histElevation), borderSize))
		//	);

		//	_historgramItems.Add(histogramBorder);
		//	_drawingGroup.Children.Add(histogramBorder);

		//	return histogramBorder;
		//}

		//private GeometryDrawing DrawHistogramBorder(int width, int height)
		//{
		//	var topLeft = new Point(0, 0);
		//	var borderSize = width > 1 && height > 1 ? new Size(width + 2, height + 2) : new Size(1, 1);

		//	var histogramBorder = new GeometryDrawing
		//	(
		//		Brushes.Transparent,
		//		new Pen(Brushes.DarkGray, 1),
		//		new RectangleGeometry(new Rect(topLeft, borderSize))
		//	);

		//	Debug.WriteLineIf(_useDetailedDebug, $"Drawing the Histogram Border with Size: {borderSize} at pos: {topLeft}.");


		//	_historgramItems.Add(histogramBorder);
		//	_drawingGroup.Children.Add(histogramBorder);

		//	return histogramBorder;
		//}

		//private GeometryDrawing BuildFoundationRectangle(SizeDbl logicalDisplaySize)
		//{
		//	var result = new GeometryDrawing
		//	(
		//		Brushes.Transparent,
		//		new Pen(Brushes.DarkViolet, 3),
		//		new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize))
		//	);

		//	return result;
		//}

		//private void UpdateFoundationRectangle(GeometryDrawing foundationRectangle, SizeDbl logicalDisplaySize)
		//{
		//	foundationRectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize));
		//}

		//private HPlotSeriesData BuildTestSeries()
		//{
		//	double[] dataX = new double[] { 1, 2, 3, 4, 5 };
		//	double[] dataY = new double[] { 1, 4, 9, 16, 25 };

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

		#endregion
	}
}
