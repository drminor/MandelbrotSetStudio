using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private static bool _keepDisplaySquare;

		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly IScreenSectionCollection _screenSectionCollection;

		private SizeInt _canvasSize;
		private VectorInt _canvasControlOffset;

		private Job? _currentJob;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;

		private SizeDbl _containerSize;

		#region Constructor

		public MapDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_useEscapeVelocities = true;
			_keepDisplaySquare = false;
			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;
			_mapLoaderManager.MapSectionReady += MapLoaderManager_MapSectionReady;

			BlockSize = blockSize;

			_screenSectionCollection = new ScreenSectionCollection(BlockSize);
			ImageSource = new DrawingImage(_screenSectionCollection.DrawingGroup);
			_currentJob = null;
			_colorBandSet = new ColorBandSet();
			_colorMap = null;

			//_containerSize = new SizeDbl(1050, 1050);
			//CanvasSize = new SizeInt(1024, 1024);
			//var screenSectionExtent = new SizeInt(12, 12);
			ContainerSize = new SizeDbl(1050, 1050);
			CanvasControlOffset = new VectorInt();

			MapSections = new ObservableCollection<MapSection>();
			MapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; }

		public ImageSource ImageSource { get; init; }

		public Job? CurrentJob
		{
			get => _currentJob;
			set
			{
				if (value != _currentJob)
				{
					var previousJob = _currentJob;
					_currentJob = value?.Clone();
					HandleCurrentJobChanged(previousJob, _currentJob);
					OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentJob));
				}
			}
		}

		public ColorBandSet ColorBandSet => _colorBandSet;

		public void SetColorBandSet(ColorBandSet value, bool updateDisplay)
		{
			if (value != _colorBandSet)
			{
				Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}. UpdateDisplay = {updateDisplay}");

				if (CurrentJob is null)
				{
					// Take the value given, as is. Without a current job, we cannot adjust the iterations.
					LoadColorMap(value);
				}
				else
				{
					var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(value, CurrentJob.MapCalcSettings.TargetIterations);
					LoadColorMap(adjustedColorBandSet);

					if (updateDisplay)
					{
						HandleColorMapChanged(_colorMap, _useEscapeVelocities);
					}	
				}
			}
			else
			{
				if (updateDisplay)
				{
					Debug.WriteLine($"The MapDisplay is processing the existing ColorMap event though the new value is the same as the existing value. Id = {value.Id}. UpdateDisplay = {updateDisplay}");
					HandleColorMapChanged(_colorMap, _useEscapeVelocities);
				}
				else
				{
					Debug.WriteLine($"The MapDisplay is NOT processing a new ColorMap, the new value is the same as the existing value. Id = {value.Id}. UpdateDisplay = {updateDisplay}");
				}
			}
		}

		private void LoadColorMap(ColorBandSet colorBandSet)
		{
			if (ColorBandSet != colorBandSet)
			{
				_colorBandSet = colorBandSet;
				_colorMap = new ColorMap(colorBandSet);
				_colorMap.UseEscapeVelocities = _useEscapeVelocities;
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
					Debug.WriteLine($"The MapDisplay is turning {strState} the use of EscapeVelocities.");
					_useEscapeVelocities = value;

					if (!(_colorMap is null))
					{
						_colorMap.UseEscapeVelocities = value;
						HandleColorMapChanged(_colorMap, _useEscapeVelocities);
					}
				}
			}
		}

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;
				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(value, BlockSize, _keepDisplaySquare);
				_screenSectionCollection.CanvasSizeInWholeBlocks = sizeInWholeBlocks.Inflate(2);

				CanvasSize = sizeInWholeBlocks.Scale(BlockSize);
			}
		}

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					_canvasSize = value;
					OnPropertyChanged();
				}
			}
		}

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set { _canvasControlOffset = value; OnPropertyChanged(); }
		}

		public ObservableCollection<MapSection> MapSections { get; }

		#endregion

		#region Public Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var newArea = e.Area;
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, newArea, e.IsPreview));
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var newArea = new RectangleInt(new PointInt(invOffset), CanvasSize);
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, newArea));
		}

		public void TearDown()
		{
			// TODO: Unsubscribe our Event Handlers in MapDisplayViewModel::TearDown
		}

		#endregion

		#region Event Handlers

		private void MapLoaderManager_MapSectionReady(object? sender, MapSection e)
		{
			// TODO: Use a lock on MapSectionReady to avoid race conditions as the ColorMap is applied.
			MapSections.Add(e);
		}

		private void MapSections_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				_screenSectionCollection.HideScreenSections();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Add items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				DrawSections(mapSections, _colorMap, _useEscapeVelocities);
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.OldItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					if (!_screenSectionCollection.Hide(mapSection))
					{
						Debug.WriteLine($"While handling the MapSections_CollectionChanged:Remove, the MapDisplayViewModel cannot Hide the MapSection: {mapSection}.");
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private void HandleColorMapChanged(ColorMap? colorMap, bool useEscapeVelocities)
		{
			if (colorMap != null)
			{
				var loadedSections = GetMapSectionsSnapShot();
				DrawSections(loadedSections, colorMap, useEscapeVelocities);
			}
		}

		private void DrawSections(IEnumerable<MapSection> mapSections, ColorMap? colorMap, bool useEscapVelocities)
		{
			if (colorMap != null)
			{
				Debug.Assert(colorMap.UseEscapeVelocities == useEscapVelocities, "UseEscapeVelocities MisMatch on DrawSections.");

				foreach (var mapSection in mapSections)
				{
					if (mapSection.Counts != null)
					{
						//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
						var pixels = _mapSectionHelper.GetPixelArray(mapSection.Counts, mapSection.Size, colorMap, !mapSection.IsInverted, useEscapVelocities);
						_screenSectionCollection.Draw(mapSection.BlockPosition, pixels);
					}
				}
			}
			else
			{
				foreach (var mapSection in mapSections)
				{
					Debug.WriteLine($"Not drawing screen section at position: {mapSection.BlockPosition}, the color map is null.");
				}
			}
		}

		private void HandleCurrentJobChanged(Job? previousJob, Job? newJob)
		{
			Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {newJob?.Id ?? ObjectId.Empty}");
			_mapLoaderManager.StopCurrentJob();

			if (newJob != null)
			{
				if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
				{
					ReuseLoadedSections(newJob);
				}
				else
				{
					Debug.WriteLine($"Clearing Display. TransformType: {newJob.TransformType}.");
					MapSections.Clear();
					CanvasControlOffset = newJob.CanvasControlOffset;

					_mapLoaderManager.Push(newJob);
				}
			}
			else
			{
				MapSections.Clear();
			}
		}

		private void ReuseLoadedSections(Job curJob)
		{
			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(curJob);
			var loadedSections = GetMapSectionsSnapShot();

			// Avoid requesting sections already drawn
			var sectionsToLoad = GetNotYetLoaded(sectionsRequired, loadedSections);

			// Remove from the screen sections that are not part of the updated view.
			var uResults = UpdateMapSectionCollection(MapSections, sectionsRequired, out var shiftAmount);
			var cntRemoved = uResults.Item1;
			var cntRetained = uResults.Item2;
			var cntUpdated = uResults.Item3;

			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {cntRemoved}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");

			if (cntRemoved > 0 || cntUpdated > 0)
			{
				//MessageBox.Show("The old sections have been removed.");

				_screenSectionCollection.Shift(shiftAmount);
			}

			if (CanvasControlOffset != curJob.CanvasControlOffset)
			{
				CanvasControlOffset = curJob.CanvasControlOffset;
				//MessageBox.Show("The CanvasControlOffset has been updated.");
			}

			_screenSectionCollection.HideScreenSections();
			//MessageBox.Show("The Screen Sections have been hidden.");

			RedrawSections(MapSections);
			//MessageBox.Show("The Screen Sections have been redrawn.");

			if (sectionsToLoad.Count > 0)
			{
				//MessageBox.Show("Just before requesting new Screen Sections.");
				_mapLoaderManager.Push(curJob, sectionsToLoad);
			}
		}

		private bool ShouldAttemptToReuseLoadedSections(Job? previousJob, Job newJob)
		{
			if (MapSections.Count == 0 || previousJob is null)
			{
				return false;
			}

			if (newJob.CanvasSizeInBlocks != previousJob.CanvasSizeInBlocks)
			{
				return false;
			}

			if (newJob.ColorBandSetId != previousJob.ColorBandSetId)
			{
				return false;
			}

			var jobSpd = RNormalizer.Normalize(newJob.Subdivision.SamplePointDelta, previousJob.Subdivision.SamplePointDelta, out var previousSpd);
			return jobSpd == previousSpd;
		}

		private IList<MapSection> GetNotYetLoaded(IList<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		{
			var result = sectionsNeeded.Where(
				neededSection => !sectionsPresent.Any(
					presentSection => presentSection == neededSection 
					&& presentSection.TargetIterations == neededSection.TargetIterations
					)
				).ToList();

			return result;
		}

		private Tuple<int, int, int> UpdateMapSectionCollection(ObservableCollection<MapSection> sectionsPresent, IList<MapSection> newSet, out VectorInt shiftAmount)
		{
			var cntRemoved = 0;
			var cntRetained = 0;
			var cntUpdated = 0;

			var toBeRemoved = new List<MapSection>();
			var differences = new Dictionary<VectorInt, int>();

			foreach (var mapSection in sectionsPresent)
			{
				var matchingNewSection = newSet.FirstOrDefault(x => x == mapSection);
				if (matchingNewSection == null)
				{
					toBeRemoved.Add(mapSection);
					cntRemoved++;
				}
				else
				{
					cntRetained++;
					var diff = matchingNewSection.BlockPosition.Sub(mapSection.BlockPosition);
					if (diff.X != 0 || diff.Y != 0)
					{
						cntUpdated++;
						mapSection.BlockPosition = matchingNewSection.BlockPosition;

						if (differences.TryGetValue(diff, out var value))
						{
							differences[diff] = value + 1;
						}
						else
						{
							differences.Add(diff, 1);
						}
					}
				}
			}

			if (differences.Count > 0)
			{
				var mostPrevalentCnt = differences.Max(x => x.Value);
				shiftAmount = differences.First(x => x.Value == mostPrevalentCnt).Key;
			}
			else
			{
				shiftAmount = new VectorInt();
			}

			foreach(var mapSection in toBeRemoved)
			{
				if (!sectionsPresent.Remove(mapSection))
				{
					Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
					//Thread.Sleep(300);
				}
			}

			return new Tuple<int, int, int>(cntRemoved, cntRetained, cntUpdated);
		}

		private IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		private void RedrawSections(IEnumerable<MapSection> source)
		{
			foreach (var mapSection in source)
			{
				//Debug.WriteLine($"About to redraw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				_screenSectionCollection.Redraw(mapSection.BlockPosition);
				//Thread.Sleep(200);
			}
		}

		#endregion
	}
}
