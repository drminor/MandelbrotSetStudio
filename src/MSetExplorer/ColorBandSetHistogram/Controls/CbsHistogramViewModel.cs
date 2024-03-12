using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class CbsHistogramViewModel : ViewModelBase, IUndoRedoViewModel, ICbsHistogramViewModel
	{
		#region Private Fields

		private const int SELECTION_LINE_UPDATE_THROTTLE_INTERVAL = 200;
		private DebounceDispatcher _selectionLineMovedDispatcher;

		private readonly object _paintLocker;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;
		private ColorBandSetEditMode _currentCbEditMode;

		private ColorBandSet _colorBandSet;         // The value assigned to this model

		private bool _useEscapeVelocities;
		private bool _useRealTimePreview;
		private bool _highlightSelectedBand;

		private bool _usePercentagesGlobally;
		private bool _usePercentagesLocalSetting;
		private bool _usePercentagesGlobalSetting;

		private string _percentageUseStatus;

		private readonly ColorBandSetHistoryCollection _colorBandSetHistoryCollection;

		private ColorBandSet _currentColorBandSet;  // The value which is currently being edited.

		private ListCollectionView _colorBandsView;
		private ColorBand? _currentColorBand;

		private bool _isDirty;
		private readonly object _histLock;

		private bool _isEnabled;

		private bool _colorBandUserControlHasErrors;
		private bool _disableProcessCurColorBandPropertyChanges;

		private readonly bool _traceCBSVersions = true;

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

			_selectionLineMovedDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;
			_mapSectionHistogramProcessor.HistogramUpdated += HistogramUpdated;

			_colorBandSet = new ColorBandSet();

			_currentCbEditMode = ColorBandSetEditMode.Bands;
			_useEscapeVelocities = true;
			_useRealTimePreview = true;
			_highlightSelectedBand = false;

			_usePercentagesGlobally = false;
			_usePercentagesLocalSetting = false;
			_usePercentagesGlobalSetting = false;
			_percentageUseStatus = string.Empty;

			_colorBandSetHistoryCollection = new ColorBandSetHistoryCollection(new List<ColorBandSet> { new ColorBandSet() });
			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.Clone();

			_colorBandsView = BuildColorBandsView(null);
			_currentColorBand = null;

			_isDirty = false;
			_histLock = new object();
			BeyondTargetSpecs = null;

			_isEnabled = true;

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

		public ColorBandSetEditMode CurrentCbEditMode
		{
			get => _currentCbEditMode;
			set
			{
				if (value != _currentCbEditMode)
				{
					Debug.WriteLine($"CbsHistogramViewModel: The Edit mode is now {value}");
					_currentCbEditMode = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentCbEditMode));
					OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentCbEditModeAsString));
				}
			}
		}

		public string CurrentCbEditModeAsString
		{
			get => _currentCbEditMode.ToString();
		}

		public ObjectId ColorBandSetBeingEditedId => _currentColorBandSet.Id;

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					// TODO: Save the Undo/Redo info.
					//if (_colorBandSetHistoryCollection.Count > 1)
					//{
					//
					//}

					// Temporarily set the view using a empty ColorBandSet.
					// Presumably the call to ResetView requires that the ColorBandsView have some value.
					ColorBandsView = BuildColorBandsView(null);

					Debug.WriteLineIf(_traceCBSVersions, $"The CbsHistogramViewModel's ColorBandSet is being updated from: {_colorBandSet.Id}/{_colorBandSet.LastUpdatedUtc} to {value.Id}/{value.LastUpdatedUtc}.");
					Debug.WriteLine(value.ToString(style: 1));

					_colorBandSet = value;

					if (!UsePercentagesGlobally)
					{
						UsePercentagesLocalSetting = _colorBandSet.UsingPercentages;
					}

					//if (!IsEnabled) return;

					var unscaledWidth = GetExtent(_colorBandSet);

					if (unscaledWidth > 10)
					{
						ResetView(unscaledWidth, DisplayPosition, DisplayZoom);
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is not resetting the view -- the unscaled width <= 10.");
					}

					HistCutoffsSnapShot histCutoffsSnapShot;
					lock (_histLock)
					{
						_colorBandSetHistoryCollection.Load(value.CreateNewCopy());
						IsDirty = false;
						_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();

						_mapSectionHistogramProcessor.Clear(value.HighCutoff);
						histCutoffsSnapShot = GetHistCutoffsSnapShot(_mapSectionHistogramProcessor.Histogram, histogramIsFromACompleteMap: false, _currentColorBandSet);
					}

					PercentageUseStatus = GetPercentageUseStatus(_currentColorBandSet.UsingPercentages, UsePercentagesLocalSetting, _mapSectionHistogramProcessor.Histogram);

					ApplyHistogram(histCutoffsSnapShot);

					// This sets the ColorBandsView
					UpdateViewAndRaisePropertyChangeEvents();
				}
				else
				{
					Debug.WriteLineIf(_traceCBSVersions, $"The CbsHistogramViewModel's ColorBandSet is not being updated. The Id already = {value.Id}/{value.LastUpdatedUtc}.");
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
					OnPropertyChanged(nameof(ICbsHistogramViewModel.UseEscapeVelocities));
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
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is turning {strState} the use of RealTimePreview. IsDirty = {IsDirty}.");
					_useRealTimePreview = value;

					if (IsDirty)
					{
						var colorBandSet = _useRealTimePreview ? _currentColorBandSet : _colorBandSetHistoryCollection[0];

						colorBandSet = colorBandSet.CreateNewCopy();

						colorBandSet.MarkAsDirty();
						ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(colorBandSet, isPreview: true));
					}

					OnPropertyChanged(nameof(ICbsHistogramViewModel.UseRealTimePreview));
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
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is turning {strState} the High Lighting the Selected Color Band.");
					_highlightSelectedBand = value;

					OnPropertyChanged(nameof(HighlightSelectedBand));
				}
			}
		}

		public bool UsePercentagesLocalSetting
		{
			get => _usePercentagesLocalSetting;
			set
			{
				if (value != _usePercentagesLocalSetting)
				{
					_usePercentagesLocalSetting = value;
					OnPropertyChanged(nameof(UsePercentagesLocalSetting));

					UsePercentagesGlobalSetting = UsePercentagesLocalSetting;

					PercentageUseStatus = GetPercentageUseStatus(_currentColorBandSet.UsingPercentages, UsePercentagesLocalSetting, _mapSectionHistogramProcessor.Histogram);
				}
			}
		}

		public bool UsePercentagesGlobalSetting
		{
			get => _usePercentagesGlobalSetting;
			private set
			{
				if (value != _usePercentagesGlobalSetting)
				{
					_usePercentagesGlobalSetting = value;
					OnPropertyChanged(nameof(UsePercentagesGlobally));
				}
			}
		}

		public bool UsePercentagesGlobally
		{
			get => _usePercentagesGlobally;
			set
			{
				if (value != _usePercentagesGlobally)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel is updating UsePercentagesGlobally from {GetDefaultUsePercentagesStrVal(_usePercentagesGlobally)} to {GetDefaultUsePercentagesStrVal(value)}.");

					_usePercentagesGlobally = value;

					OnPropertyChanged(nameof(UsePercentagesGlobally));
					OnPropertyChanged(nameof(PercentageUseIsGlobalDisplayStr));

					if (value)
					{
						UsePercentagesGlobalSetting = UsePercentagesLocalSetting;
					}
				}
			}
		}

		public string PercentageUseIsGlobalDisplayStr
		{ 
			get
			{
				return UsePercentagesGlobally ? "Global" : string.Empty;
			}
		}

		public string PercentageUseStatus
		{
			get => _percentageUseStatus;
			private set
			{
				if (value != _percentageUseStatus)
				{
					_percentageUseStatus = value;
					OnPropertyChanged(nameof(PercentageUseStatus));
				}
			}
		}

		private string GetPercentageUseStatus(bool currentUsingPercentages, bool targetUsingPercentages, IHistogram histogram) 
		{
			var numberOfHistogramValues = histogram.Values.Count(x => x != 0);
			if (numberOfHistogramValues == 0)
			{
				return string.Empty;
			}
			else
			{
				return currentUsingPercentages == targetUsingPercentages ? "Locked" : "Pending";
			}
		}

		private string GetPercentageUseStatus(bool currentUsingPercentages, bool targetUsingPercentages)
		{
			return currentUsingPercentages == targetUsingPercentages ? "Locked" : "Pending";
		}

		//public bool CurrentUsingPercentages
		//{
		//	get => _currentColorBandSet.UsingPercentages;
		//	set
		//	{
		//		if (value != _currentColorBandSet.UsingPercentages)
		//		{
		//			_currentColorBandSet.UsingPercentages = value;

		//			if (UsePercentagesGlobally)
		//			{
		//				UsePercentagesGlobalSetting = value;
		//			}

		//			OnPropertyChanged(nameof(CurrentUsingPercentages));
		//		}
		//	}
		//}

		public ListCollectionView ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				var valueIsNew = value != _colorBandsView;
				Debug.WriteLine($"The CbsHistogramViewModel is getting a new ColorBandsView. ValueIsNew is {valueIsNew}.");

				_colorBandsView.CurrentChanged -= ColorBandsView_CurrentChanged;

				_colorBandsView = value;

				CurrentColorBand = _colorBandsView.CurrentItem as ColorBand;

				OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsView));
				OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBandIndex));
				OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBandNumber));
				OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsCount));

				_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
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

					if (_currentColorBandSet != null)
					{
						if (_currentColorBandSet.HighlightedColorBandIndex != ColorBandsView.CurrentPosition)
						{
							Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:ColorBandsView_CurrentChanged. Setting the HighlightedColorBandIndex from: {ColorBandSet.HighlightedColorBandIndex} to the ColorBandsView's CurrentPosition: {ColorBandsView.CurrentPosition}.");

							_currentColorBandSet.HighlightedColorBandIndex = ColorBandsView.CurrentPosition;
						}
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
			get
			{
				//var result = _isDirty;

				var altResult = _colorBandSetHistoryCollection.CurrentIndex != 0;
				//Debug.Assert(altResult == result, "The CbsHistogramViewModel's IsDirty flag is set, however the ColorBandHistoryCollection has no pending changes.");

				return altResult;
			}

			private set
			{
				if (value != _isDirty)
				{
					_isDirty = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.IsDirty));
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

					_mapSectionHistogramProcessor.ProcessingEnabled = value;

					if (_isEnabled)
					{
						_mapSectionHistogramProcessor.NumberOfSectionsProcessed = 0;
						ClearDisplay();
					}

					OnPropertyChanged(nameof(ICbsHistogramViewModel.IsEnabled));
				}
			}
		}

		//public Visibility WindowVisibility
		//{
		//	get => _windowVisibility;
		//	set
		//	{
		//		if (value != _windowVisibility)
		//		{
		//			_windowVisibility = value;
		//			IsEnabled = _windowVisibility == Visibility.Visible ? true : false;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

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
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's DisplayZoom is being updated to it's current value: {value}. No Change.");
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

		public bool ColorBandUserControlHasErrors
		{
			get => _colorBandUserControlHasErrors;
			set
			{
				if (value != _colorBandUserControlHasErrors)
				{
					_colorBandUserControlHasErrors = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandUserControlHasErrors));
				}
			}
		}

		#endregion

		#region Public Methods 

		//public void ClearSelectedItems()
		//{
		//	SelectedItems.Clear();
		//	_selectedItemsArray = null;
		//}

		public void AdvanceEditMode()
		{
			if (CurrentCbEditMode == ColorBandSetEditMode.Cutoffs)
			{
				CurrentCbEditMode = ColorBandSetEditMode.Colors;
			}
			else if (CurrentCbEditMode == ColorBandSetEditMode.Colors)
			{
				CurrentCbEditMode = ColorBandSetEditMode.Bands;
			}
			else
			{
				CurrentCbEditMode = ColorBandSetEditMode.Cutoffs;
			}
		}

		public void RetardEditMode()
		{
			if (CurrentCbEditMode == ColorBandSetEditMode.Cutoffs)
			{
				CurrentCbEditMode = ColorBandSetEditMode.Bands;
			}
			else if (CurrentCbEditMode == ColorBandSetEditMode.Colors)
			{
				CurrentCbEditMode = ColorBandSetEditMode.Cutoffs;
			}
			else
			{
				CurrentCbEditMode = ColorBandSetEditMode.Colors;
			}
		}

		public bool TryMoveCurrentColorBandToNext()
		{
			if (ColorBandUserControlHasErrors) return false;

			if (CurrentColorBandIndex > ColorBandsCount - 2) return false;
			var result = _colorBandsView.MoveCurrentToNext();
			return result;
		}

		public bool TryMoveCurrentColorBandToPrevious()
		{
			if (ColorBandUserControlHasErrors) return false;

			if (CurrentColorBandIndex < 1) return false;

			var result = _colorBandsView.MoveCurrentToPrevious();
			return result;
		}

		public void ApplyChanges(int newTargetIterations)
		{
			//if (!IsEnabled) return;

			if (newTargetIterations != _currentColorBandSet.HighCutoff)
			{
				var newSet = ColorBandSetHelper.AdjustTargetIterations(_currentColorBandSet, newTargetIterations);

				var unscaledWidth = GetExtent(newSet);

				if (unscaledWidth > 10)
				{
					ResetView(unscaledWidth, DisplayPosition, DisplayZoom);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel::ApplyChanges is not resetting the view -- the new iterations target <= 10.");
				}

				_mapSectionHistogramProcessor.Reset(newSet.HighCutoff);

				ApplyChangesInt(newSet);
			}
		}

		public void ApplyChanges()
		{
			//if (!IsEnabled) return;

			Debug.Assert(IsDirty, "ColorBandSetViewModel:ApplyChanges is being called, but we are not dirty.");

			Debug.Assert(_currentColorBandSet.IsDirty, "ColorBandSetViewModel:ApplyChanges is being called, but the current ColorBandSet is not dirty.");

			var newSet = _currentColorBandSet.CreateNewCopy();

			ApplyChangesInt(newSet);
		}

		private void ApplyChangesInt(ColorBandSet newSet)
		{
			Debug.WriteLineIf(_traceCBSVersions, $"The ColorBandSetViewModel is Applying changes. The new Id is {newSet.Id}/{newSet.LastUpdatedUtc}, name: {newSet.Name}. The old Id is {ColorBandSet.Id}/{ColorBandSet.LastUpdatedUtc}");

			//Debug.WriteLine($"The new ColorBandSet: {newSet}");
			//Debug.WriteLine($"The existing ColorBandSet: {_colorBandSet}");

			_colorBandSet = newSet;

			// Clear all existing items from history and add the new set.
			_colorBandSetHistoryCollection.Load(_colorBandSet);
			IsDirty = false;

			var curPos = CurrentColorBandIndex;

			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();
			UpdateViewAndRaisePropertyChangeEvents(curPos);

			ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_colorBandSet, isPreview: false));
		}

		public void RevertChanges()
		{
			// Remove all but the first entry from the History Collection
			_colorBandSetHistoryCollection.Trim(0);
			IsDirty = false;

			var curPos = CurrentColorBandIndex;
			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();
			UpdateViewAndRaisePropertyChangeEvents(curPos);

			ApplyHistogram(histogramIsFromACompleteMap: false);

			if (UseRealTimePreview)
			{
				ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
			}
		}

		public IDictionary<int, int> GetHistogramForColorBand(ColorBand colorBand)
		{
			var previousCutoff = colorBand.PreviousCutoff ?? 0;
			var cutoff = colorBand.Cutoff;

			var kvpsForBand = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(previousCutoff, cutoff, includeCatchAll: true);

			return new Dictionary<int, int>(kvpsForBand);
		}

		public bool ApplyHistogram(bool histogramIsFromACompleteMap)
		{
			var histCutoffsSnapShot = GetHistCutoffsSnapShot(_mapSectionHistogramProcessor.Histogram, histogramIsFromACompleteMap, _currentColorBandSet);

			var result = ApplyHistogram(histCutoffsSnapShot);
			return result;
		}

		public ReservedColorBand PopReservedColorBand()
		{
			var result = _currentColorBandSet.PopReservedColorBand();
			return result;
		}

		public void PushReservedColorBand(ReservedColorBand reservedColorBand)
		{
			_currentColorBandSet.PushReservedColorBand(reservedColorBand);
		}

		#endregion

		#region Public Methods Insertions

		public bool TestInsertItem(int colorBandIndex)
		{
			if (CurrentColorBand == null || ColorBandUserControlHasErrors)
			{
				return false;
			}

			switch (CurrentCbEditMode)
			{
				case ColorBandSetEditMode.Cutoffs:
					var colorBand = _currentColorBandSet[colorBandIndex];

					if (colorBand.BucketWidth < 2)
					{
						Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
						return false;
					}

					break;

				case ColorBandSetEditMode.Colors:
					break;

				case ColorBandSetEditMode.Bands:
					colorBand = _currentColorBandSet[colorBandIndex];

					if (colorBand.BucketWidth < 2)
					{
						Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
						return false;
					}
					break;

				default:
					throw new InvalidOperationException($"{CurrentCbEditMode} is not recognized.");
			}

			return true;
		}

		public void CompleteCutoffInsertion(int index, ColorBand colorBand, ReservedColorBand reservedColorBand)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. Before CutoffInsertion, the current position is {ColorBandsView.CurrentPosition}.");

			_disableProcessCurColorBandPropertyChanges = true;

			var result = TryInsertColorBand(index, colorBand);

			if (!result)
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel. Could not CompleteCutoffInsertion.");
				return;
			}

			result = TryDeleteColor(index, reservedColorBand);

			if (!result)
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel. Could not CompleteColorRemoval.");
				return;
			}

			_disableProcessCurColorBandPropertyChanges = false;

			if (_colorBandsView.CurrentPosition != index)
			{
				_colorBandsView.MoveCurrentToPosition(index);
			}
			else
			{

				CurrentColorBand = _currentColorBandSet[index];
				_colorBandsView.MoveCurrentTo(CurrentColorBand);
			}

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. After CutoffInsertion, the current position is {ColorBandsView.CurrentPosition}. The newIndex is {index}.");
			OnCurrentColorBandSetUpdated();
		}

		public ReservedColorBand CompleteColorInsertion(int index, ColorBand colorBand)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. CompleteColorInsertion has been callled.");

			var result = InsertColor(index, colorBand);

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. After ColorInsertion, the current position is {ColorBandsView.CurrentPosition}. The newIndex is {index}.");

			OnCurrentColorBandSetUpdated();

			return result;
		}

		private ReservedColorBand InsertColor(int index, ColorBand colorBand)
		{
			_disableProcessCurColorBandPropertyChanges = true;
			var result = _currentColorBandSet.InsertColor(index, colorBand);
			_disableProcessCurColorBandPropertyChanges = false;

			return result;
		}

		public void CompleteBandInsertion(int index, ColorBand colorBand)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. CompleteBandInsertion has been callled.");

			_disableProcessCurColorBandPropertyChanges = true;

			var result = TryInsertColorBand(index, colorBand);

			_disableProcessCurColorBandPropertyChanges = false;

			if (!result)
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel. Could not CompleteBandInsertion.");
				return;
			}

			if (_colorBandsView.CurrentPosition != index)
			{
				_colorBandsView.MoveCurrentToPosition(index);
			}
			else
			{
				CurrentColorBand = _currentColorBandSet[index];
			}

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. After BandInsertion, the current position is {ColorBandsView.CurrentPosition}. The newIndex is {index}.");

			OnCurrentColorBandSetUpdated();
		}

		private bool TryInsertColorBand(int index, ColorBand colorBand)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:About to Insert item at index: {index}. The new ColorBand is: {colorBand}.");

			lock (_histLock)
			{
				_currentColorBandSet.Insert(index, colorBand);

				// TODO: Consider updating the following ColorBand's PreviousCutoff instead of calling UpdateItemsAndNeighbors
				// This is the only reference to UpdateItemsAndNeighbors
				// AND is the only use of ColorBand.UpdateNeighbors.
				_currentColorBandSet.UpdateItemAndNeighbors(index, colorBand);
			}

			return true;
		}

		//private bool TryInsertColorBand(int index, ColorBand colorBand)
		//{
		//	if (colorBand.Cutoff - colorBand.StartingCutoff < 2)
		//	{
		//		Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
		//		return false;
		//	}

		//	var prevCutoff = colorBand.PreviousCutoff ?? 0;
		//	var newWidth = (colorBand.Cutoff - prevCutoff) / 2;
		//	var newCutoff = prevCutoff + newWidth;
		//	var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.Next, colorBand.StartColor, colorBand.PreviousCutoff, colorBand.StartColor, double.NaN);

		//	Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:About to Insert item at index: {index}. The new ColorBand is: {newItem}.");

		//	lock (_histLock)
		//	{
		//		_currentColorBandSet.Insert(index, newItem);
		//		//_currentColorBandSet.UpdateItemAndNeighbors(index, newItem);
		//	}

		//	return true;
		//}

		#endregion

		#region Public Methods - Deletions

		public bool TestDeleteItem(int colorBandIndex)
		{
			bool result;

			switch (CurrentCbEditMode)
			{
				case ColorBandSetEditMode.Cutoffs:

					// Cannot delete the last entry
					result = colorBandIndex <= _currentColorBandSet.Count - 2;
					break;

				case ColorBandSetEditMode.Colors:
					// Cannot delete the last entry

					result = colorBandIndex <= _currentColorBandSet.Count - 2;
					break;

				case ColorBandSetEditMode.Bands:

					if (_colorBandsView.Count < 1)
					{
						// There is only one ColorBand remaining. 
						result = false;
					}
					else if (colorBandIndex > _currentColorBandSet.Count - 1)
					{
						// Cannot delete the last entry
						result = false;
					}
					else
					{
						result = true;
					}
					break;

				default:
					throw new InvalidOperationException($"{CurrentCbEditMode} is not recognized.");
			}

			return result;
		}

		public ReservedColorBand? CompleteCutoffRemoval(int index)
		{
			var selItem = _currentColorBandSet[index];

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. CompleteCutoffRemoval has been callled for index = {index} with Cutoff = {selItem.Cutoff}.");

			var percentage = selItem.Percentage;
			var result = TryDeleteStartingCutoff(selItem, out var reservedColorBand);

			if (!result)
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel. Could not CompleteCutoffRemoval.");
				return null;
			}

			if (index == 0)
			{
				var cb = _currentColorBandSet[index];
				cb.PreviousCutoff = 0;
				cb.Percentage += percentage;
			}
			else
			{
				var cb = _currentColorBandSet[index - 1];
				var newCutoff = _currentColorBandSet[index].PreviousCutoff ?? 0;

				//Debug.WriteLine($"Extending the width of the previous item: index={index - 1} from {cb.Cutoff} to newCutoff: {newCutoff} compare to the cutoff of the band being removed: {cutOff}.");
				//var sC = selItem.StartColor;
				//var newSc = _currentColorBandSet[index].StartColor;
				//Debug.WriteLine($"The successor start color of band just before the band being removed is {cb.SuccessorStartColor}, the start color of the band immed after the one being removed is {newSc}.");

				cb.Cutoff = newCutoff;
				cb.Percentage += percentage;
			}

			if (index > 0)
				index--;

			if (_colorBandsView.CurrentPosition != index)
			{
				_colorBandsView.MoveCurrentToPosition(index);
			}
			else
			{
				CurrentColorBand = _currentColorBandSet[index];
			}

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel.After CutoffRemoval, the current position is {ColorBandsView.CurrentPosition}.");

			OnCurrentColorBandSetUpdated();

			ReportRemoveCurrentItem(index);

			return reservedColorBand;
		}

		private bool TryDeleteStartingCutoff(ColorBand colorBand, out ReservedColorBand? reservedColorBand)
		{
			_disableProcessCurColorBandPropertyChanges = true;
			try
			{
				lock (_histLock)
				{
					return _currentColorBandSet.DeleteStartingCutoff(colorBand, out reservedColorBand);
				}
			}
			finally
			{
				_disableProcessCurColorBandPropertyChanges = false;
			}
		}

		public void CompleteColorRemoval(int index, ReservedColorBand reservedColorBand)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. CompleteColorRemoval has been callled.");

			var result = TryDeleteColor(index, reservedColorBand);

			if (!result)
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel. Could not CompleteColorRemoval.");
				return;
			}

			if (index > 0)
			{
				_currentColorBandSet[index - 1].SuccessorStartColor = _currentColorBandSet[index].StartColor;
			}

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. After ColorRemoval, the current position is {ColorBandsView.CurrentPosition}.");

			OnCurrentColorBandSetUpdated();
			//ReportRemoveCurrentItem(index);
		}

		private bool TryDeleteColor(int index, ReservedColorBand reservedColorBand)
		{
			if (index > _currentColorBandSet.Count - 2)
			{
				// Cannot delete the last entry
				return false;
			}

			_disableProcessCurColorBandPropertyChanges = true;
			_currentColorBandSet.DeleteColor(index, reservedColorBand);
			_disableProcessCurColorBandPropertyChanges = false;

			return true;
		}

		public void CompleteBandRemoval(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. CompleteBandRemoval has been callled.");
			var selItem = _currentColorBandSet[index];
			var percentage = selItem.Percentage;

			var result = TryDeleteBand(selItem);

			if (!result)
			{
				Debug.WriteLine("WARNING: Could not CompleteBandRemoval.");
				return;
			}

			if (_currentColorBandSet.Count == 1)
			{
				var singleCb = _currentColorBandSet[0];
				singleCb.PreviousCutoff = null;
				singleCb.BlendStyle = ColorBandBlendStyle.Next;
				singleCb.IsLast = true;
				singleCb.Percentage = 100;
			}
			else
			{
				var cb = _currentColorBandSet[index];
				cb.PreviousCutoff = selItem.PreviousCutoff;
				cb.Percentage += percentage;
			}

			if (_colorBandsView.CurrentPosition != index)
			{
				_colorBandsView.MoveCurrentToPosition(index);
			}
			else
			{
				CurrentColorBand = _currentColorBandSet[index];
			}

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. After BandRemoval, the current position is {ColorBandsView.CurrentPosition}.");

			OnCurrentColorBandSetUpdated();

			ReportRemoveCurrentItem(index);
		}

		private bool TryDeleteBand(ColorBand colorBand)
		{
			if (_colorBandsView.Count < 2)
			{
				// There is only one ColorBand remaining. 
				return false;
			}

			var index = _currentColorBandSet.IndexOf(colorBand);

			if (index >= _currentColorBandSet.Count - 1)
			{
				// Cannot delete the last entry
				return false;
			}

			_disableProcessCurColorBandPropertyChanges = true;
			try
			{
				lock (_histLock)
				{
					return _currentColorBandSet.Remove(colorBand);
				}
			}
			finally
			{
				_disableProcessCurColorBandPropertyChanges = false;
			}
		}

		#endregion

		#region Public Methods - Plotting

		public void RefreshDisplay()
		{
			lock (_paintLocker)
			{
				var values = _mapSectionHistogramProcessor.Histogram.Values;

				BuildSeriesData(SeriesData, values);
			}

			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's SerieData is being updated. There are {SeriesData.LongLength} entries.");

			SeriesData = new HPlotSeriesData(SeriesData);
			//OnPropertyChanged(nameof(ICbsHistogramViewModel.SeriesData));
		}

		public int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"\nCbsHistogramViewModel is having its ViewportSizePosAndScale set to size:{contentViewportSize}, offset:{contentOffset}, scale:{contentScale}.");
			Debug.WriteLineIf(_useDetailedDebug, $"\nCbsHistogramViewModel is having its ViewportSizePosAndScale set to scale:{contentScale}, size:{contentViewportSize.Width}, offset:{contentOffset.X}.");

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
				SeriesData = new HPlotSeriesData(SeriesData);
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
				_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();
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
				_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();
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

		private void ColorBandsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:ColorBandsView_CollectionChanged. Action is {e.Action}. NewItemsCount: {e.NewItems?.Count ?? -1}. OldItemsCount: {e.OldItems?.Count ?? -1}.");

			OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsCount));
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			if (ColorBandsView != null)
			{
				CurrentColorBand = (ColorBand)ColorBandsView.CurrentItem;
			}
		}

		private void CurrentColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (_disableProcessCurColorBandPropertyChanges)
			{
				Debug.WriteLineIf(_useDetailedDebug, "ColorBandSetViewModel. Not handling CurrentColorBand_PropertyChanged, _disableProcessCurColorBandPropertyChanges is true.");
				return;
			}

			if (sender is ColorBand colorBandToUpdate)
			{
				//Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:CurrentColorBand Prop: {e.PropertyName} is changing.");
			}
			else
			{
				Debug.WriteLine($"ColorBandSetViewModel: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");
				return;
			}

			var foundUpdate = ProcessColorBandUpdate(colorBandToUpdate, e.PropertyName);

			if (foundUpdate)
			{
				_currentColorBandSet.MarkAsDirty();

				// Don't include each property change when the Current ColorBand is being edited.
				if (!colorBandToUpdate.IsInEditMode)
				{
					PushCurrentColorBandOnToHistoryCollection();
					IsDirty = true;
				}

				if (UseRealTimePreview)
				{
					//if (!_currentColorBandSet.IsDirty)
					//{
					//	Debug.WriteLine("WARNINIG: CbsHistogramViewModel is Marking the Current ColorBandSet as Dirty on CurrentColorBand_PropertyChanged.");
					//	_currentColorBandSet.MarkAsDirty();
					//}

					var newColorBandSet = _currentColorBandSet.CreateNewCopy();

					Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel. Calling RaiseUpdateRequestThrottled.");
					RaiseUpdateRequestThrottled(newColorBandSet);
				}
			}
			else
			{
				if (e.PropertyName == nameof(ColorBand.IsSelected))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. IsSelected at index: {CurrentColorBandIndex} is now {colorBandToUpdate.IsSelected}.");
				}
			}
		}

		private bool ProcessColorBandUpdate(ColorBand cb, string? propertyName)
		{
			bool foundUpdate;

			// StartColor is being updated.
			if (propertyName == nameof(ColorBand.StartColor))
			{
				if (TryGetPredeccessor(_currentColorBandSet, cb, out var predecessorColorBand))
				{
					predecessorColorBand.SuccessorStartColor = cb.StartColor;
				}

				foundUpdate = true;
			}

			// Cutoff is being updated
			else if (propertyName == nameof(ColorBand.Cutoff))
			{
				if (cb.BucketWidth < 1)
				{
					throw new ArgumentOutOfRangeException("BucketWidth is < 1");
				}

				if (TryGetSuccessor(_currentColorBandSet, cb, out var successorColorBand))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Cutoff was updated. CbsHistogramViewModel is updating the PreviousCutoff for ColorBand: {_colorBandsView.IndexOf(cb)}");
					successorColorBand.PreviousCutoff = cb.Cutoff;

					if (successorColorBand.BucketWidth < 1)
					{
						throw new ArgumentOutOfRangeException("The next ColorBand's BucketWidth is < 1");
					}
				}

				foundUpdate = true;
				UpdatePercentages(_mapSectionHistogramProcessor.Histogram);
			}

			// Previous Cutoff is being updated
			else if (propertyName == nameof(ColorBand.PreviousCutoff))
			{
				if (cb.BucketWidth < 1)
				{
					throw new ArgumentOutOfRangeException("BucketWidth is < 1");
				}

				if (TryGetPredeccessor(_currentColorBandSet, cb, out var predecessorColorBand))
				{
					if (cb.PreviousCutoff == null)
					{
						throw new InvalidOperationException("The PreviousCutoff is null, however we are not the first ColorBand.");
					}

					Debug.WriteLineIf(_useDetailedDebug, $"PreviousCutoff was updated. CbsHistogramViewModel is updating the Cutoff for ColorBand: {_colorBandsView.IndexOf(cb)}");

					predecessorColorBand.Cutoff = cb.PreviousCutoff.Value;

					if (predecessorColorBand.BucketWidth < 1)
					{
						throw new ArgumentOutOfRangeException("The predecessor ColorBand's BucketWidth is < 1");
					}
				}

				foundUpdate = true;
				UpdatePercentages(_mapSectionHistogramProcessor.Histogram);
			}

			// BlendStyle is being updated
			else if (propertyName == nameof(ColorBand.BlendStyle))
			{
				//cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? cb.SuccessorStartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;
				foundUpdate = true;
			}

			// EndColor is being updated
			else if (propertyName == nameof(ColorBand.EndColor))
			{
				foundUpdate = cb.BlendStyle == ColorBandBlendStyle.End;
			}
			else
			{
				// Some other property is being updated.
				foundUpdate = false;
			}

			return foundUpdate;
		}

		private void RaiseUpdateRequestThrottled(ColorBandSet colorBandSet)
		{
			_selectionLineMovedDispatcher.Throttle(
				interval: SELECTION_LINE_UPDATE_THROTTLE_INTERVAL,
				action: parm =>
				{
					ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(colorBandSet, isPreview: true));
				},
				param: null);
		}

		private void CurrentColorBand_EditEnded(object? sender, EventArgs e)
		{
			ReportIndexOfSender(sender);

			PushCurrentColorBandOnToHistoryCollection();
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
				Debug.WriteLine($"WARNING: ColorBandSetViewModel. The Histogram is empty (BuildSeriesData).");

				hPlotSeriesData.Clear();
				return;
			}

			var existingLength = hPlotSeriesData.LongLength;
			hPlotSeriesData.SetYValues(values, out var bufferWasPreserved);

			ReportSeriesBufferAllocation(existingLength, newLength, bufferWasPreserved);
		}

		private int GetExtent(ColorBandSet colorBandSet)
		{
			var result = colorBandSet.Count < 1 ? 0 : colorBandSet.HighCutoff;
			return result;
		}

		private void ResetView(double extentWidth, VectorDbl displayPosition, double displayZoom)
		{
			var extent = new SizeDbl(extentWidth, _viewportSize.Height);

			DisplaySettingsInitializedEventArgs initialSettings;

			if (ScreenTypeHelper.IsDoubleChanged(extentWidth, UnscaledExtent.Width))
			{
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is raising the DisplaySettingsInitialized Event and resetting the Position and Scale.");

				// The ExtentWidth is changing -- reset the position and scale.
				var positionZero = new VectorDbl();
				var zoomUnity = 1d;

				initialSettings = new DisplaySettingsInitializedEventArgs(extent, positionZero, zoomUnity);
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is raising the DisplaySettingsInitialized Event and keeping the Position and Scale.");
				initialSettings = new DisplaySettingsInitializedEventArgs(extent, displayPosition, displayZoom);
			}

			UnscaledExtent = new SizeDbl(); // This will ensure that we process the update when this is set after raising the DisplaySettingsInitialized event.

			DisplaySettingsInitialized?.Invoke(this, initialSettings);

			// Trigger a ViewportChanged event on the PanAndZoomControl -- this will result in our UpdateViewportSizeAndPos method being called.
			Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is setting the Unscaled Extent to complete the process of initializing the Histogram Display.");
			UnscaledExtent = extent;
		}

		#endregion

		#region Private Methods - ColorBandsView

		private void OnCurrentColorBandSetUpdated()
		{
			PushCurrentColorBandOnToHistoryCollection();

			ApplyHistogram(histogramIsFromACompleteMap: false);

			if (UseRealTimePreview)
			{
				ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(_currentColorBandSet, isPreview: true));
			}
		}

		private void UpdateViewAndRaisePropertyChangeEvents(int? currentColorBandIndex = null)
		{
			//_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();

			if (ColorBandsView is INotifyCollectionChanged t1) t1.CollectionChanged -= ColorBandsView_CollectionChanged;

			var newView = BuildColorBandsView(_currentColorBandSet);

			if (currentColorBandIndex != null)
			{
				newView.MoveCurrentToPosition(currentColorBandIndex.Value);
			}
			else
			{
				newView.MoveCurrentToFirst();
			}

			ColorBandsView = newView;

			if (ColorBandsView is INotifyCollectionChanged t2) t2.CollectionChanged += ColorBandsView_CollectionChanged;

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
			// Push the current copy and make a new copy for any further changes.
			//var currentVal = _currentColorBandSet;
			//_currentColorBandSet = _currentColorBandSet.CreateNewCopy();
			//_colorBandSetHistoryCollection.Push(currentVal);

			var newVal = _currentColorBandSet.CreateNewCopy();
			_colorBandSetHistoryCollection.Push(newVal);
			IsDirty = true;

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

		private List<ColorBand> GetSelectedItems(IList<ColorBand> colorBands)
		{
			var result = colorBands.Where(c => c.IsSelected).ToList();
			return result;
		}

		#endregion

		#region Private Methods - Percentages

		private bool ApplyHistogram(HistCutoffsSnapShot histCutoffsSnapShot)
		{
			Debug.WriteLine($"ApplyHistogram is being called with a HistCutoffSnapShot with ColorBandSetId: {histCutoffsSnapShot.ColorBandSetId}. SomeNaN = {histCutoffsSnapShot.NoPercentageIsNaN}. AllZero = {histCutoffsSnapShot.AtLeastOnePercentageIsNonZero}.");
			if (histCutoffsSnapShot.HistKeyValuePairs.Length > 0)
			{
				// If the Default is set, us it, otherwise use the value from the current ColorBandSet.
				var currentlyUsingPercentages = UsePercentagesLocalSetting;

				if (currentlyUsingPercentages)
				{
					if (histCutoffsSnapShot.UsingPercentages/* || histCutoffsSnapShot.HavePercentages*/)
					{
						// Cutoffs are adjusted based on Percentages
						UpdateCutoffsCheckThread(histCutoffsSnapShot, out _, out _);
					}
					else
					{
						Debug.WriteLine($"WARNING: ColorBandSetViewModel. Percentage Values are unavailable. Using Cutoffs to rebuild the Percentages. NoPercentagesIsNaN = {histCutoffsSnapShot.NoPercentageIsNaN}. AtLeastOnePercentageIsNonZero = {histCutoffsSnapShot.AtLeastOnePercentageIsNonZero}.");

						// 'Rebuild' the percentage values from the current Cutoff values.
						if (UpdatePercentages(histCutoffsSnapShot, out var newPercentages, out var resultsAreComplete))
						{
							if (resultsAreComplete)
							{
								_currentColorBandSet.UsingPercentages = true;
								_currentColorBandSet.MarkAsDirty();

								PercentageUseStatus = GetPercentageUseStatus(_currentColorBandSet.UsingPercentages, UsePercentagesLocalSetting);

								UpdateAssignedColorBandSetWithNewPercentages(newPercentages);
							}
						}
					}
				}
				else
				{
					if (histCutoffsSnapShot.UsingCutoffs)
					{
						// Percentages are adjusted based on Cutoffs
						UpdatePercentages(histCutoffsSnapShot, out _, out _);
					}
					else
					{
						Debug.WriteLine($"WARNING: ColorBandSetViewModel. Cutoff Values are unavailable. Using Percentages to rebuild the Cutoffs. ");

						if (UpdateCutoffsCheckThread(histCutoffsSnapShot, out var newCutoffBands, out var resultsAreComplete))
						{
							if (resultsAreComplete)
							{
								_currentColorBandSet.UsingPercentages = false;
								_currentColorBandSet.MarkAsDirty();

								PercentageUseStatus = GetPercentageUseStatus(_currentColorBandSet.UsingPercentages, UsePercentagesLocalSetting);

								UpdateAssignedColorBandSetWithNewOffsets(newCutoffBands);
							}
						}
					}
				}

				return true;
			}
			else
			{
				// The histogram is empty.

				//ClearPercentages();
				return false;
			}
		}

		private void UpdatePercentages(IHistogram histogram)
		{
			var histCutoffsSnapShot = GetHistCutoffsSnapShot(histogram, histogramIsFromACompleteMap: false, _currentColorBandSet);

			UpdatePercentages(histCutoffsSnapShot, out _, out _);
		}

		private bool UpdatePercentages(HistCutoffsSnapShot histCutoffsSnapShot, [NotNullWhen(true)] out PercentageBand[]? percentageBands, out bool resultsAreComplete)
		{
			bool result;

			// Percentages are adjusted based on Cutoffs
			if (ColorBandSetHelper.TryGetPercentagesFromCutoffs(histCutoffsSnapShot, out percentageBands, out resultsAreComplete))
			{
				result = ApplyNewPercentages(percentageBands, histCutoffsSnapShot.ColorBandSetId);
			}
			else
			{
				percentageBands = null;
				result = false;
			}

			return result;
		}

		private bool ApplyNewPercentages(PercentageBand[] newPercentages, ObjectId colorBandSetId)
		{
			bool result;

			lock (_histLock)
			{
				if (_currentColorBandSet.Id != colorBandSetId)
				{
					Debug.WriteLine("The HistCutoffsSnapShot is stale, not Applying the New Percentages.");
				}

				if (_currentColorBandSet.UpdatePercentagesCheckOffsets(newPercentages))
				{
					result = true;

					ReportNewPercentages(newPercentages);

					BeyondTargetSpecs = newPercentages[^1];
					var numberReachedTargetIteration = BeyondTargetSpecs.Count;
					var total = BeyondTargetSpecs.RunningSum;
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. received new percentages. Top Count: {numberReachedTargetIteration}, Total: {total}.");
				}
				else
				{
					BeyondTargetSpecs = null;
					result = false;
				}
			}

			return result;
		}

		private void UpdateAssignedColorBandSetWithNewPercentages(PercentageBand[] newPercentages)
		{
			var percentagesWereUpdated = ColorBandSet.UpdatePercentagesCheckOffsets(newPercentages);
			if (!percentagesWereUpdated)
			{
				Debug.WriteLine($"WARNING: CbsHistogramViewModel. Unable to update the ColorBandSet's percentages.");
			}
			else
			{
				_colorBandSetHistoryCollection[0] = ColorBandSet;
				ColorBandSet.UsingPercentages = true;
				ColorBandSet.MarkAsDirty();
			}
		}

		private void UpdateAssignedColorBandSetWithNewOffsets(CutoffBand[] newCutoffBands)
		{
			var cutoffsWereUpdated = ColorBandSet.UpdateCutoffs(newCutoffBands);
			if (!cutoffsWereUpdated)
			{
				Debug.WriteLine($"WARNING: CbsHistogramViewModel. Unable to update the ColorBandSet's Cutoffs.");
			}
			else
			{
				_colorBandSetHistoryCollection[0] = ColorBandSet;
				ColorBandSet.UsingPercentages = false;
				ColorBandSet.MarkAsDirty();
			}
		}

		private void ClearPercentages()
		{
			lock (_histLock)
			{
				_currentColorBandSet.ClearPercentages();
				BeyondTargetSpecs = null;
			}
		}

		private bool UpdateCutoffsCheckThread(HistCutoffsSnapShot histCutoffsSnapShot, [NotNullWhen(true)] out CutoffBand[]? cutoffBands, out bool resultsAreComplete)
		{
			var dispatcher = _selectionLineMovedDispatcher.Dispatcher;

			if (!dispatcher.CheckAccess())
			{
				Debug.WriteLine("CbsHistogramViewModel switching to Ui Thread to update Cutoffs.");
				var x = dispatcher.Invoke(UpdateCutoffs, new object[] { histCutoffsSnapShot });

				var y = (ValueTuple<CutoffBand[]?, bool>) x; 

				if (y.Item1 != null)
				{
					cutoffBands = y.Item1;
					resultsAreComplete = y.Item2;
					return true;
				}
				else
				{
					cutoffBands = null;
					resultsAreComplete = false;
					return false;
				}
			}
			else
			{
				Debug.WriteLine("CbsHistogramViewModel already on the Ui Thread to update Cutoffs.");
				//UpdateCutoffsGetResults(histCutoffsSnapShot, out _, out _);
				(CutoffBand[]? cutoffBands1, bool resultsAreComplete1) = UpdateCutoffs(histCutoffsSnapShot);
				cutoffBands = cutoffBands1;
				resultsAreComplete = resultsAreComplete1;
				return cutoffBands != null;
			}
		}

		//private void UpdateCutoffs(HistCutoffsSnapShot histCutoffsSnapShot)
		//{
		//	if (histCutoffsSnapShot.ColorBandSetId != _currentColorBandSet.Id)
		//	{
		//		Debug.WriteLine("ColorBandSetViewModel.The HistCutoffsSnapShot is stale, not Updating the New Cutoffs.");
		//	}

		//	// TODO: Do not Apply the new Cutoffs if there was some problem getting the Cutoffs from the current Percentage values
		//	// This may be caused by an incomplete histogram.
		//	// Update the TryGetCutoffsFromPercentages to report if there any problems.

		//	bool result;

		//	if (ColorBandSetHelper.TryGetCutoffsFromPercentages(histCutoffsSnapShot, out var cutoffBands, out bool resultsAreComplete))
		//	{
		//		CheckNewCutoffs(histCutoffsSnapShot.PercentageBands, cutoffBands);
		//		ReportNewCutoffs(histCutoffsSnapShot, histCutoffsSnapShot.PercentageBands, cutoffBands);

		//		result = ApplyNewCutoffs(cutoffBands, histCutoffsSnapShot.ColorBandSetId);

		//		var newColorBandSet = _currentColorBandSet.CreateNewCopy();
		//		ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(newColorBandSet, isPreview: true));
		//	}
		//	//else
		//	//{
		//	//	cutoffBands = null;
		//	//	result = false;
		//	//}

		//	//return result;
		//}

		private (CutoffBand[]? cutoffBands, bool resultsAreComplete) UpdateCutoffs(HistCutoffsSnapShot histCutoffsSnapShot)
		{
			CutoffBand[]? cutoffBands = null;
			bool resultsAreComplete = false;

			if (histCutoffsSnapShot.ColorBandSetId != _currentColorBandSet.Id)
			{
				Debug.WriteLine("ColorBandSetViewModel.The HistCutoffsSnapShot is stale, not Updating the New Cutoffs.");
				return (cutoffBands, resultsAreComplete);
			}

			// TODO: Do not Apply the new Cutoffs if there was some problem getting the Cutoffs from the current Percentage values
			// This may be caused by an incomplete histogram.
			// Update the TryGetCutoffsFromPercentages to report if there any problems.

			if (ColorBandSetHelper.TryGetCutoffsFromPercentages(histCutoffsSnapShot, out cutoffBands, out resultsAreComplete))
			{
				CheckNewCutoffs(histCutoffsSnapShot.PercentageBands, cutoffBands);
				ReportNewCutoffs(histCutoffsSnapShot, histCutoffsSnapShot.PercentageBands, cutoffBands);

				if (ApplyNewCutoffs(cutoffBands, histCutoffsSnapShot.ColorBandSetId))
				{
					var newColorBandSet = _currentColorBandSet.CreateNewCopy();
					ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(newColorBandSet, isPreview: true));
				}
				else
				{
					cutoffBands = null;
					resultsAreComplete = false;
				}
			}
			else
			{
				cutoffBands = null;
				resultsAreComplete = false;
			}

			return (cutoffBands, resultsAreComplete);
		}

		private bool ApplyNewCutoffs(CutoffBand[] newCutoffs, ObjectId colorBandSetId)
		{
			bool result;

			lock (_histLock)
			{
				if (_currentColorBandSet.Id != colorBandSetId)
				{
					Debug.WriteLine("The HistCutoffsSnapShot is stale, not Applying the New Cutoffs.");
				}

				_disableProcessCurColorBandPropertyChanges = true;

				if (_currentColorBandSet.UpdateCutoffs(newCutoffs))
				{
					//_currentColorBandSet.MarkAsDirty();
					result = true;
					//_currentColorBandSet.UsingPercentages = false;

					BeyondTargetSpecs = new PercentageBand(newCutoffs[^1].Cutoff, newCutoffs[^1].Percentage);
					var numberReachedTargetIteration = BeyondTargetSpecs.Count;
					var total = BeyondTargetSpecs.RunningSum;
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel. received new Cutoffs. Top Count: {numberReachedTargetIteration}, Total: {total}.");
				}
				else
				{
					BeyondTargetSpecs = null;
					result = false;
				}

				_disableProcessCurColorBandPropertyChanges = false;
			}

			//PercentageUseStatus = GetPercentageUseStatus(_currentColorBandSet.UsingPercentages, UsePercentagesLocalSetting);

			return result;
		}

		private HistCutoffsSnapShot GetHistCutoffsSnapShot(IHistogram histogram, bool histogramIsFromACompleteMap, ColorBandSet colorBandSet)
		{
			HistCutoffsSnapShot result;

			lock (_histLock)
			{
				result = new HistCutoffsSnapShot(
					colorBandSet.Id,
					histogram.GetKeyValuePairs(),
					histogram.Length,
					histogram.UpperCatchAllValue,
					histogramIsFromACompleteMap,
					ColorBandSetHelper.GetPercentageBands(colorBandSet),
					colorBandSet.UsingPercentages
				);
			}

			return result;
		}

		private string GetDefaultUsePercentagesStrVal(bool? value)
		{
			var strState = value.HasValue
				? value.Value ? "True" : "False"
				: "Undefined";

			return strState;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void CheckNewCutoffs(PercentageBand[] percentageBands, CutoffBand[] cutoffBands)
		{
			ColorBandSetHelper.CheckNewCutoffs(percentageBands, cutoffBands);
		}

		[Conditional("DEBUG2")]
		private void ReportNewCutoffs(HistCutoffsSnapShot histCutoffsSnapShot, PercentageBand[] percentageBands, CutoffBand[] cutoffBands)
		{
			ColorBandSetHelper.ReportNewCutoffs(histCutoffsSnapShot, percentageBands, cutoffBands);
		}

		[Conditional("DEBUG2")]
		private void ReportNewPercentages(PercentageBand[] percentageBands)
		{
			ColorBandSetHelper.ReportNewPercentages(percentageBands);
		}

		[Conditional("DEBUG2")]

		private void ReportRemoveCurrentItem(int index)
		{
			var newIndex = _currentColorBandSet.IndexOf((ColorBand)ColorBandsView.CurrentItem);
			//Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {newIndex}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");
			Debug.WriteLine($"ColorBandSetViewModel:Removed item at former index: {index}. The new index is: {newIndex}. The View's CurrentPosition is {ColorBandsView.CurrentPosition}.");
		}

		[Conditional("DEBUG2")]
		private void ReportSeriesBufferAllocation(long existingLength, long newLength, bool bufferWasPreserved)
		{
			if (!bufferWasPreserved)
			{
				Debug.WriteLine($"WARNING: ColorBandSetViewModel. Allocating new buffer to hold the Y Values. New length:{newLength}, existing length:{existingLength}.");
			}
			else
			{
				Debug.WriteLine($"ColorBandSetViewModel. Updating SeriesData. Using existing buffer. Length:{newLength}.");
			}
		}

		[Conditional("DEBUG")]
		private void ReportIndexOfSender(object? sender)
		{
			var index = GetIndexOfSender(sender);

			Debug.WriteLine($"CbsHistogramViewModel is handling EditEnded for {index}.");
		}

		private int? GetIndexOfSender(object? sender)
		{
			if (sender == null)
			{
				Debug.WriteLine($"ColorBandSetViewModel: The sender on the call to EditEnded is null.");
				return null;
			}

			int? result;

			if (sender is ColorBand cb)
			{
				var cbsView = ColorBandsView;
				if (cbsView == null)
				{
					throw new InvalidOperationException("The ColorBandsView is NULL.");
				}

				var index = cbsView.IndexOf(cb);

				Debug.Assert(index >= 0, "Could not find the ColorBand whose edits are being ended in the ColorBandsView.");

				result = index == -1 ? null : index;

				Debug.WriteLine($"CbsHistogramViewModel is handling EditEnded for {index}.");
			}
			else
			{
				Debug.WriteLine($"ColorBandSetViewModel: A sender of type {sender?.GetType()} is raising the EditEnded event. EXPECTED: {typeof(ColorBand)}.");
				result = null;
			}

			return result;
		}

		//private HPlotSeriesData BuildTestSeries()
		//{
		//	double[] dataX = new double[] { 1, 2, 3, 4, 5 };
		//	double[] dataY = new double[] { 1, 4, 9, 16, 25 };

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

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

					_mapSectionHistogramProcessor.HistogramUpdated -= HistogramUpdated;

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

		#region Unused

		//private void UpdateSectionLinePosition(int colorBandIndex, int newCutoff)
		//{
		//	if (ListViewItems.Count == 0)
		//	{
		//		return;
		//	}

		//	if (colorBandIndex < 0 || colorBandIndex > ListViewItems.Count - 2)
		//	{
		//		throw new InvalidOperationException($"CbListView::UpdateSectionLinePosition. The ColorBandIndex must be between 0 and {ListViewItems.Count - 1}, inclusive.");
		//	}

		//	Debug.WriteLineIf(_useDetailedDebug, $"CbListView. About to call SectionLine::UpdatePosition. Index = {colorBandIndex}");

		//	var selectionLine = ListViewItems[colorBandIndex].CbSectionLine;

		//	if (ScreenTypeHelper.IsDoubleChanged(newCutoff, selectionLine.XPosition))
		//	{
		//		var cbRectangleLeft = ListViewItems[colorBandIndex].CbRectangle;
		//		var cbRectangleRight = ListViewItems[colorBandIndex + 1].CbRectangle;

		//		Debug.Assert(cbRectangleLeft.XPosition + cbRectangleLeft.Width == selectionLine.XPosition);
		//		Debug.Assert(cbRectangleRight.XPosition == selectionLine.XPosition);

		//		var diff = newCutoff - selectionLine.XPosition;

		//		selectionLine.XPosition = newCutoff;

		//		cbRectangleLeft.Width += diff;

		//		cbRectangleRight.XPosition = newCutoff;
		//		cbRectangleRight.Width -= diff;
		//	}
		//}

		//private bool TryGetColorBandIndex(ListCollectionView? colorbandsView, ColorBand cb, [NotNullWhen(true)] out int? index)
		//{
		//	//var colorBandsList = colorbandsView as IList<ColorBand>;
		//	if (colorbandsView == null)
		//	{
		//		index = null;
		//		return false;
		//	}

		//	index = colorbandsView.IndexOf(cb);

		//	if (index < 0)
		//	{
		//		var t = colorbandsView.SourceCollection.Cast<ColorBand>();

		//		var cbWithMatchingOffset = t.FirstOrDefault(x => x.Cutoff == cb.Cutoff);

		//		if (cbWithMatchingOffset != null)
		//		{
		//			index = colorbandsView.IndexOf(cbWithMatchingOffset);
		//			Debug.WriteLine($"CbListView. The ColorBandsView does not contain the ColorBand: {cb}, but found an item with a matching offset: {cbWithMatchingOffset} at index: {index}.");

		//			return true;
		//		}
		//		else
		//		{
		//			return false;
		//		}
		//	}
		//	else
		//	{
		//		return true;
		//	}
		//}

		//private ColorBand GetColorBandAt(ListCollectionView cbsView, int index)
		//{
		//	try
		//	{
		//		var result = (ColorBand)cbsView.GetItemAt(index);
		//		return result;
		//	}
		//	catch (ArgumentOutOfRangeException aore)
		//	{
		//		throw new InvalidOperationException($"No item exists at index {index} within the ColorBandsView.", aore);
		//	}
		//	catch (InvalidCastException ice)
		//	{
		//		throw new InvalidOperationException($"The item at index {index} is not of type ColorBand.", ice);
		//	}
		//}

		#endregion

	}
}
