using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;

namespace MSetRepo
{
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, IMapper<Job, JobRecord>, IMapper<MSetInfo, MSetInfoRecord>,
		IMapper<Subdivision, SubdivisionRecord>, IMapper<MapSectionResponse?, MapSectionRecord?>,
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
			var result = new Project(target.Id, target.Name);
			return result;
		}

		public ProjectRecord MapTo(Project source)
		{
			var result = new ProjectRecord(source.Name);
			return result;
		}

		public Job MapFrom(JobRecord target)
		{
			throw new NotImplementedException();
		}

		public JobRecord MapTo(Job source)
		{
			var result = new JobRecord(
				source.ParentJob?.Id,
				source.Project.Id,
				source.Subdivision.Id,
				source.Label,

				(int) source.TransformType,
				MapTo(source.NewArea.Position),
				MapTo(source.NewArea.Size),
				MapTo(source.MSetInfo),
				MapTo(source.CanvasSizeInBlocks),
				MapTo(source.MapBlockOffset),
				MapTo(source.CanvasControlOffset)
				);

			return result;
		}

		public MSetInfo MapFrom(MSetInfoRecord target)
		{
			var coords = _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto);

			var result = new MSetInfo(
				coords: coords,
				mapCalcSettings: target.MapCalcSettings,
				target.ColorMapEntries);

			return result;
		}

		public MSetInfoRecord MapTo(MSetInfo source)
		{
			var coords = MapTo(source.Coords);
			var result = new MSetInfoRecord(coords, source.MapCalcSettings, source.ColorMapEntries);

			return result;
		}

		public Subdivision MapFrom(SubdivisionRecord target)
		{
			var samplePointDelta = _dtoMapper.MapFrom(target.SamplePointDelta.Size);
			var result = new Subdivision(target.Id, samplePointDelta, new SizeInt(target.BlockWidth, target.BlockHeight));

			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var samplePointDelta = MapTo(source.SamplePointDelta);
			var result = new SubdivisionRecord(samplePointDelta, source.BlockSize.Width, source.BlockSize.Height);

			return result;
		}

		public MapSectionRecord? MapTo(MapSectionResponse? source)
		{
			if (source is null)
			{
				return null;
			}

			var blockPosition = _dtoMapper.MapFrom(source.BlockPosition);
			var blockPositionRecord = MapTo(blockPosition);
			var result = new MapSectionRecord
				(
				new ObjectId(source.SubdivisionId),
				blockPositionRecord,
				source.Counts
				);

			return result;
		}

		public MapSectionResponse? MapFrom(MapSectionRecord? target)
		{
			if (target is null)
			{
				return null;
			}

			var result = new MapSectionResponse
			{
				MapSectionId = target.Id.ToString(),
				SubdivisionId = target.SubdivisionId.ToString(),
				BlockPosition = target.BlockPosition.BigVector,
				Counts = target.Counts
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
			var display = BigIntegerHelper.GetDisplay(rPoint);
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
			var display = BigIntegerHelper.GetDisplay(rSize);
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
			var display = BigIntegerHelper.GetDisplay(rRectangle);
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
