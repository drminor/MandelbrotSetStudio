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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Windows.UI.WebUI;

namespace MSetExplorer
{
	public class CbsHistogramViewModel : ViewModelBase, IDisposable, IUndoRedoViewModel, ICbsHistogramViewModel
	{
		#region Private Fields

		private const int SELECTION_LINE_UPDATE_THROTTLE_INTERVAL = 200;
		private DebounceDispatcher _selectionLineMovedDispatcher;

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

		private bool _colorBandUserControlHasErrors;

		private readonly bool _useDetailedDebug = false;

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

			_editMode = ColorBandSetEditMode.Bands;

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

		public ColorBandSetEditMode EditMode
		{
			get => _editMode;
			set
			{
				if (value != _editMode)
				{
					Debug.WriteLine($"ColorBandSetViewModel: The Edit mode is now {value}");
					_editMode = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.EditMode));
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

					lock (_histLock)
					{
						_colorBandSetHistoryCollection.Load(value.CreateNewCopy());

						_mapSectionHistogramProcessor.Reset(value.HighCutoff);
						UpdatePercentages();
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
					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetViewModel is turning {strState} the use of RealTimePreview. IsDirty = {IsDirty}.");
					_useRealTimePreview = value;

					if (IsDirty)
					{
						var colorBandSet = _useRealTimePreview ? _currentColorBandSet : _colorBandSetHistoryCollection[0];

						colorBandSet = colorBandSet.CreateNewCopy();
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

		public bool TryInsertNewItem(ColorBand colorBand, out int index)
		{
			bool result;
			switch (EditMode)
			{
				case ColorBandSetEditMode.Offsets:
					result = TryInsertOffset(colorBand, out index);
					break;
				case ColorBandSetEditMode.Colors:
					result = TryInsertColor(colorBand, out index);
					break;
				case ColorBandSetEditMode.Bands:
					result = TryInsertColorBand(colorBand, out index);
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

		private bool TryInsertOffset(ColorBand colorBand, out int index)
		{
			if (colorBand.Cutoff - colorBand.StartingCutoff < 1)
			{
				Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
				index = -1;
				return false;
			}

			var prevCutoff = colorBand.PreviousCutoff ?? 0;
			var newCutoff = prevCutoff + (colorBand.Cutoff - prevCutoff) / 2;

			index = _currentColorBandSet.IndexOf(colorBand);

			CurrentColorBand = null;
			_currentColorBandSet.InsertCutoff(index, newCutoff);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(colorBand);

			return true;
		}

		private bool TryInsertColor(ColorBand colorBand, out int index)
		{
			index = _currentColorBandSet.IndexOf(colorBand);

			var newItem = new ColorBand(0, ColorBandColor.White, ColorBandBlendStyle.Next, colorBand.StartColor);

			CurrentColorBand = null;
			_currentColorBandSet.InsertColor(index, newItem);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(colorBand);

			return true;
		}

		private bool TryInsertColorBand(ColorBand colorBand, out int index)
		{
			if (colorBand.Cutoff - colorBand.StartingCutoff < 1)
			{
				Debug.WriteLine($"ColorBandSetViewModel:InsertNewItem is aborting. The starting and ending cutoffs have the same value.");
				index = -1;
				return false;
			}

			var prevCutoff = colorBand.PreviousCutoff ?? 0;
			var newCutoff = prevCutoff + (colorBand.Cutoff - prevCutoff) / 2;
			var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.Next, colorBand.StartColor, colorBand.PreviousCutoff, colorBand.StartColor, double.NaN);

			index = _currentColorBandSet.IndexOf(colorBand);

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:About to Insert item at index: {index}. The new item is: {newItem}.");

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



		public bool TryDeleteSelectedItem(ColorBand colorBand)
		{
			//var selectedItems = GetSelectedItems(_currentColorBandSet);

			bool result;

			switch (EditMode)
			{
				case ColorBandSetEditMode.Offsets:
					result = TryDeleteOffset(colorBand);
					break;
				case ColorBandSetEditMode.Colors:
					result = TryDeleteColor(colorBand);
					break;
				case ColorBandSetEditMode.Bands:
					result = TryDeleteColorBand(colorBand);
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

		private bool TryDeleteOffset(ColorBand colorBand)
		{
			var index = _currentColorBandSet.IndexOf(colorBand);

			if (index > _currentColorBandSet.Count - 2)
			{
				// Cannot delete the last entry
				return false;
			}

			CurrentColorBand = null;
			_currentColorBandSet.DeleteCutoff(index);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(colorBand);

			return true;
		}

		private bool TryDeleteColor(ColorBand colorBand)
		{
			var index = _currentColorBandSet.IndexOf(colorBand);

			if (index > _currentColorBandSet.Count - 2)
			{
				// Cannot delete the last entry
				return false;
			}

			CurrentColorBand = null;
			_currentColorBandSet.DeleteColor(index);
			ColorBandsView.Refresh();
			ColorBandsView.MoveCurrentTo(colorBand);

			return true;
		}

		private bool TryDeleteColorBand(ColorBand colorBand)
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

			bool colorBandWasRemoved;
			lock (_histLock)
			{
				colorBandWasRemoved = _currentColorBandSet.Remove(colorBand);
			}

			if (colorBandWasRemoved)
			{
				if (index > _colorBandsView.Count - 1)
				{
					_colorBandsView.MoveCurrentToPosition(Math.Max(0, index - 1));
				}
			}
			else
			{
				Debug.WriteLine("WARNING: ColorBandSetViewModel:Could not remove the item.");
			}

			ReportRemoveCurrentItem(index);

			return true;
		}

		[Conditional("DEBUG")]
		private void ReportRemoveCurrentItem(int index)
		{
			var newIndex = _currentColorBandSet.IndexOf((ColorBand)ColorBandsView.CurrentItem);
			//Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {newIndex}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:Removed item at former index: {index}. The new index is: {newIndex}.");
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
			var newSet = _currentColorBandSet.CreateNewCopy();

			ApplyChangesInt(newSet);
		}

		private void ApplyChangesInt(ColorBandSet newSet)
		{
			Debug.WriteLine($"The ColorBandSetViewModel is Applying changes. The new Id is {newSet.Id}, name: {newSet.Name}. The old Id is {ColorBandSet?.Id ?? ObjectId.Empty}");

			//Debug.WriteLine($"The new ColorBandSet: {newSet}");
			//Debug.WriteLine($"The existing ColorBandSet: {_colorBandSet}");

			_colorBandSet = newSet;

			// Clear all existing items from history and add the new set.
			_colorBandSetHistoryCollection.Load(_colorBandSet);

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

		public IDictionary<int, int> GetHistogramForColorBand(ColorBand colorBand)
		{
			var previousCutoff = colorBand.PreviousCutoff ?? 0;
			var cutoff = colorBand.Cutoff;

			var kvpsForBand = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(previousCutoff, cutoff, includeCatchAll: true);

			return new Dictionary<int, int>(kvpsForBand);
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

			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's SerieData is being updated. There are {SeriesData.LongLength} entries.");

			SeriesData = new HPlotSeriesData(SeriesData);

			return result;
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

		private void ColorBandsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:ColorBandsView_CollectionChanged. Action is {e.Action}. NewItemsCount: {e.NewItems?.Count ?? -1}. OldItemsCount: {e.OldItems?.Count ?? -1}.");

			OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsCount));
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			if (ColorBandsView != null)
			{
				if (ColorBandSet != null)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:ColorBandsView_CurrentChanged. Setting the HighlightedColorBandIndex from: {ColorBandSet.HilightedColorBandIndex} to the ColorBandsView's CurrentPosition: {ColorBandsView.CurrentPosition}.");

					ColorBandSet.HilightedColorBandIndex = ColorBandsView.CurrentPosition;
				}

				CurrentColorBand = (ColorBand)ColorBandsView.CurrentItem;
			}
		}

		private void CurrentColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is ColorBand colorBandToUpdate)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetViewModel:CurrentColorBand Prop: {e.PropertyName} is changing.");
			}
			else
			{
				Debug.WriteLine($"ColorBandSetViewModel: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");
				return;
			}

			var foundUpdate = ProcessColorBandUpdate(colorBandToUpdate, e.PropertyName);

			if (foundUpdate)
			{
				// Don't include each property change when the Current ColorBand is being edited.
				if (!colorBandToUpdate.IsInEditMode)
				{
					PushCurrentColorBandOnToHistoryCollection();
					IsDirty = true;
				}

				if (UseRealTimePreview)
				{
					var newColorBandSet = _currentColorBandSet.CreateNewCopy();

					//ColorBandSetUpdateRequested?.Invoke(this, new ColorBandSetUpdateRequestedEventArgs(newColorBandSet, isPreview: true));
					RaiseUpdateRequestThrottled(newColorBandSet);
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
				if (cb.BucketWidth < 0)
				{
					throw new ArgumentOutOfRangeException("BucketWidth is < 0");
				}

				if (TryGetSuccessor(_currentColorBandSet, cb, out var successorColorBand))
				{
					Debug.WriteLine($"Cutoff was updated. CbsHistogramViewModel is updating the PreviousCutoff for ColorBand: {_colorBandsView.IndexOf(cb)}");
					successorColorBand.PreviousCutoff = cb.Cutoff;

					if (successorColorBand.BucketWidth < 0)
					{
						throw new ArgumentOutOfRangeException("The next ColorBand's BucketWidth is < 0");
					}
				}

				foundUpdate = true;
				UpdatePercentages();
			}

			// Previous Cutoff is being updated
			else if (propertyName == nameof(ColorBand.PreviousCutoff))
			{
				if (cb.BucketWidth < 0)
				{
					throw new ArgumentOutOfRangeException("BucketWidth is < 0");
				}

				if (TryGetPredeccessor(_currentColorBandSet, cb, out var predecessorColorBand))
				{
					if (cb.PreviousCutoff == null)
					{
						throw new InvalidOperationException("The PreviousCutoff is null, however we are not the first ColorBand.");
					}

					Debug.WriteLine($"PreviousCutoff was updated. CbsHistogramViewModel is updating the Cutoff for ColorBand: {_colorBandsView.IndexOf(cb)}");

					predecessorColorBand.Cutoff = cb.PreviousCutoff.Value;

					if (predecessorColorBand.BucketWidth < 0)
					{
						throw new ArgumentOutOfRangeException("The predecessor ColorBand's BucketWidth is < 0");
					}
				}

				foundUpdate = true;
				UpdatePercentages();
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
						_ = RefreshDisplay();
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

		private int GetExtent(ColorBandSet colorBandSet)
		{
			var result = colorBandSet.Count < 2 ? 0 : colorBandSet.HighCutoff;
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

		private void UpdateViewAndRaisePropertyChangeEvents(int? currentColorBandIndex = null)
		{
			_currentColorBandSet = _colorBandSetHistoryCollection.CurrentColorBandSet.CreateNewCopy();

			if (ColorBandsView is INotifyCollectionChanged t1) t1.CollectionChanged -= ColorBandsView_CollectionChanged;

			ColorBandsView = BuildColorBandsView(_currentColorBandSet);

			if (ColorBandsView is INotifyCollectionChanged t2) t2.CollectionChanged += ColorBandsView_CollectionChanged;

			if (currentColorBandIndex != null)
			{
				ColorBandsView.MoveCurrentToPosition(currentColorBandIndex.Value);
			}
			else
			{
				ColorBandsView.MoveCurrentToFirst();
			}

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

		private List<ColorBand> GetSelectedItems(IList<ColorBand> colorBands)
		{
			var result = colorBands.Where(c => c.IsSelected).ToList();
			return result;
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
				pb.Percentage = Math.Round(100 * (pb.Count / total), digits: 2);
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
					var numberReachedTargetIteration = BeyondTargetSpecs.Count;
					var total = BeyondTargetSpecs.RunningSum;
					Debug.WriteLine($"CBS received new percentages. Top Count: {numberReachedTargetIteration}, Total: {total}.");
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
