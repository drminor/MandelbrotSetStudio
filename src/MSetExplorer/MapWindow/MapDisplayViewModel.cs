using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private static readonly bool _keepDisplaySquare = true;

		private readonly ProjectAdapter _projectAdapter;
		private readonly IJobStack _jobStack;
		private readonly IMapLoaderManager _mapLoaderManager;

		private readonly IScreenSectionCollection _screenSectionCollection;

		private SizeInt _canvasSize;
		private VectorInt _canvasControlOffset;

		#region Constructor

		public MapDisplayViewModel(ProjectAdapter projectAdapter, IJobStack jobsStack, IMapLoaderManager mapLoaderManager, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;

			_jobStack = jobsStack;
			_jobStack.CurrentJobChanged += JobStack_CurrentJobChanged;

			_mapLoaderManager = mapLoaderManager;
			_mapLoaderManager.MapSectionReady += MapLoaderManager_MapSectionReady;

			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();

			_screenSectionCollection = new ScreenSectionCollection(BlockSize);
			ImageSource = new DrawingImage(_screenSectionCollection.DrawingGroup);

			MapSections.CollectionChanged += MapSections_CollectionChanged;

			ContainerSize = new SizeDbl(1050, 1050);

			//_containerSize = new SizeDbl(1050, 1050);
			//CanvasSize = new SizeInt(1024, 1024);
			//var screenSectionExtent = new SizeInt(12, 12);

			CanvasControlOffset = new VectorInt();
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public SizeInt BlockSize { get; }

		private SizeDbl _containerSize;
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
			set
			{
				_canvasControlOffset = value;
				_screenSectionCollection.CanvasControlOffset = value;
				OnPropertyChanged();
			}
		}

		public Project CurrentProject { get; set; }

		public ObservableCollection<MapSection> MapSections { get; }

		#endregion

		#region Public Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var newArea = e.Area;
			UpdateMapView(TransformType.Zoom, newArea);
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var newArea = new RectangleInt(new PointInt(invOffset), CanvasSize);
			UpdateMapView(TransformType.Pan, newArea);
		}

		#endregion

		#region Event Handlers

		private void JobStack_CurrentJobChanged(object sender, EventArgs e)
		{
			var curJob = _jobStack.CurrentJob;

			_mapLoaderManager.StopCurrentJob();

			if (curJob.TransformType == TransformType.Pan)
			{
				var sectionsRequired = MapSectionHelper.CreateEmptyMapSections(curJob);

				// X: Consider not using the ObservableCollection<MapSection>, instead have the ScreenSectionCollection maintain a simple list of MapSections
				var loadedSections = GetMapSectionsSnapShot();

				// Avoid requesting sections already drawn
				var sectionsToLoad = GetNotYetLoaded(sectionsRequired, loadedSections);

				// Remove from the screen sections that are not part of the updated view.
				var uResults = UpdateMapSectionCollection(MapSections, sectionsRequired, out var shiftAmount);
				var cntRemoved = uResults.Item1;
				var cntRetained = uResults.Item2;
				var cntUpdated = uResults.Item3;

				Debug.WriteLine($"Panning: requesting {sectionsToLoad.Count} new sections, removing {cntRemoved}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");


				_screenSectionCollection.Shift(shiftAmount);
				CanvasControlOffset = curJob?.CanvasControlOffset ?? new VectorInt();
				RedrawSections(MapSections);
				_mapLoaderManager.Push(curJob, sectionsToLoad);
			}
			else
			{
				// X:
				//_screenSectionCollection.HideScreenSections();

				MapSections.Clear();
				CanvasControlOffset = curJob?.CanvasControlOffset ?? new VectorInt();
				_mapLoaderManager.Push(curJob);
			}
		}

		private void MapLoaderManager_MapSectionReady(object sender, MapSection e)
		{
			// X:
			////Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
			//_screenSectionCollection.Draw(mapSection);

			MapSections.Add(e);
		}

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
				foreach (var mapSection in mapSections)
				{
					//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
					_screenSectionCollection.Draw(mapSection);
				}
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
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

		private void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = _jobStack.CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);

			var updatedInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);

			//if (Iterations > 0 && Iterations != updatedInfo.MapCalcSettings.MaxIterations)
			//{
			//	updatedInfo = MSetInfo.UpdateWithNewIterations(updatedInfo, Iterations, Steps);
			//}

			var parentJob = _jobStack.CurrentJob;
			var jobName = MapWindowHelper.GetJobName(transformType);
			var job = MapWindowHelper.BuildJob(parentJob, CurrentProject, jobName, CanvasSize, updatedInfo, transformType, newArea, BlockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {updatedInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			_jobStack.Push(job);
		}

		private IList<MapSection> GetNotYetLoaded(IList<MapSection> source, IReadOnlyList<MapSection> current)
		{
			IList<MapSection> result = new List<MapSection>();

			foreach(var mapSection in source)
			{
				if (!current.Any(x => x == mapSection))
				{
					result.Add(mapSection);
				}
			}

			return result;
		}

		private Tuple<int, int, int> UpdateMapSectionCollection(ObservableCollection<MapSection> source, IList<MapSection> newSet, out VectorInt shiftAmount)
		{
			var cntRemoved = 0;
			var cntRetained = 0;
			var cntUpdated = 0;

			IList<MapSection> toBeRemoved = new List<MapSection>();
			shiftAmount = new VectorInt();

			foreach (var mapSection in source)
			{
				var matchingNewSection = newSet.FirstOrDefault(x => x == mapSection);
				if(matchingNewSection == null)
				{
					toBeRemoved.Add(mapSection);
					cntRemoved++;
				}
				else
				{
					var diff = matchingNewSection.BlockPosition.Sub(mapSection.BlockPosition);

					if (cntRetained == 0)
					{
						shiftAmount = diff;
					}
					else
					{
						if (shiftAmount != diff)
						{
							throw new InvalidOperationException($"The MapSection Collection contains inconsistent block positions. Compare: {diff} and {shiftAmount}.");
							//Debug.WriteLine("The MapSection Collection contains inconsistent block positions.");
						}
					}

					cntRetained++;

					if (mapSection.BlockPosition != matchingNewSection.BlockPosition)
					{
						mapSection.BlockPosition = matchingNewSection.BlockPosition;
						cntUpdated++;
					}
				}
			}

			foreach(var mapSection in toBeRemoved)
			{
				// X: 
				//if (!_screenSectionCollection.Hide(mapSection))
				//{
				//	Debug.WriteLine($"While handling the MapSections_CollectionChanged:Remove, the MapDisplayViewModel cannot Hide the MapSection: {mapSection}.");
				//}

				if (!source.Remove(mapSection))
				{
					Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
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
				_screenSectionCollection.Redraw(mapSection);
			}
		}

		#endregion
	}
}
