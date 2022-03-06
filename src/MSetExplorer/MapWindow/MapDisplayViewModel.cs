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
		private readonly ProjectAdapter _projectAdapter;
		private readonly IJobStack _jobStack;
		private readonly IMapLoaderManager _mapLoaderManager;

		private readonly DrawingGroup _drawingGroup;
		private readonly IScreenSectionCollection _screenSectionCollection;

		private VectorInt _canvasControlOffset;

		#region Constructor

		public MapDisplayViewModel(ProjectAdapter projectAdapter, IJobStack jobsStack, IMapLoaderManager mapLoaderManager, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;

			_jobStack = jobsStack;
			_jobStack.CurrentJobChanged += JobStack_CurrentJobChanged;

			_mapLoaderManager = mapLoaderManager;
			_mapLoaderManager.MapSectionReady += MapLoaderManager_MapSectionReady;

			_drawingGroup = new DrawingGroup();
			ImageSource = new DrawingImage(_drawingGroup);

			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();

			// TODO: Update the ScreenSectionCollection in the SetCanvasSize method.
			CanvasSize = new SizeInt(1024, 1024);
			var canvasSizeInBlocks = GetSizeInBlocks(CanvasSize, BlockSize);
			_screenSectionCollection = new ScreenSectionCollection(canvasSizeInBlocks, BlockSize, _drawingGroup);

			MapSections.CollectionChanged += MapSections_CollectionChanged;

			CanvasControlOffset = new VectorInt();
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public SizeInt BlockSize { get; }

		public SizeInt CanvasSize { get; private set; }

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set { _canvasControlOffset = value; OnPropertyChanged(); }
		}

		public Project CurrentProject { get; set; }

		public ObservableCollection<MapSection> MapSections { get; }

		#endregion

		#region Public Methods

		public void SetCanvasSize(SizeInt size)
		{
			CanvasSize = size;
			// TODO: Update the ScreenSectionCollection
		}

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
				var loadedSections = GetMapSectionsSnapShot();

				// Avoid requesting sections already drawn
				var sectionsToLoad = RemoveMapSectionsInPlay(sectionsRequired, loadedSections);

				// Remove from the screen sections that are not part of the updated view.
				var _ = UpdateMapSectionCollection(MapSections, sectionsRequired, out var shiftAmount);

				_screenSectionCollection.Shift(shiftAmount);
				RefreshScreenSections(MapSections);

				CanvasControlOffset = curJob?.CanvasControlOffset ?? new VectorInt();
				_mapLoaderManager.Push(curJob, sectionsToLoad);
			}
			else
			{
				MapSections.Clear();
				CanvasControlOffset = curJob?.CanvasControlOffset ?? new VectorInt();
				_mapLoaderManager.Push(curJob);
			}
		}

		private void MapLoaderManager_MapSectionReady(object sender, MapSection e)
		{
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
				RefreshScreenSections(mapSections);
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
			var jobName = GetJobName(transformType);
			var job = MapWindowHelper.BuildJob(parentJob, CurrentProject, jobName, CanvasSize, updatedInfo, transformType, newArea, BlockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {updatedInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			_jobStack.Push(job);

		}

		private void RefreshScreenSections(IEnumerable<MapSection> source)
		{
			foreach(var mapSection in source)
			{
				//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				_screenSectionCollection.Draw(mapSection);
			}
		}

		private IList<MapSection> RemoveMapSectionsInPlay(IList<MapSection> source, IReadOnlyList<MapSection> current)
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

		private int UpdateMapSectionCollection(ObservableCollection<MapSection> source, IList<MapSection> newSet, out VectorInt shiftAmount)
		{
			var cntUpdated = 0;
			IList<MapSection> toBeRemoved = new List<MapSection>();

			shiftAmount = new VectorInt();

			foreach (var mapSection in source)
			{
				var matchingNewSection = newSet.FirstOrDefault(x => x == mapSection);
				if(matchingNewSection == null)
				{
					toBeRemoved.Add(mapSection);
				}
				else
				{
					var diff = matchingNewSection.BlockPosition.Sub(mapSection.BlockPosition);

					if (cntUpdated == 0)
					{
						shiftAmount = diff;
					}
					else
					{
						if (shiftAmount != diff)
						//if (shiftAmount.X != diff.X || Math.Abs(shiftAmount.Y) != Math.Abs(diff.Y))
						{
							//throw new InvalidOperationException("The MapSection Collection contains inconsistent block positions.");
							Debug.WriteLine("The MapSection Collection contains inconsistent block positions.");
						}
					}

					mapSection.BlockPosition = matchingNewSection.BlockPosition;
					cntUpdated++;
				}
			}

			foreach(var mapSection in toBeRemoved)
			{
				if (!source.Remove(mapSection))
				{
					Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
				}
			}

			return cntUpdated;
		}

		private string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		private SizeInt GetSizeInBlocks(SizeInt canvasSize, SizeInt blockSize)
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, blockSize);
			var result = canvasSizeInBlocks.Inflate(2);

			// Always overide the above calculation and allocate 144 sections.
			if (result.Width > 0)
			{
				result = new SizeInt(12, 12);
			}

			return result;
		}

		private IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		#endregion
	}
}
