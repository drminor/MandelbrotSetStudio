using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;

namespace MSetRepo
{
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, IMapper<Job, JobRecord>
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
			var coords = _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto);

			var result = new Job(target.Id, target.Label, target.ProjectId, target.ParentJobId, target.CanvasSize,
				coords, target.SubDivisionId,
				target.MaxInterations, target.Threshold, target.IterationsPerStep, target.ColorMapEntries, target.HighColorCss);

			return result;
		}

		public JobRecord MapTo(Job source)
		{
			var coords = _coordsHelper.BuildCoords(source.Coords);

			var result = new JobRecord(source.Label, source.ProjectId, source.ParentJobId, source.CanvasSize, 
				coords, source.SubdivisionId, 
				source.MaxInterations, source.Threshold, source.IterationsPerStep, source.ColorMapEntries, source.HighColorCss);

			return result;
		}
	}
}
