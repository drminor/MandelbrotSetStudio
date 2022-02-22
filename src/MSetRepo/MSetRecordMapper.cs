using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;

namespace MSetRepo
{
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, IMapper<Job, JobRecord>, IMapper<MSetInfo, MSetInfoRecord>, 
		IMapper<Subdivision, SubdivisionRecord>, IMapper<MapSectionResponse?, MapSectionRecord?>, IMapper<BigVector, BigVectorRecord>
	{
		private readonly DtoMapper _dtoMapper;
		private readonly CoordsHelper _coordsHelper;

		public MSetRecordMapper(DtoMapper dtoMapper, CoordsHelper coordsHelper )
		{
			_dtoMapper = dtoMapper;
			_coordsHelper = coordsHelper;
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
				source.NewArea.X1,
				source.NewArea.Y1,
				source.NewArea.Width,
				source.NewArea.Height,
				MapTo(source.MSetInfo),
				source.CanvasSizeInBlocks.Width,
				source.CanvasSizeInBlocks.Height,
				MapTo(source.MapBlockOffset),
				source.CanvasControlOffset.Width,
				source.CanvasControlOffset.Height);

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
			var coords = _coordsHelper.BuildCoords(source.Coords);
			var result = new MSetInfoRecord(coords, source.MapCalcSettings, source.ColorMapEntries);

			return result;
		}

		public Subdivision MapFrom(SubdivisionRecord target)
		{
			var position = _dtoMapper.MapFrom(target.Position.PointDto);
			var samplePointDelta = _dtoMapper.MapFrom(target.SamplePointDelta.SizeDto);
			var result = new Subdivision(target.Id, position, samplePointDelta, new SizeInt(target.BlockWidth, target.BlockHeight));

			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var position = _coordsHelper.BuildPointRecord(source.Position);
			var samplePointDelta = _coordsHelper.BuildSizeRecord(source.SamplePointDelta);
			var result = new SubdivisionRecord(position, samplePointDelta, source.BlockSize.Width, source.BlockSize.Height);

			return result;
		}

		public MapSectionRecord? MapTo(MapSectionResponse? source)
		{
			if (source is null)
			{
				return null;
			}

			var blockPositionRecord = _coordsHelper.BuildBigVectorRecord(source.BlockPosition);
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
				BlockPosition = target.BlockPosition.BigVectorDto,
				Counts = target.Counts
			};

			return result;
		}

		public BigVectorRecord MapTo(BigVector bigVector)
		{
			var result = _coordsHelper.BuildBigVectorRecord(bigVector);
			return result;
		}

		public BigVector MapFrom(BigVectorRecord target)
		{
			var result = _dtoMapper.MapFrom(target.BigVectorDto);

			return result;
		}
	}
}
