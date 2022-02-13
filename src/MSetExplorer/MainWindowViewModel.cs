using MapSectionProviderLib;
using MongoDB.Bson;
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

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMapJobViewModel
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly List<GenMapRequestInfo> _requestStack;
		private int _requestStackPointer;

		private readonly Action<MapSection> _onMapSectionReady;
		private readonly object hmsLock = new();

		#region Constructor

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requestStack = new List<GenMapRequestInfo>();
			_requestStackPointer = -1;

			Project = _projectAdapter.GetOrCreateProject("Home");

			_mapCoords = new RRectangle();
			_mapCalcSettings = new MapCalcSettings();
			_colorMapEntries = Array.Empty<ColorMapEntry>();
			_canvasSize = new SizeInt();

			MapSections = new ObservableCollection<MapSection>();

			var mapLoadingProgress = new Progress<MapSection>(HandleMapSectionReady);
			_onMapSectionReady = ((IProgress<MapSection>)mapLoadingProgress).Report;
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

		private GenMapRequestInfo CurrentRequest => _requestStackPointer == -1 ? null : _requestStack[_requestStackPointer];
		private int? CurrentJobNumber => CurrentRequest?.JobNumber;

		public Job CurrentJob => CurrentRequest?.Job;
		public bool CanGoBack => _requestStackPointer > 0;
		public bool CanGoForward => _requestStackPointer < _requestStack.Count - 1;

		public ObservableCollection<MapSection> MapSections { get; init; }

		public IEnumerable<Job> GetJobs()
		{
			List<Job> result = new List<Job>();

			foreach(var genMapRequestInfo in _requestStack)
			{
				result.Add(genMapRequestInfo.Job);
			}

			return result;
		}

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
			if (CanGoBack)
			{
				LoadMap(_requestStackPointer - 1);
			}
		}

		public void GoForward()
		{
			if (CanGoForward)
			{
				LoadMap(_requestStackPointer + 1);
			}
		}

		public Point GetBlockPosition(Point posYInverted)
		{
			var pointInt = new PointInt((int)posYInverted.X, (int)posYInverted.Y);

			var curReq = CurrentRequest;
			var mapBlockOffset = curReq?.Job?.MapBlockOffset ?? new SizeInt();

			var blockPos = RMapHelper.GetBlockPosition(pointInt, mapBlockOffset, BlockSize);

			return new Point(blockPos.X, blockPos.Y);
		}

		public void Save()
		{
			foreach (var genMapRequestInfo in _requestStack)
			{
				var job = genMapRequestInfo.Job;
				if (job.Id.Equals(ObjectId.Empty))
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					genMapRequestInfo.UpdateJob(updatedJob);
				}
			}
		}

		public void Load()
		{
			//_projectAdapter.GetJob()
		}

		#endregion

		#region Private Methods 

		private void LoadMap(TransformType transformType, SizeInt newArea)
		{
			var curReq = CurrentRequest;
			curReq?.MapLoader?.Stop();
			MapSections.Clear();

			var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
			var jobName = GetJobName(transformType);
			var canvasSize = CanvasSize;
			var mSetInfo = new MSetInfo(MapCoords, MapCalcSettings, ColorMapEntries);

			var job = MapWindowHelper.BuildJob(Project, jobName, canvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);
			Debug.WriteLine($"The new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.");

			var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);
			var genMapRequestInfo = new GenMapRequestInfo(job, mapLoader, jobNumber);

			lock (hmsLock)
			{
				_requestStack.Add(genMapRequestInfo);
				_requestStackPointer = _requestStack.Count - 1;
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);

				OnPropertyChanged("CanGoBack");
				OnPropertyChanged("CanGoForward");
			}
		}

		private void LoadMap(int newRequestStackPointer)
		{
			var curReq = CurrentRequest;
			curReq?.MapLoader?.Stop();
			MapSections.Clear();

			_requestStackPointer = newRequestStackPointer;

			var genMapRequestInfo = CurrentRequest;
			var job = genMapRequestInfo.Job;

			var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
			var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);

			genMapRequestInfo.Renew(jobNumber, mapLoader);

			lock (hmsLock)
			{
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);
				OnPropertyChanged("CanGoBack");
				OnPropertyChanged("CanGoForward");
			}
		}

		private string GetJobName(TransformType transformType)
		{
			//var opName = transformType == TransformType.None ? "Home" : transformType.ToString();
			//var result = $"{opName}:{jobNumber.ToString(CultureInfo.InvariantCulture)}";
			//return result;

			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		private void HandleMapSection(int jobNumber, MapSection mapSection)
		{
			lock (hmsLock)
			{
				if (jobNumber == CurrentJobNumber)
				{
					_onMapSectionReady(mapSection);
				}
				else
				{
					Debug.WriteLine($"HandleMapSection is ignoring the new section. CurJobNum:{CurrentJobNumber}, Handling JobNum: {jobNumber}.");
				}
			}
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			MapSections.Add(mapSection);
		}

		#endregion
	}
}
