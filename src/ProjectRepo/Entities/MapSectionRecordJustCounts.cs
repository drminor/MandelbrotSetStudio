using MongoDB.Bson;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	/// <summary>
	/// Record used to store the data found in a MapSectionResponse
	/// </summary>
	public record MapSectionRecordJustCounts(
		ObjectId Id,
		DateTime DateCreatedUtc,
		ObjectId SubdivisionId,
		long BlockPosXHi,
		long BlockPosXLo,
		long BlockPosYHi,
		long BlockPosYLo,

		MapCalcSettings MapCalcSettings,
		bool AllPointsHaveEscaped,
		byte[] Counts

		)
	{
		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessed { get; set; }
	}


}
