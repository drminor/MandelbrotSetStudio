using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MSetRepo
{
	/// <summary>
	/// Maps 
	///		Project, 
	///		ColorBandSet, ColorBand
	///		Job, MSetInfo, 
	///		Subdivision, MapSectionResponse
	///		RPoint, RSize, RRectangle,
	///		PointInt, SizeInt, VectorInt, BigVector,
	///		ColorBandBlendStyle,
	///		TransformType,
	/// </summary>
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, 
		IMapper<ColorBandSet, ColorBandSetRecord>, IMapper<ColorBand, ColorBandRecord>,
		IMapper<Job, JobRecord>, 
		//IMapper<MSetInfo, MSetInfoRecord>,
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
			var mSetInfoRecord = new MSetInfoRecord(coords, source.MapCalcSettings);

			var result = new JobRecord(
				source.ParentJobId,
				source.IsPreferredChild,
				source.ProjectId,
				source.Subdivision.Id,
				source.Label,

				(int)source.TransformType,
				MapTo(source.NewArea?.Position ?? new PointInt()),
				MapTo(source.NewArea?.Size ?? new SizeInt()), 
				mSetInfoRecord,
				source.ColorBandSetId,
				MapTo(source.MapBlockOffset),
				MapTo(source.CanvasControlOffset),
				MapTo(source.CanvasSizeInBlocks)
				)
			{
				Id = source.Id,
				LastSaved = source.LastSavedUtc,
				CanvasSize = MapTo(source.CanvasSize)
			};

			return result;
		}

		public Job MapFrom(JobRecord target)
		{
			throw new NotImplementedException();
		}

		public JobAreaInfo MapFrom(JobAreaInfoRecord target)
		{
			var result = new JobAreaInfo(
				coords: _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto),
				canvasSize: MapFrom(target.CanvasSize),
				subdivision: MapFrom(target.SubdivisionRecord),
				mapBlockOffset: MapFrom(target.MapBlockOffset),
				canvasControlOffset: MapFrom(target.CanvasControlOffset)
				);

			return result;
		}

		public JobAreaInfoRecord MapTo(JobAreaInfo source)
		{
			var result = new JobAreaInfoRecord(
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
				SubdivisionId: source.SubdivisionId,
				JobAreaInfoRecord: MapTo(source.JobAreaInfo), 
				ColorBandSetId: source.ColorBandSet.Id, 
				MapCalcSettings: source.MapCalcSettings, 
				DateCreatedUtc: source.DateCreatedUtc, 
				LastSavedUtc: source.LastSavedUtc,
				LastAccessedUtc: source.LastAccessedUtc);

			result.Id = source.Id;

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
			var result = new SubdivisionRecord(samplePointDelta, MapTo(source.BlockSize));

			return result;
		}

		public MapSectionRecord MapTo(MapSectionResponse source)
		{
			var result = new MapSectionRecord
				(
				DateCreatedUtc: DateTime.UtcNow,
				SubdivisionId: new ObjectId(source.SubdivisionId),
				BlockPosXHi: source.BlockPosition.X[0],
				BlockPosXLo: source.BlockPosition.X[1],
				BlockPosYHi: source.BlockPosition.Y[0],
				BlockPosYLo: source.BlockPosition.Y[1],
				source.MapCalcSettings,
				source.Counts,
				source.DoneFlags,
				source.ZValues
				)
			{
				LastAccessed = DateTime.UtcNow,
			};

			return result;
		}

		public MapSectionResponse MapFrom(MapSectionRecord target)
		{
			var x = new long[][] { new long[] { 0, 0 }, new long[]{ 0, 0 } };

			var blockPosition = new BigVectorDto(new long[][] { new long[] { target.BlockPosXHi, target.BlockPosXLo }, new long[] { target.BlockPosYHi, target.BlockPosYLo } });

			var result = new MapSectionResponse
			{
				MapSectionId = target.Id.ToString(),
				SubdivisionId = target.SubdivisionId.ToString(),
				BlockPosition = blockPosition,
				MapCalcSettings = target.MapCalcSettings,
				Counts = target.Counts,
				DoneFlags = target.DoneFlags,
				ZValues = target.ZValues
			};

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
