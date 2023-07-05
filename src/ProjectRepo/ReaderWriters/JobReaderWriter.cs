using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProjectRepo
{
	public class JobReaderWriter : MongoDbCollectionBase<JobRecord>
	{
		private const string COLLECTION_NAME = "Jobs";

		public JobReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		#region Get / Insert / Delete

		public JobRecord? Get(ObjectId jobId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);
			var jobRecord = Collection.Find(filter).FirstOrDefault();

			return jobRecord;
		}

		public IEnumerable<ObjectId> GetJobIdsByOwner(ObjectId ownerId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => p.Id
				);

			var filter = Builders<JobRecord>.Filter.Eq("OwnerId", ownerId);
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsForOwner(ObjectId ownerId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => new ValueTuple<ObjectId, ObjectId>(p.Id, p.SubDivisionId)
				);

			var filter = Builders<JobRecord>.Filter.Eq("OwnerId", ownerId);
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsForAllJobs()
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => new ValueTuple<ObjectId, ObjectId>(p.Id, p.SubDivisionId)
				);

			var filter = Builders<JobRecord>.Filter.Empty;
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}

		public IEnumerable<ObjectId> GetSubdivisionIdsForAllJobs()
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => p.SubDivisionId
				);

			var filter = Builders<JobRecord>.Filter.Empty;
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}


		public IEnumerable<ValueTuple<ObjectId, ObjectId, OwnerType>> GetJobAndOwnerIdsWithJobOwnerType()
		{
			var projection1 = Builders<JobRecord>.Projection.Expression(p => new ValueTuple<ObjectId, ObjectId, OwnerType>(p.Id, p.OwnerId, p.JobOwnerType));

			var filter = Builders<JobRecord>.Filter.Empty;

			IFindFluent<JobRecord, ValueTuple<ObjectId, ObjectId, OwnerType>> operation = Collection.Find(filter).Project(projection1);

			var itemsFound = operation.ToEnumerable();
			return itemsFound;
		}

		public (ObjectId, MapAreaInfo2Record)? GetSubdivisionIdAndMapAreaInfo(ObjectId jobId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression(p => new ValueTuple<ObjectId, MapAreaInfo2Record>(p.SubDivisionId, p.MapAreaInfo2Record));
			var filter = Builders<JobRecord>.Filter.Eq(f => f.Id, jobId);
			
			//var (subdivisionId, mapAreaInfo) = Collection.Find(filter).Project(projection1).FirstOrDefault();
			//return (subdivisionId, mapAreaInfo);

			var x = Collection.Find(filter).Project(projection1).FirstOrDefault();
			if (x.Item1 == ObjectId.Empty)
			{
				return null;
			}
			else
			{
				var (subdivisionId, mapAreaInfo) = x;
				return (subdivisionId, mapAreaInfo);
			}
		}

		public ObjectId? GetSubdivisionId(ObjectId jobId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression(p => p.MapAreaInfo2Record.SubdivisionRecord.Id);
			var filter = Builders<JobRecord>.Filter.Eq(f => f.Id, jobId);

			var result = Collection.Find(filter).Project(projection1).FirstOrDefault();

			return result;
		}

		public ObjectId Insert(JobRecord jobRecord)
		{
			var idValueBeoreInsert = jobRecord.Id;

			Collection.InsertOne(jobRecord);

			if (jobRecord.Id != idValueBeoreInsert)
			{
				Debug.WriteLine("WARNING: The jobrecord's Id was udated upon insert.");
			}



			return jobRecord.Id;
		}

		public void UpdateJobDetails(JobRecord jobRecord)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobRecord.Id);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.ParentJobId, jobRecord.ParentJobId)
				.Set(u => u.OwnerId, jobRecord.OwnerId)
				.Set(u => u.JobOwnerType, jobRecord.JobOwnerType)

				.Set(u => u.MapAreaInfo2Record, jobRecord.MapAreaInfo2Record)
				.Set(u => u.ColorBandSetId, jobRecord.ColorBandSetId)
				.Set(u => u.MapCalcSettings, jobRecord.MapCalcSettings)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow)
				.Set(u => u.LastAccessedUtc, jobRecord.LastAccessedUtc);


			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateColorBandSet(ObjectId jobId, int targetIterations, ObjectId colorBandSetId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.MapCalcSettings.TargetIterations, targetIterations)
				.Set(u => u.ColorBandSetId, colorBandSetId)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateJobOwnerType(ObjectId jobId, OwnerType jobOwnerType)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.JobOwnerType, jobOwnerType)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}


		public long? Delete(ObjectId jobId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteAllForProject(ObjectId projectId)
		{
			var ids = GetJobIdsByOwner(projectId);

			// Create an $in filter for those ids
			var idsFilter = Builders<JobRecord>.Filter.In(d => d.Id, ids);

			// Delete the documents using the $in filter
			var deleteResult = Collection.DeleteMany(idsFilter);
			return GetReturnCount(deleteResult);
		}

		#endregion

		#region Aggregate Results

		public IEnumerable<ObjectId> GetAllReferencedColorBandSetIds()
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => p.ColorBandSetId
				);

			//List models = collection.Find(_ => true).Project(projection1).ToList();

			var filter = Builders<JobRecord>.Filter.Empty;
			var colorBandSetIds = Collection.Find(filter).Project(projection1).ToEnumerable().Distinct();

			return colorBandSetIds;
		}

		public IEnumerable<JobSudivisionInfo> GetJobSubdivisionInfosForOwner(ObjectId ownerId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => new JobSudivisionInfo(p.Id, p.DateCreatedUtc, p.SubDivisionId, p.MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Exponent)
				);

			//List models = collection.Find(_ => true).Project(projection1).ToList();

			var filter = Builders<JobRecord>.Filter.Eq("OwnerId", ownerId);
			var jobInfos = Collection.Find(filter).Project(projection1).ToEnumerable();

			return jobInfos;
		}

		public IEnumerable<JobInfo> GetJobInfosForOwner(ObjectId ownerId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					//p => new JobSudivisionInfo(p.Id, p.DateCreatedUtc, p.SubDivisionId, p.MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Exponent)
					p => new JobInfo(p.Id, p.ParentJobId, p.Id.CreationTime, p.TransformType, p.MapAreaInfo2Record.SubdivisionRecord.Id, p.MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Exponent)
				);

			var filter = Builders<JobRecord>.Filter.Eq("OwnerId", ownerId);
			var jobInfos = Collection.Find(filter).Project(projection1).ToEnumerable();

			return jobInfos;
		}

		#endregion

		public class JobSudivisionInfo
		{
			public JobSudivisionInfo(ObjectId id, DateTime dateCreatedUtc, ObjectId subdivisionId, int mapCoordExponent)
			{
				Id = id;
				DateCreatedUtc = dateCreatedUtc;
				SubdivisionId = subdivisionId;
				MapCoordExponent = mapCoordExponent;
			}

			public ObjectId Id { get; init; }
			public DateTime DateCreatedUtc { get; init; }

			public ObjectId SubdivisionId { get; init; }
			public int MapCoordExponent { get; init; }
		}

		#region SCHEMA Changes

		/*
		private DtoMapper _dtoMapper = new DtoMapper();

		public long UpdateJobsToUseMapAreaInfo2()
		{

			var updateDefinition = Builders<JobRecord>.Update
				.Unset(f => f.MapAreaInfoRecord)
				.Unset(f => f.ProjectId)
				.Unset(f => f.IsAlternatePathHead)
				.Unset(f => f.CanvasSizeInBlocks);

			var options = new UpdateOptions { IsUpsert = false };

			var result = 0L;

			var filter = Builders<JobRecord>.Filter.Empty;

			var jobs = Collection.Find(filter).ToList();

			foreach (var jobRecord in jobs)
			{
				var mapAreaInfoV1 = MapFrom(jobRecord.MapAreaInfoRecord);

				var mapAreaInfoV2 = MapJobHelper.Convert(mapAreaInfoV1);

				var mapAreaInfoV2Record = MapTo(mapAreaInfoV2);

				var updateDef2 = updateDefinition
					.Set(u => u.MapAreaInfo2Record, mapAreaInfoV2Record)
					.Set(u => u.OwnerId, jobRecord.ProjectId)
					.Set(u => u.JobOwnerType, JobOwnerType.Project)
					.Set(u => u.LastSavedUtc, jobRecord.LastSaved);

				filter = Builders<JobRecord>.Filter.Eq("_id", jobRecord.Id);
				var updateResult2 = Collection.UpdateOne(filter, updateDef2, options);
				var cnt = GetReturnCount(updateResult2) ?? -1;
				result += cnt;
			}

			//var result = 0L;

			return result;
		}

		public MapAreaInfo2Record MapTo(MapAreaInfo2 source)
		{
			var result = new MapAreaInfo2Record(
				RPointAndDeltaRecord: MapTo(source.PositionAndDelta),
				SubdivisionRecord: MapTo(source.Subdivision),
				MapBlockOffset: MapTo(source.MapBlockOffset),
				CanvasControlOffset: MapTo(source.CanvasControlOffset),
				Precsion: source.Precision
				);

			return result;
		}

		public RPointAndDeltaRecord MapTo(RPointAndDelta source)
		{
			var rPointAndDeltaDto = _dtoMapper.MapTo(source);
			var display = source.ToString();
			var result = new RPointAndDeltaRecord(display, rPointAndDeltaDto);

			return result;
		}

		public SubdivisionRecord MapTo(Subdivision source)
		{
			var baseMapPosition = MapTo(source.BaseMapPosition);
			var samplePointDelta = MapTo(source.SamplePointDelta);

			var result = new SubdivisionRecord(samplePointDelta, MapTo(source.BlockSize))
			{
				Id = source.Id,
				BaseMapPosition = MapTo(source.BaseMapPosition)
			};

			return result;
		}

		public RSizeRecord MapTo(RSize rSize)
		{
			var rSizeDto = _dtoMapper.MapTo(rSize);
			var display = rSize.ToString();
			var result = new RSizeRecord(display, rSizeDto);

			return result;
		}

		public SizeIntRecord MapTo(SizeInt source)
		{
			return new SizeIntRecord(source.Width, source.Height);
		}

		public VectorIntRecord MapTo(VectorInt source)
		{
			return new VectorIntRecord(source.X, source.Y);
		}

		public MapAreaInfo MapFrom(MapAreaInfoRecord target)
		{
			var result = new MapAreaInfo(
				coords: _dtoMapper.MapFrom(target.CoordsRecord.CoordsDto),
				canvasSize: MapFrom(target.CanvasSize),
				subdivision: MapFrom(target.SubdivisionRecord),
				precision: target.Precision ?? RMapConstants.DEFAULT_PRECISION,
				mapBlockOffset: MapFrom(target.MapBlockOffset),
				canvasControlOffset: MapFrom(target.CanvasControlOffset)
				);

			return result;
		}

		public SizeInt MapFrom(SizeIntRecord target)
		{
			return new SizeInt(target.Width, target.Height);
		}

		public Subdivision MapFrom(SubdivisionRecord target)
		{
			var samplePointDelta = _dtoMapper.MapFrom(target.SamplePointDelta.Size);

			BigVector baseMapPosition;

			if (target.BaseMapPosition != null)
			{
				baseMapPosition = _dtoMapper.MapFrom(target.BaseMapPosition.BigVector);
			}
			else
			{
				baseMapPosition = new BigVector();
			}

			var result = new Subdivision(target.Id, samplePointDelta, baseMapPosition, MapFrom(target.BlockSize));

			return result;
		}

		public BigVector MapFrom(BigVectorRecord target)
		{
			var result = _dtoMapper.MapFrom(target.BigVector);

			return result;
		}

		public BigVectorRecord MapTo(BigVector bigVector)
		{
			var bigVectorDto = _dtoMapper.MapTo(bigVector);
			var display = bigVector.ToString() ?? "0";
			var result = new BigVectorRecord(display, bigVectorDto);

			return result;
		}

		public VectorInt MapFrom(VectorIntRecord target)
		{
			return new VectorInt(target.X, target.Y);
		}

*/


		//public void RemoveEscapeVelsFromAllJobs()
		//{
		//	var filter = Builders<JobRecord>.Filter.Empty;
		//	var updateDefinition = Builders<JobRecord>.Update.Unset(f => f.MapCalcSettings.UseEscapeVelocities);
		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

		//public void AddIsIsAlternatePathHeadToAllJobs()
		//{
		//	var filter = Builders<JobRecord>.Filter.Empty;
		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Set("IsAlternatePathHead", false);
		//		//.Unset("IsPreferredChild");

		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

		//public DateTime GetLastSaveTime(ObjectId projectId)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
		//	var jobs = Collection.Find(filter).ToList();

		//	if (jobs.Count < 1)
		//	{
		//		return DateTime.MinValue;
		//	}
		//	else
		//	{
		//		var result = jobs.Max(x => x.Id.CreationTime);
		//		return result;
		//	}
		//}

		//public void AddColorBandSetIdByProject(ObjectId projectId, ObjectId colorBandSetId)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
		//	var updateDefinition = Builders<JobRecord>.Update.Set("ColorBandSetId", colorBandSetId);
		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

		//public void AddIsPreferredChildToAllJobs()
		//{
		//	var filter = Builders<JobRecord>.Filter.Empty;
		//	var updateDefinition = Builders<JobRecord>.Update.Set("IsPreferredChild", true);
		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

		//public long ConvertToMapAreaRecord(ObjectId jobId, MapAreaInfoRecord mapAreaInfoRecord, string transformType)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Set("MapAreaInfoRecord", mapAreaInfoRecord)
		//		.Set("TransformTypeString", transformType)
		//		.Unset("MSetInfoRecord.MapCalcSetting.FetchZValues")
		//		.Unset("MSetInfoRecord.MapCalcSettings.DontFetchZValuesFromRepo")
		//		.Unset("MSetInfoRecord.MapCalcSettings.DontFetchZValues");

		//	var options = new UpdateOptions { IsUpsert = false };

		//	var updateResult = Collection.UpdateOne(filter, updateDefinition, options);

		//	return GetReturnCount(updateResult) ?? -1;
		//}

		//public long AddMapCalcSettingsField(ObjectId jobId, MapCalcSettings mapCalcSettings)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Set("MapCalcSettings", mapCalcSettings);

		//	var options = new UpdateOptions { IsUpsert = false };

		//	var updateResult1 = Collection.UpdateOne(filter, updateDefinition, options);

		//	updateDefinition = Builders<JobRecord>.Update
		//		.Unset("MSetInfoRecord.MapCalcSetting.FetchZValues")
		//		.Unset("MSetInfoRecord.MapCalcSettings.DontFetchZValuesFromRepo")
		//		.Unset("MSetInfoRecord.MapCalcSettings.DontFetchZValues");

		//	var updateResult2 = Collection.UpdateOne(filter, updateDefinition, options);
		//	var cnt1 = GetReturnCount(updateResult1) ?? -1;
		//	var cnt2 = GetReturnCount(updateResult2) ?? -1;

		//	return cnt1 + cnt2;
		//}

		//public long RemoveJobsWithNoProject()
		//{
		//	var result = 0L;
		//	var filter = Builders<JobRecord>.Filter.Where(x => x.MapAreaInfoRecord == null);

		//	var jobs = Collection.Find(filter).ToEnumerable();

		//	foreach(var j in jobs)
		//	{
		//		Delete(j.Id);
		//		result++;
		//	}

		//	return result;
		//}

		//public long RemoveFetchZValuesProperty()
		//{
		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Unset("MapCalcSettings.FetchZValues")
		//		.Unset("MapCalcSettings.DontFetchZValuesFromRepo")
		//		.Unset("MapCalcSettings.DontFetchZValues")
		//		.Unset("MSetInfo.MapCalcSettings.FetchZValues")
		//		.Unset("MSetInfo.MapCalcSettings.DontFetchZValuesFromRepo")
		//		.Unset("MSetInfo.MapCalcSettings.DontFetchZValues");


		//	var options = new UpdateOptions { IsUpsert = false };

		//	var result = 0L;
		//	//var filter = Builders<JobRecord>.Filter.Where(x => x.MapCalcSettings.DontFetchZValues.HasValue);

		//	var filter = Builders<JobRecord>.Filter.Empty;

		//	var jobs = Collection.Find(filter).ToList();

		//	foreach (var j in jobs)
		//	{
		//		filter = Builders<JobRecord>.Filter.Eq("_id", j.Id);
		//		var updateResult2 = Collection.UpdateOne(filter, updateDefinition, options);
		//		var cnt = GetReturnCount(updateResult2) ?? -1;
		//		result += cnt;
		//	}

		//	//var result = 0L;

		//	return result;
		//}

		//public long RemoveFetchZValuesProperty2()
		//{
		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Unset("MSetInfoRecord.MapCalcSetting.FetchZValues")
		//		.Unset("MSetInfoRecord.MapCalcSettings.DontFetchZValuesFromRepo")
		//		.Unset("MSetInfoRecord.MapCalcSettings.DontFetchZValues");

		//	var options = new UpdateOptions { IsUpsert = false };

		//	var result = 0L;
		//	var filter = Builders<JobRecord>.Filter.Where(x => x.MSetInfo.MapCalcSettings.DontFetchZValues.HasValue);

		//	var jobs = Collection.Find(filter).ToEnumerable();

		//	foreach (var j in jobs)
		//	{
		//		filter = Builders<JobRecord>.Filter.Eq("_id", j.Id);
		//		var updateResult2 = Collection.UpdateOne(filter, updateDefinition, options);
		//		var cnt = GetReturnCount(updateResult2) ?? -1;
		//		result += cnt;
		//	}

		//	return result;
		//}

		//public long RemoveOldMapAreaProperties()
		//{
		//	//var updateDefinition = Builders<JobRecord>.Update
		//	//	.Unset("MSetInfo")
		//	//	.Unset("MapBlockOffset")
		//	//	.Unset("CanvasControlOffset")
		//	//	.Unset("CanvasSize");

		//	//var options = new UpdateOptions { IsUpsert = false };

		//	var result = 0L;

		//	//var filter = Builders<JobRecord>.Filter.Empty;
		//	//var jobs = Collection.Find(filter).ToList();

		//	//foreach (var j in jobs)
		//	//{
		//	//	filter = Builders<JobRecord>.Filter.Eq("_id", j.Id);
		//	//	var updateResult2 = Collection.UpdateOne(filter, updateDefinition, options);
		//	//	var cnt = GetReturnCount(updateResult2) ?? -1;
		//	//	result += cnt;
		//	//}


		//	return result;
		//}

		#endregion
	}
}
