﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		bool IsAlternatePathHead,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		int TransformType,

		MapAreaInfoRecord MapAreaInfoRecord,
		string TransformTypeString,

		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		ObjectId ColorBandSetId,
		MapCalcSettings MapCalcSettings,
		SizeIntRecord CanvasSizeInBlocks
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		// TODO: Rename LastSaved to LastSavedUtc
		public DateTime LastSaved { get; set; }
		public DateTime LastAccessedUtc { get; set; }

		public IterationUpdateRecord[]? IterationUpdates { get; set; }
		public ColorMapUpdateRecord[]? ColorMapUpdates { get; set; }
	}

}
