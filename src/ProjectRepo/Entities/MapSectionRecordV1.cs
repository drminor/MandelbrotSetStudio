﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	/// <summary>
	/// Record used to store the data found in a MapSectionResponse
	/// </summary>
	public record MapSectionRecordV1(
		DateTime DateCreatedUtc,
		ObjectId SubdivisionId,
		long BlockPosXHi,
		long BlockPosXLo,
		long BlockPosYHi,
		long BlockPosYLo,
		MapCalcSettings MapCalcSettings,
		int[] Counts,
		bool[] DoneFlags,
		double[] ZValues
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessed { get; set; }
	}

}