using MapSectionProviderLib;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
			CanvasSize = new SizeInt(1024, 1024);

			var canvasSizeInBlocks = GetSizeInBlocks(CanvasSize, BlockSize);
			_screenSectionCollection = new ScreenSectionCollection(canvasSizeInBlocks, BlockSize, _drawingGroup);

			_ = new MapSectionCollectionBinder(_screenSectionCollection, MapSections);

			CanvasControlOffset = new VectorInt();
		}

		private void MapLoaderJobStack_MapSectionReady(object sender, MapSection e)
		{
			MapSections.Add(e);
		}

		#endregion

		#region Event Handlers

		private void MapLoaderJobStack_CurrentJobChanged(object sender, EventArgs e)
		{
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public SizeInt BlockSize { get; }

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				_canvasSize = value;
				Clip(new PointInt(CanvasControlOffset));
				OnPropertyChanged();
			}
		}

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set
			{
				_canvasControlOffset = value;
				Clip(new PointInt(value));
				OnPropertyChanged(); }
		}

		public ObservableCollection<MapSection> MapSections { get; }

		public Project CurrentProject { get; set; }

		public Job CurrentJob => _mapLoaderJobStack.CurrentJob;

		public IEnumerable<Job> Jobs => _mapLoaderJobStack.Jobs;

		public bool CanGoBack => _mapLoaderJobStack.CanGoBack;
		public bool CanGoForward => _mapLoaderJobStack.CanGoForward;

		#endregion

		#region Public Methods

		public void GoBack()
		{
			_mapLoaderJobStack.StopCurrentJob();
			ResetMapDisplay(CurrentJob.CanvasControlOffset);
			_ = _mapLoaderJobStack.GoBack();
		}

		public void GoForward()
		{
			_mapLoaderJobStack.StopCurrentJob();
			ResetMapDisplay(CurrentJob.CanvasControlOffset);
			_ = _mapLoaderJobStack.GoForward();
		}

		public void LoadJobStack(IEnumerable<Job> jobs)
		{
			_mapLoaderJobStack.StopCurrentJob();
			ResetMapDisplay(new VectorInt());
			_mapLoaderJobStack.LoadJobStack(jobs);
		}

		public void UpdateJob(Job oldJob, Job newJob)
		{
			_mapLoaderJobStack.UpdateJob(oldJob, newJob);
		}

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			var newArea = new RectangleInt(new PointInt(), CanvasSize);
			LoadMap(mSetInfo, TransformType.None, newArea);
		}

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var newArea = e.Area;
			UpdateMapView(TransformType.Zoom, newArea);
		}

		public void UpdateMapViewPan(ScreenPannedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var newArea = new RectangleInt(new PointInt(invOffset), CanvasSize);
			UpdateMapView(TransformType.Pan, newArea);
		}

		public IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		public void ShiftMapSections(VectorInt amount)
		{

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
			var job = MapWindowHelper.BuildJob(parentJob, CurrentProject, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

			//Debug.WriteLine($"\nThe new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.\n");

			_mapLoaderJobStack.StopCurrentJob();
			ResetMapDisplay(CurrentJob?.CanvasControlOffset ?? new VectorInt());
			var requests = MapLoader.CreateSectionRequests(job);
			_mapLoaderJobStack.Push(job, requests);
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

			// Always overide the above calculation and allocate 400 sections.
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
				var rect = new Rect(new Point(bottomLeft.X, drawingGroupSize.Height - CanvasSize.Height - bottomLeft.Y), new Point(CanvasSize.Width + bottomLeft.X, drawingGroupSize.Height - bottomLeft.Y));

				//Debug.WriteLine($"The clip rect is {rect}.");
				_drawingGroup.ClipGeometry = new RectangleGeometry(rect);
			}
		}

		private void ResetMapDisplay(VectorInt canvasControOffset)
		{
			CanvasControlOffset = canvasControOffset;
			MapSections.Clear();
		}


		#endregion
	}
}
