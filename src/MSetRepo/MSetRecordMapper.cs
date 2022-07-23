using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;
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
		IMapper<Subdivision, SubdivisionRecord>, IMapper<MapSectionResponse, MapSectionRecord>,
		IMapper<RPoint, RPointRecord>, IMapper<RSize, RSizeRecord>, IMapper<RRectangle, RRectangleRecord>,
		IMapper<PointInt, PointIntRecord>, IMapper<SizeInt, SizeIntRecord>, IMapper<VectorInt, VectorIntRecord>, IMapper<BigVector, BigVectorRecord>
	{
		private readonly DtoMapper _dtoMapper;

		public MSetRecordMapper(DtoMapper dtoMapper)
		{
			_dtoMapper = dtoMapper;
		}
		
		public Project MapFrom(ProjectRecord target)
		{
			throw new NotImplementedException();
		}

		public ProjectRecord MapTo(Project source)
		{
			var result = new ProjectRecord(source.Name, source.Description, source.CurrentJobId, source.LastSavedUtc)
			{
				Id = source.Id
			};

			return result;
		}

		public ColorBandSetRecord MapTo(ColorBandSet source)
		{
			var result = new ColorBandSetRecord(source.ParentId, source.ProjectId, source.Name, source.Description, source.Select(x => MapTo(x)).ToArray())
			{ 
				Id = source.Id, 
				//LastSaved = source.LastSavedUtc
				ReservedColorBandRecords = source.GetReservedColorBands().Select(x => MapTo(x)).ToArray()
			};

			return result;
		}

		public ColorBandSet MapFrom(ColorBandSetRecord target)
		{
			return new ColorBandSet(
				target.Id, target.ParentId, target.ProjectId, target.Name, target.Description, 
				target.ColorBandRecords.Select(x => MapFrom(x)).ToList(), 
				target.ReservedColorBandRecords?.Select(x => MapFrom(x))
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
			var coords = MapTo(source.Coords);
			var mapAreaInfoRecord = new MapAreaInfoRecord(coords, MapTo(source.CanvasSize), MapTo(source.Subdivision), MapTo(source.MapBlockOffset), MapTo(source.CanvasControlOffset));

			var result = new JobRecord(
				source.ParentJobId,
				source.IsPreferredChild,
				source.ProjectId,
				source.Subdivision.Id,
				source.Label,

				(int)source.TransformType,

				MapAreaInfoRecord: mapAreaInfoRecord,
				TransformTypeString: Enum.GetName(source.TransformType) ?? "unknown",
				

				NewAreaPosition: MapTo(source.NewArea?.Position ?? new PointInt()),
				NewAreaSize: MapTo(source.NewArea?.Size ?? new SizeInt()), 
				ColorBandSetId: source.ColorBandSetId,
				MapCalcSettings: source.MapCalcSettings,
				CanvasSizeInBlocks: MapTo(source.CanvasSizeInBlocks)
				)
			{
				Id = source.Id,
				LastSaved = source.LastSavedUtc
			};

			return result;
		}

		public Job MapFrom(JobRecord target)
		{
			throw new NotImplementedException();
		}

		public MapAreaInfo MapFrom(MapAreaInfoRecord target)
		{
			var result = new MapAreaInfo(
				coords: _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto),
				canvasSize: MapFrom(target.CanvasSize),
				subdivision: MapFrom(target.SubdivisionRecord),
				mapBlockOffset: MapFrom(target.MapBlockOffset),
				canvasControlOffset: MapFrom(target.CanvasControlOffset)
				);

			return result;
		}

		public MapAreaInfoRecord MapTo(MapAreaInfo source)
		{
			var result = new MapAreaInfoRecord(
				CoordsRecord: MapTo(source.Coords),
				CanvasSize: MapTo(source.CanvasSize),
				SubdivisionRecord: MapTo(source.Subdivision),
				MapBlockOffset: MapTo(source.MapBlockOffset),
				CanvasControlOffset: MapTo(source.CanvasControlOffset)
				);

			return result;
		}

		public Poster MapFrom(PosterRecord target)
		{
			throw new NotImplementedException();
		}

		public PosterRecord MapTo(Poster source)
		{
			var result = new PosterRecord(
				Name: source.Name,
				Description: source.Description,
				SourceJobId: source.SourceJobId,
				JobAreaInfoRecord: MapTo(source.MapAreaInfo),
				ColorBandSetId: source.ColorBandSet.Id,
				MapCalcSettings: source.MapCalcSettings,
				DisplayPosition: MapTo(source.DisplayPosition),
				DisplayZoom: source.DisplayZoom,
				DateCreatedUtc: source.DateCreatedUtc,
				LastSavedUtc: source.LastSavedUtc,
				LastAccessedUtc: source.LastAccessedUtc)
			{
				Id = source.Id
			};

			return result;
		}

		public Subdivision MapFrom(SubdivisionRecord target)
		{
			var samplePointDelta = _dtoMapper.MapFrom(target.SamplePointDelta.Size);
			var result = new Subdivision(target.Id, samplePointDelta, MapFrom(target.BlockSize));

			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var samplePointDelta = MapTo(source.SamplePointDelta);
			var result = new SubdivisionRecord(samplePointDelta, MapTo(source.BlockSize))
			{
				Id = source.Id
			};

			return result;
		}

		/// <summary>
		/// Take a response from the MEngineService and prepare it for storing in the repo.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public MapSectionRecord MapTo(MapSectionResponse source)
		{
			ZValues zVals;

			try
			{
				zVals = new ZValues(source.ZValuesForLocalStorage);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While parsing the ZValues as a MapSectionRecord is created from a MapSectionResponse, an exception was encountered: {e}.");
				throw;
			}

			var result = new MapSectionRecord
				(
				DateCreatedUtc: DateTime.UtcNow,
				SubdivisionId: new ObjectId(source.SubdivisionId),
				BlockPosXHi: source.BlockPosition.X[0],
				BlockPosXLo: source.BlockPosition.X[1],
				BlockPosYHi: source.BlockPosition.Y[0],
				BlockPosYLo: source.BlockPosition.Y[1],

				source.MapCalcSettings,
				Counts: GetBytes(source.Counts),
				EscapeVelocities: GetBytes(source.EscapeVelocities),
				DoneFlags: GetBytes(source.DoneFlags),
				ZValues: zVals
				)
			{
				Id = source.MapSectionId is null ? ObjectId.GenerateNewId() : new ObjectId(source.MapSectionId),
				LastAccessed = DateTime.UtcNow,
			};

			return result;
		}

		private byte[] GetBytes(ushort[] uShorts)
		{
			var result = new byte[uShorts.Length * 2];

			for (var i = 0; i < uShorts.Length; i++)
			{
				BitConverter.TryWriteBytes(new Span<byte>(result, i * 2, 2), uShorts[i]);
			}

			return result;
		}

		private byte[] GetBytes(bool[] bools)
		{
			var result = new byte[bools.Length];

			for (var i = 0; i < bools.Length; i++)
			{
				BitConverter.TryWriteBytes(new Span<byte>(result, i, 1), bools[i]);
			}

			return result;
		}

		// Take a record from the repo and prepare it for display.
		public MapSectionResponse MapFrom(MapSectionRecord target)
		{
			var blockPosition = new BigVectorDto(new long[][] { new long[] { target.BlockPosXHi, target.BlockPosXLo }, new long[] { target.BlockPosYHi, target.BlockPosYLo } });

			var result = new MapSectionResponse
			(
				mapSectionId: target.Id.ToString(),
				ownerId: string.Empty,
				jobOwnerType: (int)JobOwnerType.Undetermined,
				subdivisionId:  target.SubdivisionId.ToString(),
				blockPosition: blockPosition,
				mapCalcSettings: target.MapCalcSettings,
				counts: GetUShorts(target.Counts),

				escapeVelocities:  GetUShorts(target.EscapeVelocities),
				doneFlags: GetBools(target.DoneFlags),
				zValues: target.ZValues.GetZValuesAsDoubleArray()
			);

			return result;
		}

		private ushort[] GetUShorts(byte[] raw)
		{
			var result = new ushort[raw.Length / 2];

			for(var i = 0; i < result.Length; i++)
			{
				result[i] = BitConverter.ToUInt16(raw, i * 2);
			}

			return result;
		}

		private bool[] GetBools(byte[] raw)
		{
			var result = new bool[raw.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = BitConverter.ToBoolean(raw, i);
			}

			return result;
		}

		public MapSectionResponse MapFrom(MapSectionRecordJustCounts target)
		{
			var blockPosition = new BigVectorDto(new long[][] { new long[] { target.BlockPosXHi, target.BlockPosXLo }, new long[] { target.BlockPosYHi, target.BlockPosYLo } });

			var result = new MapSectionResponse
			(
				mapSectionId: target.Id.ToString(),
				ownerId: string.Empty,
				jobOwnerType: JobOwnerType.Poster,
				subdivisionId: target.SubdivisionId.ToString(),
				blockPosition: blockPosition,
				mapCalcSettings: target.MapCalcSettings,
				counts: GetUShorts(target.Counts),

				escapeVelocities: GetUShorts(target.EscapeVelocities),
				doneFlags: GetBools(target.DoneFlags),
				zValues: null
			);

			return result;
		}

		#region IMapper<Shape, ShapeRecord> Support

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

		#endregion
	}
}
