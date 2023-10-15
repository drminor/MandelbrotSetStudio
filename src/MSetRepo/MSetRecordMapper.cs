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
	///		RPoint, RSize, RRectangle, RPointAndDelta
	///		PointInt, SizeInt, VectorInt, BigVector,
	///		ColorBandBlendStyle,
	///		TransformType,
	/// </summary>
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, 
		IMapper<ColorBandSet, ColorBandSetRecord>, IMapper<ColorBand, ColorBandRecord>,
		IMapper<Job, JobRecord>, 
		IMapper<Subdivision, SubdivisionRecord>, //IMapper<MapSectionResponse, MapSectionRecord>, IMapper<MapSectionBytes, MapSectionRecord>
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
				DateCreatedUtc = source.DateCreated,
				IterationUpdates = source.IterationUpdates,
				ColorMapUpdates = source.ColorMapUpdates,
			};

			return result;
		}

		public Job MapFrom(JobRecord target)
		{
			throw new NotImplementedException();
		}

		public MapCenterAndDelta MapFrom(MapAreaInfo2Record target)
		{
			var bv = MapFrom(target.MapBlockOffset);

			if (!bv.TryConvertToLong(out var jobMapBlockOffset))
			{
				throw new InvalidOperationException("The MapSectionRecord's BlockPos will not fit into a LongVector.");
			}

			var result = new MapCenterAndDelta(
				rPointAndDelta: _dtoMapper.MapFrom(target.RPointAndDeltaRecord.RPointAndDeltaDto),
				subdivision: MapFrom(target.SubdivisionRecord),
				precision: target.Precsion,
				mapBlockOffset: jobMapBlockOffset,
				canvasControlOffset: MapFrom(target.CanvasControlOffset)
				);

			return result;
		}

		public MapAreaInfo2Record MapTo(MapCenterAndDelta source)
		{
			var result = new MapAreaInfo2Record(
				RPointAndDeltaRecord: MapTo(source.PositionAndDelta),
				SubdivisionRecord: MapTo(source.Subdivision),
				MapBlockOffset: MapTo(new BigVector(source.MapBlockOffset.X, source.MapBlockOffset.Y)),
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

			var baseMapPosition = _dtoMapper.MapFrom(target.BaseMapPosition.BigVector);

			var result = new Subdivision(target.Id, samplePointDelta, baseMapPosition, MapFrom(target.BlockSize), target.DateCreatedUtc);

			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var baseMapPosition = MapTo(source.BaseMapPosition);
			var samplePointDelta = MapTo(source.SamplePointDelta);

			var result = new SubdivisionRecord(baseMapPosition, samplePointDelta, MapTo(source.BlockSize))
			{
				Id = source.Id,
				DateCreatedUtc = source.DateCreatedUtc
			};

			return result;
		}

		#endregion

		#region Public Methods - MapSection

		public MapSectionRecord MapTo(MapSectionResponse source)
		{
			if (source.MapSectionVectors2 == null)
			{
				throw new InvalidOperationException("The MapSectionResponse::MapSectionVectors is null.");
			}

			MapSectionRecord result;


			result = new MapSectionRecord
				(
				DateCreatedUtc: DateTime.UtcNow,
				SubdivisionId: new ObjectId(source.SubdivisionId),

				BlockPosXHi: 0,
				BlockPosXLo: source.BlockPosition.X,
				BlockPosYHi: 0,
				BlockPosYLo: source.BlockPosition.Y,

				MapCalcSettings: source.MapCalcSettings ?? throw new ArgumentNullException(),

				Counts: source.MapSectionVectors2.Counts,
				EscapeVelocities: source.MapSectionVectors2.EscapeVelocities,

				AllRowsHaveEscaped: source.AllRowsHaveEscaped
				)
			{
				Id = source.MapSectionId is null ? ObjectId.GenerateNewId() : new ObjectId(source.MapSectionId),
				Complete = source.RequestCompleted,
				LastAccessed = DateTime.UtcNow,
			};

			return result;
		}

		public MapSectionResponse MapFrom(MapSectionBytes target, MapSectionVectors mapSectionVectors)
		{
			mapSectionVectors.Load(target.Counts, target.EscapeVelocities);

			var result = new MapSectionResponse
			(
				mapSectionId: target.Id.ToString(),
				subdivisionId: target.SubdivisionId.ToString(),
				blockPosition: target.BlockPosition,
				mapCalcSettings: target.MapCalcSettings,
				requestCompleted: target.RequestWasCompleted,
				allRowsHaveEscaped: target.AllRowsHaveEscaped,
				mapSectionVectors: mapSectionVectors
			);

			return result;
		}

		public MapSectionBytes MapFrom(MapSectionRecord target)
		{
			var blockPosition = GetBlockPosition(target.BlockPosXHi, target.BlockPosXLo, target.BlockPosYHi, target.BlockPosYLo);

			var result = new MapSectionBytes
			(
				mapSectionId: target.Id,
				dateCreatedUtc: target.DateCreatedUtc, lastSavedUtc: target.LastSavedUtc, lastAccessed: target.LastAccessed, subdivisionId: target.SubdivisionId,
				blockPosition: blockPosition,
				mapCalcSettings: target.MapCalcSettings,
				requestWasCompleted: target.RequestWasCompleted,
				allRowsHaveEscaped: target.AllRowsHaveEscaped,
				counts: target.Counts,
				escapeVelocities: target.EscapeVelocities
			);

			return result;
		}

		private VectorLong GetBlockPosition(long blockPosXHi, long blockPosXLo, long blockPosYHi, long blockPosYLo)
		{
			if (blockPosXHi != 0 || blockPosYHi != 0)
			{
				throw new InvalidOperationException("The MapSectionRecord's BlockPos will not fit into a LongVector.");
			}

			var result = new VectorLong(blockPosXLo, blockPosYLo);

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

		public RVectorRecord MapTo(RVector rVector)
		{
			var rSizeDto = _dtoMapper.MapTo(rVector);
			var display = rVector.ToString();
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
