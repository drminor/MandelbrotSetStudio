using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSetRepo
{
	public class ProjectAdapter
	{
		public const string ROOT_JOB_LABEL = "Root";

		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		private readonly DtoMapper _dtoMapper;

		public ProjectAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;

			_dtoMapper = new DtoMapper();
		}

		public void CreateCollections()
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.CreateCollection();

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.CreateCollection();

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subdivisionReaderWriter.CreateCollection();

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			mapSectionReaderWriter.CreateCollection();
		}

		#region Project

		public Project GetOrCreateProject(string name)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			var projectRecord = projectReaderWriter.Get(name);
			if (projectRecord is null)
			{
				var projectId = projectReaderWriter.Insert(new ProjectRecord(name));
				projectRecord = projectReaderWriter.Get(projectId);
			}

			var result = _mSetRecordMapper.MapFrom(projectRecord);

			return result;
		}

		/// <summary>
		/// Inserts the project record if it does not exist on the database.
		/// </summary>
		/// <param name="project"></param>
		public Project InsertProject(Project project, bool overwrite)
		{
			ProjectRecord projectRecord;

			var projectName = project.Name;
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			projectRecord = projectReaderWriter.Get(projectName);

			if (projectRecord == null)
			{
				projectRecord = _mSetRecordMapper.MapTo(project);
				_ = projectReaderWriter.Insert(projectRecord);
			}
			else
			{
				if (!overwrite)
				{
					throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already exists.");
				}
				else
				{
					var projectId = projectRecord.Id;

					var jobReaderWriter = new JobReaderWriter(_dbProvider);

					var jobIds = jobReaderWriter.GetJobIds(projectId);

					foreach (var jobId in jobIds)
					{
						var dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
						Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
					}

					_ = projectReaderWriter.Delete(projectId);

					projectRecord = _mSetRecordMapper.MapTo(project);
					_ = projectReaderWriter.Insert(projectRecord);
				}
			}

			project = _mSetRecordMapper.MapFrom(projectRecord);

			return project;
		}

		#endregion

		#region Job

		public Job GetJob(ObjectId jobId)
		{
			Debug.WriteLine($"Retrieving Job object for JobId: {jobId}.");
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var job = GetJob(jobId, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter);

			return job;
		}

		private Job GetJob(ObjectId jobId, JobReaderWriter jobReaderWriter, ProjectReaderWriter projectReaderWriter, SubdivisonReaderWriter subdivisonReaderWriter)
		{
			var jobRecord = jobReaderWriter.Get(jobId);

			Job? parentJob;

			if (jobRecord.ParentJobId.HasValue)
			{
				Debug.WriteLine($"Retrieving Job object for parent JobId: {jobRecord.ParentJobId}.");
				parentJob = GetJob(jobRecord.ParentJobId.Value, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter);
			}
			else
			{
				parentJob = null;
			}

			var projectRecord = projectReaderWriter.Get(jobRecord.ProjectId);
			var project = _mSetRecordMapper.MapFrom(projectRecord);

			var subdivisionRecord = subdivisonReaderWriter.Get(jobRecord.SubDivisionId);
			var subdivision = _mSetRecordMapper.MapFrom(subdivisionRecord);

			var mSetInfo = _mSetRecordMapper.MapFrom(jobRecord.MSetInfo);

			var job = new Job(
				id: jobId, 
				parentJob: parentJob, 
				project: project, 
				subdivision: subdivision, 
				label: jobRecord.Label, 
				mSetInfo: mSetInfo, 
				canvasSizeInBlocks: new SizeInt(jobRecord.CanvasSizeInBlocksWidth, jobRecord.CanvasSizeInBlocksHeight), 
				mapBlockOffset: new SizeInt(jobRecord.MapBlockOffsetWidth, jobRecord.MapBlockOffsetHeight), 
				canvasControlOffset: new SizeDbl(jobRecord.CanvasControlOffsetWidth, jobRecord.CanvasControlOffsetHeight)
				);

			return job;
		}

		//public ObjectId CreateJob(Project project, MSetInfo mSetInfo, SizeInt blockSize, bool overwrite)
		//{
		//	ObjectId result;

		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

		//	var subdivisionId = GetSubdivision(mSetInfo.Coords.Exponent, subdivisionReaderWriter);

		//	// TODO: calculate the CanvasSize, CanvasBlockOffset and CanvasControlOffset
		//	var canvasSizeInBlocks = new SizeInt(6, 6);
		//	var canvasBlockOffset = new PointInt(-4, -3);
		//	var canvasControlOffset = new PointDbl(); 

		//	if (!subdivisionId.HasValue)
		//	{
		//		var subdivision = JobHelperNotUsed.CreateSubdivision(blockSize, mSetInfo.Coords);
		//		subdivisionId = InsertSubdivision(subdivision, subdivisionReaderWriter);
		//	}

		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	var jobIds = jobReaderWriter.GetJobIds(project.Id);

		//	if (jobIds.Length == 0)
		//	{
		//		result = CreateAndInsertFirstJob(project.Id, subdivisionId.Value, mSetInfo, canvasSizeInBlocks, canvasBlockOffset, canvasControlOffset, jobReaderWriter);
		//	}
		//	else
		//	{
		//		if (!overwrite)
		//		{
		//			throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already has at least one job.");
		//		}
		//		else
		//		{
		//			foreach (var jobId in jobIds)
		//			{
		//				var dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
		//				Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
		//			}

		//			result = CreateAndInsertFirstJob(project.Id, subdivisionId.Value, mSetInfo, canvasSizeInBlocks, canvasBlockOffset, canvasControlOffset, jobReaderWriter);
		//		}
		//	}

		//	return result;
		//}

		private ObjectId CreateAndInsertFirstJob(ObjectId projectId, ObjectId subdivisionId, MSetInfo mSetInfo, SizeInt canvasSizeInBlocks, PointInt canvasBlockOffset, PointDbl canvasControlOffset, JobReaderWriter jobReaderWriter)
		{
			var jobRecord = CreateJob(null, projectId, subdivisionId, ROOT_JOB_LABEL, mSetInfo, canvasSizeInBlocks, canvasBlockOffset, canvasControlOffset);
			var result = jobReaderWriter.Insert(jobRecord);

			return result;
		}

		private JobRecord CreateJob(ObjectId? parentJobId, ObjectId projectId, ObjectId subdivisionId, string label, MSetInfo mSetInfo, SizeInt canvasSizeInBlocks, PointInt canvasBlockOffset, PointDbl canvasControlOffset)
		{
			var mSetInfoRecord = _mSetRecordMapper.MapTo(mSetInfo);

			var jobRecord = new JobRecord(
				ParentJobId: parentJobId,
				ProjectId: projectId,
				SubDivisionId: subdivisionId,
				Label: label,
				MSetInfo: mSetInfoRecord,
				CanvasSizeInBlocksWidth: canvasSizeInBlocks.Width,
				CanvasSizeInBlocksHeight: canvasSizeInBlocks.Height,
				MapBlockOffsetWidth: canvasBlockOffset.X,
				MapBlockOffsetHeight: canvasBlockOffset.Y,
				CanvasControlOffsetWidth: canvasControlOffset.X,
				CanvasControlOffsetHeight: canvasControlOffset.Y
				);

			return jobRecord;
		}

		public Tuple<long, long> DeleteJobAndChildMapSections(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			var deleteCount = jobReaderWriter.Delete(jobId);
			var result = new Tuple<long, long>(deleteCount ?? 0, 0);
			return result;
		}

		#endregion

		#region Subdivision

		public Subdivision GetOrCreateSubdivision(RPoint position, RSize samplePointDelta, SizeInt blockSize, out bool created)
		{
			SubdivisionRecord subdivisionRecord;

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			samplePointDelta = Reducer.Reduce(samplePointDelta);

			var posDto = _dtoMapper.MapTo(position);
			var samplePointDeltaDto = _dtoMapper.MapTo(samplePointDelta);
			var matches = subdivisionReaderWriter.Get(posDto, samplePointDeltaDto, blockSize);

			if (matches.Count < 1)
			{
				var subdivision = new Subdivision(ObjectId.GenerateNewId(), position, samplePointDelta, blockSize);
				var subId = InsertSubdivision(subdivision, subdivisionReaderWriter);
				subdivisionRecord = subdivisionReaderWriter.Get(subId);
				created = true;
			}
			else if (matches.Count > 1)
			{
				//subdivisionRecord = matches[0];
				//created = false;
				throw new InvalidOperationException($"Found more than one subdivision was found matching: {samplePointDelta}.");
			}
			else
			{
				subdivisionRecord = matches[0];
				created = false;
			}

			var result = _mSetRecordMapper.MapFrom(subdivisionRecord);
			return result;
		}

		private ObjectId InsertSubdivision(Subdivision subdivision, SubdivisonReaderWriter subdivisionReaderWriter)
		{
			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			var result = subdivisionReaderWriter.Insert(subdivisionRecord);

			return result;
		}

		public bool DeleteSubdivision(Subdivision subdivision)
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			var subsDeleted = subdivisionReaderWriter.Delete(subdivision.Id);

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			_ = mapSectionReaderWriter.DeleteAllWithSubId(subdivision.Id);

			return subsDeleted.HasValue && subsDeleted.Value > 0;
		}

		#endregion
	}
}
