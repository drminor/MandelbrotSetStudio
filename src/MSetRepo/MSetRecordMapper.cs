using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;
using System.Linq;

namespace MSetRepo
{
	/// <summary>
	/// Maps 
	///		Project, 
	///		ColorBandSet, ColorBand
	///		Job, 
	///		Subdivision, MapSectionResponse
	///		RPoint, RSize, RRectangle,
	///		PointInt, SizeInt, VectorInt, BigVector,
	///		ColorBandBlendStyle,
	///		TransformType,
	/// </summary>
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, 
		IMapper<ColorBandSet, ColorBandSetRecord>, IMapper<ColorBand, ColorBandRecord>,
		IMapper<Job, JobRecord>, 
		IMapper<Subdivision, SubdivisionRecord>, //IMapper<MapSectionResponse, MapSectionRecord>,
		IMapper<RPoint, RPointRecord>, IMapper<RSize, RSizeRecord>, IMapper<RRectangle, RRectangleRecord>, IMapper<RPointAndDelta, RPointAndDeltaRecord>,
		IMapper<PointInt, PointIntRecord>, IMapper<SizeInt, SizeIntRecord>, IMapper<VectorInt, VectorIntRecord>, IMapper<BigVector, BigVectorRecord>
	{
		private readonly DtoMapper _dtoMapper;

		public MSetRecordMapper(DtoMapper dtoMapper)
		{
			_dtoMapper = dtoMapper;
		}
		
		#region Public Methods
		
		public Project MapFrom(ProjectRecord target)
		{
			throw new NotImplementedException();
		}

		public ProjectRecord MapTo(Project source)
		{
			var result = new ProjectRecord(source.Name, source.Description, source.CurrentJobId, source.LastSavedUtc)
			{
				Id = source.Id,
				LastAccessedUtc = source.LastAccessedUtc
			};

			return result;
		}

		public ColorBandSetRecord MapTo(ColorBandSet source)
		{
			var result = new ColorBandSetRecord(source.ParentId, source.ProjectId, source.Name, source.Description, source.Select(x => MapTo(x)).ToArray())
			{ 
				Id = source.Id, 
				//LastSaved = source.LastSavedUtc
				ReservedColorBandRecords = source.GetReservedColorBands().Select(x => MapTo(x)).ToArray(),
				ColorBandsSerialNumber = source.ColorBandsSerialNumber
			};

			return result;
		}

		public ColorBandSet MapFrom(ColorBandSetRecord target)
		{
			return new ColorBandSet(
				target.Id, target.ParentId, target.ProjectId, target.Name, target.Description, 
				target.ColorBandRecords.Select(x => MapFrom(x)).ToList(), 
				target.ReservedColorBandRecords?.Select(x => MapFrom(x)),
				target.ColorBandsSerialNumber
				);
		}

		public ColorBandRecord MapTo(ColorBand source)
		{
			return new ColorBandRecord(source.Cutoff, source.StartColor.GetCssColor(), source.BlendStyle.ToString(), source.EndColor.GetCssColor());
		}

		public ColorBand MapFrom(ColorBandRecord target)
		{
			return new ColorBand(target.CutOff, target.StartCssColor, MapFromBlendStyle(target.BlendStyle), target.EndCssColor);
		}

		public ReservedColorBandRecord MapTo(ReservedColorBand source)
		{
			return new ReservedColorBandRecord(source.StartColor.GetCssColor(), source.BlendStyle.ToString(), source.EndColor.GetCssColor());
		}

		public ReservedColorBand MapFrom(ReservedColorBandRecord target)
		{
			return new ReservedColorBand(target.StartCssColor, MapFromBlendStyle(target.BlendStyle), target.EndCssColor);
		}

		public ColorBandBlendStyle MapFromBlendStyle(string blendStyle)
		{
			return Enum.Parse<ColorBandBlendStyle>(blendStyle);
		}

		public TransformType MapFromTransformType(int transformType)
		{
			return Enum.Parse<TransformType>(transformType.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}

		public JobRecord MapTo(Job source)
		{
			var result = new JobRecord(
				ParentJobId: source.ParentJobId,
				OwnerId: source.OwnerId,
				JobOwnerType: source.JobOwnerType,
				SubDivisionId: source.Subdivision.Id,
				Label: source.Label,

				TransformType: (int)source.TransformType,

				MapAreaInfo2Record: MapTo(source.MapAreaInfo),
				TransformTypeString: Enum.GetName(source.TransformType) ?? "unknown",
				

				NewAreaPosition: MapTo(source.NewArea?.Position ?? new PointInt()),
				NewAreaSize: MapTo(source.NewArea?.Size ?? new SizeInt()), 
				ColorBandSetId: source.ColorBandSetId,
				MapCalcSettings: source.MapCalcSettings,
				LastSavedUtc: source.LastSavedUtc,
				LastAccessedUtc: source.LastAccessedUtc
				
				)
			{
				Id = source.Id,
				IterationUpdates = source.IterationUpdates,
				ColorMapUpdates = source.ColorMapUpdates,
			};

			return result;
		}

		public Job MapFrom(JobRecord target)
		{
			throw new NotImplementedException();
		}

		//public MapAreaInfo MapFrom(MapAreaInfoRecord target)
		//{
		//	var result = new MapAreaInfo(
		//		coords: _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto),
		//		canvasSize: MapFrom(target.CanvasSize),
		//		subdivision: MapFrom(target.SubdivisionRecord),
		//		precision: target.Precision ?? RMapConstants.DEFAULT_PRECISION,
		//		mapBlockOffset: MapFrom(target.MapBlockOffset),
		//		canvasControlOffset: MapFrom(target.CanvasControlOffset)
		//		);

		//	return result;
		//}

		//public MapAreaInfoRecord MapTo(MapAreaInfo source)
		//{
		//	var result = new MapAreaInfoRecord(
		//		CoordsRecord: MapTo(source.Coords),
		//		CanvasSize: MapTo(source.CanvasSize),
		//		SubdivisionRecord: MapTo(source.Subdivision),
		//		MapBlockOffset: MapTo(source.MapBlockOffset),
		//		CanvasControlOffset: MapTo(source.CanvasControlOffset)
		//		);

		//	result.Precision = source.Precision;

		//	return result;
		//}

		public MapAreaInfo2 MapFrom(MapAreaInfo2Record target)
		{
			var result = new MapAreaInfo2(
				rPointAndDelta: _dtoMapper.MapFrom(target.RPointAndDeltaRecord.RPointAndDeltaDto),
				subdivision: MapFrom(target.SubdivisionRecord),
				precision: target.Precsion,
				mapBlockOffset: MapFrom(target.MapBlockOffset),
				canvasControlOffset: MapFrom(target.CanvasControlOffset)
				);

			return result;
		}

		public MapAreaInfo2Record MapTo(MapAreaInfo2 source)
		{
			var result = new MapAreaInfo2Record(
				RPointAndDeltaRecord: MapTo(source.PositionAndDelta),
				SubdivisionRecord: MapTo(source.Subdivision),
				MapBlockOffset: MapTo(source.MapBlockOffset),
				CanvasControlOffset: MapTo(source.CanvasControlOffset),
				Precsion: source.Precision
				);

			return result;
		}



		public Poster MapFrom(PosterRecord target)
		{
			throw new NotImplementedException();
		}

		public PosterRecord MapTo(Poster source)
		{
			// TODO: Update all PosterRecords to use double instead of int for the Width and Height

			var posterSizeRounded = source.PosterSize.Round(MidpointRounding.AwayFromZero);

			var result = new PosterRecord(
				Name: source.Name,
				Description: source.Description,
				SourceJobId: source.SourceJobId,
				CurrentJobId: source.CurrentJobId,
				DisplayPosition: MapTo(source.DisplayPosition),
				DisplayZoom: source.DisplayZoom,
				DateCreatedUtc: source.DateCreatedUtc,
				LastSavedUtc: source.LastSavedUtc,
				LastAccessedUtc: source.LastAccessedUtc)
			{
				Id = source.Id,
				Width = posterSizeRounded.Width,
				Height = posterSizeRounded.Height
			};

			return result;
		}

		public Subdivision MapFrom(SubdivisionRecord target)
		{
			var samplePointDelta = _dtoMapper.MapFrom(target.SamplePointDelta.Size);



			var baseMapPosition = _dtoMapper.MapFrom(target.BaseMapPosition?.BigVector ?? new BigVectorDto()) ;

			var result = new Subdivision(target.Id, samplePointDelta, baseMapPosition, MapFrom(target.BlockSize));

			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var baseMapPosition = MapTo(source.BaseMapPosition);
			var samplePointDelta = MapTo(source.SamplePointDelta);

			var result = new SubdivisionRecord(baseMapPosition, samplePointDelta, MapTo(source.BlockSize))
			{
				Id = source.Id,
				BaseMapPosition = MapTo(source.BaseMapPosition)
			};

			return result;
		}

		#endregion

		#region Public Methods - MapSection

		public MapSectionRecord MapTo(MapSectionResponse source)
		{
			// TODO: Create a new type: LongVector to hold the RepoBlockPosition, instead of using a pair of longs as does the BigVector

			if (source.MapSectionVectors == null)
			{
				throw new InvalidOperationException("The MapSectionRespone has a null MapSectionVectors.");
			}

			var blockPositionDto = _dtoMapper.MapTo(source.BlockPosition);

			var result = new MapSectionRecord
				(
				DateCreatedUtc: DateTime.UtcNow,
				SubdivisionId: new ObjectId(source.SubdivisionId),

				BlockPosXHi: blockPositionDto.X[0],
				BlockPosXLo: blockPositionDto.X[1],
				BlockPosYHi: blockPositionDto.Y[0],
				BlockPosYLo: blockPositionDto.Y[1],

				MapCalcSettings: source.MapCalcSettings ?? throw new ArgumentNullException(),

				Counts: source.MapSectionVectors.GetSerializedCounts(),
				EscapeVelocities: source.MapSectionVectors.GetSerializedEscapeVelocities(),
				AllRowsHaveEscaped: source.AllRowsHaveEscaped
				)
			{
				Id = source.MapSectionId is null ? ObjectId.GenerateNewId() : new ObjectId(source.MapSectionId),
				Complete = source.RequestCompleted,
				LastAccessed = DateTime.UtcNow,
			};

			return result;
		}

		public MapSectionResponse MapFrom(MapSectionRecord target, MapSectionVectors mapSectionVectors)
		{
			var blockPosition = GetBlockPosition(target.BlockPosXHi, target.BlockPosXLo, target.BlockPosYHi, target.BlockPosYLo);

			mapSectionVectors.Load(target.Counts, target.EscapeVelocities);

			var result = new MapSectionResponse
			(
				mapSectionId: target.Id.ToString(),
				jobId: string.Empty,
				jobOwnerType: JobOwnerType.Undetermined,
				subdivisionId: target.SubdivisionId.ToString(),
				originalSourceSubdivisionId: ObjectId.Empty.ToString(),
				blockPosition: blockPosition,
				mapCalcSettings: target.MapCalcSettings,
				requestCompleted: target.Complete,
				allRowsHaveEscaped: target.AllRowsHaveEscaped,
				mapSectionVectors: mapSectionVectors
			);

			return result;
		}

		private BigVector GetBlockPosition(long blockPosXHi, long blockPosXLo, long blockPosYHi, long blockPosYLo)
		{
			var blockPosition = new BigVectorDto(new long[][]
				{
					new long[] { blockPosXHi, blockPosXLo }, 
					new long[] { blockPosYHi, blockPosYLo }
				});

			var result = _dtoMapper.MapFrom(blockPosition);

			return result;
		}

		#endregion

		#region Public Methods - PointInt, SizeInt, RPoint, etc.

		public PointIntRecord MapTo(PointInt source)
		{
			return new PointIntRecord(source.X, source.Y);
		}

		public PointInt MapFrom(PointIntRecord target)
		{
			return new PointInt(target.X, target.Y);
		}

		public SizeIntRecord MapTo(SizeInt source)
		{
			return new SizeIntRecord(source.Width, source.Height);
		}

		public SizeInt MapFrom(SizeIntRecord target)
		{
			return new SizeInt(target.Width, target.Height);
		}

		public VectorIntRecord MapTo(VectorInt source)
		{
			return new VectorIntRecord(source.X, source.Y);
		}

		public VectorInt MapFrom(VectorIntRecord target)
		{
			return new VectorInt(target.X, target.Y);
		}

		public VectorDblRecord MapTo(VectorDbl source)
		{
			return new VectorDblRecord(source.X, source.Y);
		}

		public VectorDbl MapFrom(VectorDblRecord target)
		{
			return new VectorDbl(target.X, target.Y);
		}



		public BigVectorRecord MapTo(BigVector bigVector)
		{
			var bigVectorDto = _dtoMapper.MapTo(bigVector);
			var display = bigVector.ToString() ?? "0";
			var result = new BigVectorRecord(display, bigVectorDto);

			return result;
		}

		public BigVector MapFrom(BigVectorRecord target)
		{
			var result = _dtoMapper.MapFrom(target.BigVector);

			return result;
		}

		public RPointRecord MapTo(RPoint rPoint)
		{
			var rPointDto = _dtoMapper.MapTo(rPoint);
			var display = rPoint.ToString();
			var result = new RPointRecord(display, rPointDto);

			return result;
		}

		public RPoint MapFrom(RPointRecord target)
		{
			return _dtoMapper.MapFrom(target.PointDto);
		}

		public RSizeRecord MapTo(RSize rSize)
		{
			var rSizeDto = _dtoMapper.MapTo(rSize);
			var display = rSize.ToString();
			var result = new RSizeRecord(display, rSizeDto);

			return result;
		}

		public RSize MapFrom(RSizeRecord target)
		{
			return _dtoMapper.MapFrom(target.Size);
		}



		public RVectorRecord MapTo(RVector rSize)
		{
			var rSizeDto = _dtoMapper.MapTo(rSize);
			var display = rSize.ToString();
			var result = new RVectorRecord(display, rSizeDto);

			return result;
		}

		public RVector MapFrom(RVectorRecord target)
		{
			return _dtoMapper.MapFrom(target.Vector);
		}

		public RRectangleRecord MapTo(RRectangle rRectangle)
		{
			var rRectangleDto = _dtoMapper.MapTo(rRectangle);
			var display = rRectangle.ToString();
			var result = new RRectangleRecord(display, rRectangleDto);

			return result;
		}

		public RRectangle MapFrom(RRectangleRecord target)
		{
			return _dtoMapper.MapFrom(target.CoordsDto);
		}

		public RPointAndDeltaRecord MapTo(RPointAndDelta source)
		{
			var rPointAndDeltaDto = _dtoMapper.MapTo(source);
			var display = source.ToString();
			var result = new RPointAndDeltaRecord(display, rPointAndDeltaDto);

			return result;
		}

		public RPointAndDelta MapFrom(RPointAndDeltaRecord target)
		{
			return _dtoMapper.MapFrom(target.RPointAndDeltaDto);
		}


		#endregion
	}
}
