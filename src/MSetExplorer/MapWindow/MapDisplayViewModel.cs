using MapSectionProviderLib;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly DrawingGroup _drawingGroup;
		private readonly IMapLoaderJobStack _mapLoaderJobStack;
		private readonly IScreenSectionCollection _screenSectionCollection;

		private SizeInt _canvasSize;
		private VectorInt _canvasControlOffset;

		#region Constructor

		public MapDisplayViewModel(ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;

			_mapLoaderJobStack = new MapLoaderJobStack(mapSectionRequestProcessor);
			_mapLoaderJobStack.CurrentJobChanged += MapLoaderJobStack_CurrentJobChanged;
			_mapLoaderJobStack.MapSectionReady += MapLoaderJobStack_MapSectionReady;
			_drawingGroup = new DrawingGroup();
			ImageSource = new DrawingImage(_drawingGroup);

			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();

			// TODO: Update the ScreenSectionCollection in the SetCanvasSize method.
			_canvasSize = new SizeInt(1024, 1024);
			var canvasSizeInBlocks = GetSizeInBlocks(_canvasSize, BlockSize);
			_screenSectionCollection = new ScreenSectionCollection(canvasSizeInBlocks, BlockSize, _drawingGroup);

			_ = new MapSectionCollectionBinder(_screenSectionCollection, MapSections);

			MapSections.CollectionChanged += MapSections_CollectionChanged;


			CanvasControlOffset = new VectorInt();
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public SizeInt BlockSize { get; }

		//public SizeInt CanvasSize
		//{
		//	get => _canvasSize;
		//	set
		//	{
		//		_canvasSize = value;
		//		Clip(new PointInt(CanvasControlOffset));
		//		OnPropertyChanged();
		//	}
		//}

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set
			{
				_canvasControlOffset = value;
				//Clip(new PointInt(value));
				OnPropertyChanged(); }
		}

		public Project CurrentProject { get; set; }

		public ObservableCollection<MapSection> MapSections { get; }

		public Job CurrentJob => _mapLoaderJobStack.CurrentJob;

		public IEnumerable<Job> Jobs => _mapLoaderJobStack.Jobs;

		public bool CanGoBack => _mapLoaderJobStack.CanGoBack;
		public bool CanGoForward => _mapLoaderJobStack.CanGoForward;

		#endregion

		#region Public Methods

		public void SetCanvasSize(SizeInt size)
		{
			_canvasSize = size;
			// TODO: Update the ScreenSectionCollection
		}

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			var newArea = new RectangleInt(new PointInt(), _canvasSize);
			LoadMap(mSetInfo, TransformType.None, newArea);
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
			var newArea = new RectangleInt(new PointInt(invOffset), _canvasSize);
			UpdateMapView(TransformType.Pan, newArea);
		}

		#endregion

		#region MapLoaderJobStack Methods

		public void GoBack()
		{
			_mapLoaderJobStack.StopCurrentJob();
			MapSections.Clear();
			_ = _mapLoaderJobStack.GoBack();
		}

		public void GoForward()
		{
			_mapLoaderJobStack.StopCurrentJob();
			MapSections.Clear();
			_ = _mapLoaderJobStack.GoForward();
		}

		public void LoadJobStack(IEnumerable<Job> jobs)
		{
			_mapLoaderJobStack.StopCurrentJob();
			MapSections.Clear();
			_mapLoaderJobStack.LoadJobStack(jobs);
		}

		public void UpdateJob(Job oldJob, Job newJob)
		{
			_mapLoaderJobStack.UpdateJob(oldJob, newJob);
		}

		#endregion

		#region Event Handlers

		private void MapLoaderJobStack_CurrentJobChanged(object sender, EventArgs e)
		{
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
			CanvasControlOffset = CurrentJob.CanvasControlOffset;
		}

		private void MapLoaderJobStack_MapSectionReady(object sender, MapSection e)
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
				foreach (var mapSection in GetList(e.NewItems))
				{
					if (!_screenSectionCollection.Hide(mapSection))
					{
						Debug.WriteLine($" MapSecCollBindr cannot Hide the MapSection: {mapSection}.");
					}
				}
			}
		}

		private IEnumerable<MapSection> GetList(IList lst)
		{
			return lst?.Cast<MapSection>() ?? new List<MapSection>();
		}


		#endregion

		#region Private Methods

		private void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			var mSetInfo = CurrentJob.MSetInfo;
			var updatedInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);

			//if (Iterations > 0 && Iterations != updatedInfo.MapCalcSettings.MaxIterations)
			//{
			//	updatedInfo = MSetInfo.UpdateWithNewIterations(updatedInfo, Iterations, Steps);
			//}

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {TransformType.Zoom}. SamplePointDelta: {samplePointDelta}");
			LoadMap(updatedInfo, transformType, newArea);
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		{
			var parentJob = CurrentJob;
			var jobName = GetJobName(transformType);
			var job = MapWindowHelper.BuildJob(parentJob, CurrentProject, jobName, _canvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

			//Debug.WriteLine($"\nThe new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.\n");

			_mapLoaderJobStack.StopCurrentJob();

			if (transformType == TransformType.Pan)
			{
				var sectionsRequired = MapLoader.CreateEmptyMapSections(job);
				var loadedSections = GetMapSectionsSnapShot();

				// Avoid requesting sections already drawn
				var sectionsToLoad = RemoveMapSectionsInPlay(sectionsRequired, loadedSections);

				// Remove from the screen sections that are not part of the updated view.
				UpdateMapSectionCollection(MapSections, sectionsRequired, out var shiftAmount);

				//var shiftAmountPixels = new VectorInt(newArea.Position).Invert();
				//var shiftAmountInBlocks = shiftAmountPixels.Divide(BlockSize); //new VectorInt(1, 0);
				//_ = _screenSectionCollection.Shift(shiftAmountInBlocks);

				_screenSectionCollection.Shift(shiftAmount);
				RefreshScreenSections(MapSections);

				var oldOffset = CanvasControlOffset;
				_mapLoaderJobStack.Push(job, sectionsToLoad);

				//Debug.WriteLine($"Pan completed. ShiftPix: {shiftAmountPixels}, ShiftBks: {shiftAmountInBlocks}. cco old: {oldOffset}, new: {CanvasControlOffset}.");
				Debug.WriteLine($"Pan completed. ShiftBks: {shiftAmount}. CanvasOffset old: {oldOffset}, new: {CanvasControlOffset}.");
			}
			else
			{
				MapSections.Clear();
				_mapLoaderJobStack.Push(job);
			}
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

		private void UpdateMapSectionCollection(ObservableCollection<MapSection> source, IList<MapSection> newSet, out VectorInt shiftAmount)
		{
			IList<MapSection> toBeRemoved = new List<MapSection>();

			shiftAmount = new VectorInt();
			var foundAMatch = false;

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

					if (!foundAMatch)
					{
						shiftAmount = diff;
					}
					else
					{
						if (shiftAmount != diff)
						{
							throw new InvalidOperationException("The MapSection Collection contains inconsistent block positions.");
						}
					}

					mapSection.BlockPosition = matchingNewSection.BlockPosition;
				}
			}

			foreach(var mapSection in toBeRemoved)
			{
				if (!source.Remove(mapSection))
				{
					Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
				}
			}
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

		private void Clip(PointInt bottomLeft)
		{
			if (!(_screenSectionCollection is null))
			{
				var drawingGroupSize = _screenSectionCollection.CanvasSizeInBlocks.Scale(BlockSize);
				var rect = new Rect(new Point(bottomLeft.X, drawingGroupSize.Height - _canvasSize.Height - bottomLeft.Y), new Point(_canvasSize.Width + bottomLeft.X, drawingGroupSize.Height - bottomLeft.Y));

				//Debug.WriteLine($"The clip rect is {rect}.");
				_drawingGroup.ClipGeometry = new RectangleGeometry(rect);
			}
		}

		//private void ResetMapDisplay(VectorInt canvasControOffset)
		//{
		//	CanvasControlOffset = canvasControOffset;
		//	MapSections.Clear();
		//}

		private IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		#endregion
	}
}
