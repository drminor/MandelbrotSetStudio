using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;

namespace MSetRepo
{
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, IMapper<Job, JobRecord>, IMapper<MSetInfo, MSetInfoRecord>, IMapper<Subdivision, SubdivisionRecord>
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
				MapTo(source.MSetInfo),
				source.CanvasOffset);

			return result;
		}

		public MSetInfo MapFrom(MSetInfoRecord target)
		{
			var coords = _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto);
			var result = new MSetInfo(
				canvasSize: new SizeInt(target.CanvasSizeWidth, target.CanvasSizeHeight),
				coords: coords,
				mapCalcSettings: target.MapCalcSettings,
				target.ColorMapEntries,
				target.HighColorCss);

			return result;
		}

		public MSetInfoRecord MapTo(MSetInfo source)
		{
			var coords = _coordsHelper.BuildCoords(source.Coords);

			var result = new MSetInfoRecord(source.CanvasSize.Width, source.CanvasSize.Height, coords, source.MapCalcSettings, source.ColorMapEntries, source.HighColorCss);

			return result;
		}

		public Subdivision MapFrom(SubdivisionRecord target)
		{
			var position = _dtoMapper.MapFrom(target.Position.PointDto);
			var samplePointDelta = _dtoMapper.MapFrom(target.SamplePointDelta.SizeDto);

			var result = new Subdivision(target.Id, position, new SizeInt(target.BlockWidth, target.BlockHeight), samplePointDelta);
			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var position = _coordsHelper.BuildPointRecord(source.Position);
			var samplePointDelta = _coordsHelper.BuildSizeRecord(source.SamplePointDelta);

			var result = new SubdivisionRecord(position, source.BlockSize.Width, source.BlockSize.Height, samplePointDelta);
			return result;
		}
	}
}
