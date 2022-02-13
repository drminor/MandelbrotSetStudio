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
using System.Globalization;
using System.Windows;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMapJobViewModel
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly List<GenMapRequestInfo> _requestStack;
		private readonly Action<MapSection> _onMapSectionReady;


		private readonly object hmsLock = new();

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requestStack = new List<GenMapRequestInfo>();

			Project = new Project(ObjectId.GenerateNewId(), "uncommitted");

			_mapCoords = new RRectangle();
			_mapCalcSettings = new MapCalcSettings();
			_colorMapEntries = Array.Empty<ColorMapEntry>();
			_canvasSize = new SizeInt();

			MapSections = new ObservableCollection<MapSection>();

			var mapLoadingProgress = new Progress<MapSection>(HandleMapSectionReady);
			_onMapSectionReady = ((IProgress<MapSection>)mapLoadingProgress).Report;
		}

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

		private GenMapRequestInfo CurrentRequest => _requestStack.Count == 0 ? null : _requestStack[^1];
		private int? CurrentGenMapRequestId => CurrentRequest?.JobNumber;

		public Job CurrentJob => CurrentRequest?.Job;
		public bool CanGoBack => _requestStack.Count > 1;

		public ObservableCollection<MapSection> MapSections { get; init; }

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
			// Remove the current request
			_requestStack.RemoveAt(_requestStack.Count - 1);

			// Remove and then reload the one prior to that
			var prevRequest = _requestStack[^1];
			_requestStack.RemoveAt(_requestStack.Count - 1);

			MapCoords = prevRequest.Job.MSetInfo.Coords;

			var newArea = prevRequest.NewArea;
			var transformType = prevRequest.TransformType;
			LoadMap(transformType, newArea);
		}

		public Point GetBlockPosition(Point posYInverted)
		{
			var pointInt = new PointInt((int)posYInverted.X, (int)posYInverted.Y);

			var curReq = CurrentRequest;
			var mapBlockOffset = curReq?.Job?.MapBlockOffset ?? new SizeInt();

			var blockPos = RMapHelper.GetBlockPosition(pointInt, mapBlockOffset, BlockSize);

			return new Point(blockPos.X, blockPos.Y);
		}

		public void ClearMapSections(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			_ = MapWindowHelper.BuildJob(Project, "temp", canvasControlSize, mSetInfo, BlockSize, _projectAdapter, clearExistingMapSections: true);
		}

		#endregion

		#region Private Methods 

		private void LoadMap(TransformType transformType, SizeInt? newArea)
		{
			var curReq = CurrentRequest;
			curReq?.MapLoader?.Stop();
			MapSections.Clear();

			var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
			var jobName = GetJobName(jobNumber, transformType);
			var canvasSize = CanvasSize;
			var mSetInfo = new MSetInfo(MapCoords, MapCalcSettings, ColorMapEntries);

			var job = MapWindowHelper.BuildJob(Project, jobName, canvasSize, mSetInfo, newArea, BlockSize, _projectAdapter, clearExistingMapSections: false);
			Debug.WriteLine($"The new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and a Offset of {job.CanvasControlOffset}.");

			var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);
			var genMapRequestInfo = new GenMapRequestInfo(job, jobNumber, transformType, newArea, mapLoader);

			lock (hmsLock)
			{
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);
				_requestStack.Add(genMapRequestInfo);
				OnPropertyChanged("CanGoBack");
			}
		}

		private string GetJobName(int jobNumber, TransformType transformType)
		{
			var opName = transformType == TransformType.None ? "Home" : transformType.ToString();
			var result = $"{opName}:{jobNumber.ToString(CultureInfo.InvariantCulture)}";

			return result;
		}

		private void HandleMapSection(int genMapRequestId, MapSection mapSection)
		{
			lock (hmsLock)
			{
				if (genMapRequestId == CurrentGenMapRequestId)
				{
					_onMapSectionReady(mapSection);
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
