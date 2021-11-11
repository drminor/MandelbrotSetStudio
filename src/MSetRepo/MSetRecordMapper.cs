using MSS.Common;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;

namespace MSetRepo
{
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, IMapper<Job, JobRecord>
	{
		private readonly IMapper<RRectangle, RRectangleDto> _dtoMapper;
		private readonly CoordsHelper _coordsHelper;

		public MSetRecordMapper(IMapper<RRectangle, RRectangleDto> dtoMapper, CoordsHelper coordsHelper )
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

			var result = new Job(target.Id, target.ProjectId, target.ParentJobId, target.Operation, target.OperationAmount, target.Label, target.CanvasSize,
				coords, target.MaxInterations, target.Threshold, target.IterationsPerStep, target.ColorMapEntries, target.HighColorCss);

			return result;
		}

		public JobRecord MapTo(Job source)
		{
			var coords = _coordsHelper.BuildCoords(source.Coords);

			var result = new JobRecord(source.ProjectId, source.ParentJobId, source.Operation, source.OperationAmount, source.Label, source.CanvasSize,
				coords, source.MaxInterations, source.Threshold, source.IterationsPerStep, source.ColorMapEntries, source.HighColorCss, null);

			return result;
		}
	}
}
