using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WpfMapDisplayPOC
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Private Properties

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly DtoMapper _dtoMapper;

		private int _targetIterations;
		private ColorMap _colorMap;

		private int _currentJobNumber;

		#endregion

		#region Constructor

		public MainWindowViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapSectionHelper mapSectionHelper)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionHelper = mapSectionHelper;
			_dtoMapper = new DtoMapper();

			_targetIterations = 400;
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(_targetIterations);
			_colorMap = new ColorMap(colorBandSet);

			//MapSectionIds = new List<ObjectId>();
			MapSectionRequests = new List<MapSectionRequest>();

			_currentJobNumber = -1;
			CurrentJobId = ObjectId.Empty;
			Job = null;
		}

		#endregion

		#region Public Properties

		public int CurrentJobNumber => _currentJobNumber;

		public ObjectId CurrentJobId { get; private set; }

		public Job? Job { get; private set; }

		public IList<MapSectionRequest> MapSectionRequests { get; private set; }

		//public IList<ObjectId> MapSectionIds { get; init; }

		#endregion

		#region Public Methods

		public int Load(string jobId)
		{
			_currentJobNumber++;

			CurrentJobId = ObjectId.Parse(jobId);

			Job = _projectAdapter.GetJob(CurrentJobId);

			if (Job.MapCalcSettings.TargetIterations != _targetIterations)
			{
				_targetIterations = Job.MapCalcSettings.TargetIterations;
				var colorBandSet = RMapConstants.BuildInitialColorBandSet(_targetIterations);
				_colorMap = new ColorMap(colorBandSet);
			}

			MapSectionRequests = _mapSectionHelper.CreateSectionRequests(CurrentJobId.ToString(), JobOwnerType.Project, Job.MapAreaInfo, Job.MapCalcSettings);

			//MapSectionIds.Clear();

			//var sectionIds = _mapSectionAdapter.GetMapSectionIds(CurrentJobId, JobOwnerType.Project);

			//foreach(var sectionId in sectionIds)
			//{
			//	MapSectionIds.Add(sectionId);
			//}

			return _currentJobNumber;
		}

		public MapSection? GetMapSection(MapSectionRequest mapSectionRequest, int jobNumber = 1)
		{
			var mapSectionVectors = _mapSectionHelper.ObtainMapSectionVectors();

			var mapSectionResponse = GetMapSectionResponse(mapSectionRequest, mapSectionVectors);

			if (mapSectionResponse != null)
			{
				var result = _mapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionVectors, jobNumber);
				return result;
			}
			else
			{
				return null;
			}
		}

		public bool TryGetPixelArray(MapSection mapSection, [MaybeNullWhen(false)] out byte[] pixelArray)
		{
			if (mapSection.MapSectionVectors != null)
			{
				pixelArray = _mapSectionHelper.GetPixelArray(mapSection.MapSectionVectors, RMapConstants.BLOCK_SIZE, _colorMap, mapSection.IsInverted, useEscapeVelocities: false);
				return true;
			}
			else
			{
				pixelArray = null;
				return false;
			}
		}

		#endregion

		#region Private Methods

		private MapSectionResponse? GetMapSectionResponse(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors)
		{
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var blockPosition = _dtoMapper.MapTo(mapSectionRequest.BlockPosition);

			var mapSectionResponse = _mapSectionAdapter.GetMapSection(subdivisionId, blockPosition, mapSectionVectors);

			return mapSectionResponse;
		}

		#endregion

		//public MapSection GetMapSection(ObjectId mapSectionId)
		//{
		//	var mapSectionVectors = _mapSectionHelper.ObtainMapSectionVectors();

		//	var mapSectionResponse = _mapSectionAdapter.GetMapSection(mapSectionId, mapSectionVectors);

		//	if (mapSectionResponse == null)
		//	{
		//		throw new KeyNotFoundException($"No MapSection on file for MapSectionId: {mapSectionId}; Job: {CurrentJobId}.");
		//	}

		//	var jobNumber = 1;
		//	var repoBlockPosition = mapSectionResponse.BlockPosition;
		//	var mapBlockOffset = new BigVector();	// Used to get the screen position
		//	var isInverted = false;					// Used to get the screen position
		//	var subdivisionId = mapSectionResponse.SubdivisionId;
		//	var blockSize = RMapConstants.BLOCK_SIZE;
		//	var targetIterations = mapSectionResponse.MapCalcSettings.TargetIterations;

		//	var result = _mapSectionHelper.CreateMapSection(jobNumber, repoBlockPosition, mapBlockOffset, isInverted, subdivisionId, blockSize, mapSectionVectors, targetIterations);

		//	//var result = _mapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionVectors, jobId: 1, new BigVector());

		//	return result;
		//}

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
