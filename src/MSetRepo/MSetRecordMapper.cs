using MEngineDataContracts;
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
	public class MSetRecordMapper : IMapper<Project, ProjectRecord>, IMapper<Job, JobRecord>, IMapper<MSetInfo, MSetInfoRecord>, 
		IMapper<Subdivision, SubdivisionRecord>, IMapper<MapSectionResponse?, MapSectionRecord?>
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
				source.CanvasSizeInBlocks.Width,
				source.CanvasSizeInBlocks.Height,
				source.CanvasBlockOffset.X,
				source.CanvasBlockOffset.Y,
				source.CanvasControlOffset.X,
				source.CanvasControlOffset.Y);

			return result;
		}

		public MSetInfo MapFrom(MSetInfoRecord target)
		{
			var coords = _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto);
			var result = new MSetInfo(
				coords: coords,
				mapCalcSettings: target.MapCalcSettings,
				target.ColorMapEntries,
				target.HighColorCss);

			return result;
		}

		public MSetInfoRecord MapTo(MSetInfo source)
		{
			var coords = _coordsHelper.BuildCoords(source.Coords);

			var result = new MSetInfoRecord(coords, source.MapCalcSettings, source.ColorMapEntries, source.HighColorCss);

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

		public MapSectionRecord? MapTo(MapSectionResponse? source)
		{
			if (source is null) return null;

			var result = new MapSectionRecord
				(
				new ObjectId(source.SubdivisionId),
				source.BlockPosition.X,
				source.BlockPosition.Y,
				//GetAbb(source.Counts)
				source.Counts
				);
			return result;
		}

		public MapSectionResponse? MapFrom(MapSectionRecord? target)
		{
			if (target is null) return null;

			var result = new MapSectionResponse
			{
				MapSectionId = target.Id.ToString(),
				SubdivisionId = target.SubdivisionId.ToString(),
				BlockPosition = new PointInt(target.BlockPositionX, target.BlockPositionY),
				//Counts = GetAbb(target.Counts)
				Counts = target.Counts
			};

			return result;
		}

		//private int[] GetAbb(int[] source)
		//{
		//	if (source is null) return new int[0];

		//	if(source.Length > 100)
		//	{
		//		int[] result = new int[100];
		//		Array.Copy(source, result, 100);
		//		return result;
		//	}
		//	else
		//	{
		//		int[] result = new int[source.Length];
		//		Array.Copy(source, result, source.Length);
		//		return result;
		//	}

		//}
	}
}
