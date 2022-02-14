using MapSectionProviderLib;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMapJobViewModel
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapLoaderJobStack _navStack;

		#region Constructor

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;

			Project = _projectAdapter.GetOrCreateProject("Home");

			_mapCoords = new RRectangle();
			_mapCalcSettings = new MapCalcSettings();
			_colorMapEntries = Array.Empty<ColorMapEntry>();
			_canvasSize = new SizeInt();

			MapSections = new ObservableCollection<MapSection>();

			_navStack = new MapLoaderJobStack(mapSectionRequestProcessor, HandleMapSectionReady, HandleMapNav);
		}

		#endregion

		#region Public Properties

		private MapCalcSettings _mapCalcSettings;
		public MapCalcSettings MapCalcSettings
		{
			get => _mapCalcSettings;
			set { _mapCalcSettings = value; OnPropertyChanged(); }
		}

		private ColorMapEntry[] _colorMapEntries;
		public ColorMapEntry[] ColorMapEntries
		{
			get => _colorMapEntries;
			set { _colorMapEntries = value; OnPropertyChanged(); }
		}

		private RRectangle _mapCoords;
		public RRectangle MapCoords
		{
			get => _mapCoords;
			set
			{
				_mapCoords = value;
				OnPropertyChanged();
			}
		}

		private SizeInt _canvasSize;
		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set { _canvasSize = value; OnPropertyChanged(); }
		}

		public Project Project { get; private set; }
		public SizeInt BlockSize { get; init; }

		public ObservableCollection<MapSection> MapSections { get; init; }

		public Job CurrentJob => _navStack.CurrentJob;
		public bool CanGoBack => _navStack.CanGoBack;
		public bool CanGoForward => _navStack.CanGoForward;

		#endregion

		#region Public Methods

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			MapCalcSettings = mSetInfo.MapCalcSettings;
			ColorMapEntries = mSetInfo.ColorMapEntries;
			MapCoords = mSetInfo.Coords;

			LoadMap(transformType: TransformType.None, newArea: new SizeInt());
		}

		public void UpdateMapView(TransformType transformType, SizeInt newArea, RRectangle coords)
		{
			MapCoords = coords;
			LoadMap(transformType, newArea);
		}

		public void GoBack()
		{
			if (_navStack.GoBack())
			{
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		public void GoForward()
		{
			if (_navStack.GoForward())
			{
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		public Point GetBlockPosition(Point posYInverted)
		{
			var pointInt = new PointInt((int)posYInverted.X, (int)posYInverted.Y);

			var curReq = _navStack.CurrentRequest;
			var mapBlockOffset = curReq?.Job?.MapBlockOffset ?? new SizeInt();

			var blockPos = RMapHelper.GetBlockPosition(pointInt, mapBlockOffset, BlockSize);

			return new Point(blockPos.X, blockPos.Y);
		}

		public void SaveProject()
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(Project.Id);

			foreach (var genMapRequestInfo in _navStack.GenMapRequests)
			{
				var job = genMapRequestInfo.Job;
				if (job.Id.CreationTime < lastSavedTime)
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					genMapRequestInfo.UpdateJob(updatedJob);
				}
			}
		}

		public void LoadProject()
		{
			//_projectAdapter.GetJob()
		}

		#endregion

		#region Private Methods 

		private void LoadMap(TransformType transformType, SizeInt newArea)
		{
			var jobName = GetJobName(transformType);
			var canvasSize = CanvasSize;
			var mSetInfo = new MSetInfo(MapCoords, MapCalcSettings, ColorMapEntries);

			var parentJob = _navStack.CurrentRequest?.Job;
			var job = MapWindowHelper.BuildJob(parentJob, Project, jobName, canvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);
			Debug.WriteLine($"The new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.");

			_navStack.Push(job);
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
		}

		private string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			MapSections.Add(mapSection);
		}

		private void HandleMapNav()
		{
			MapSections.Clear();
		}

		#endregion
	}
}
