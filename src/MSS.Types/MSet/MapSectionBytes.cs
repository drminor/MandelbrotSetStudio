using MongoDB.Bson;
using MSS.Types.MSet;
using System;

namespace MSS.Types.MSet
{
	public class MapSectionBytes
	{
		public MapSectionBytes(
			ObjectId mapSectionId,
			DateTime dateCreatedUtc,
			DateTime lastSavedUtc,
			DateTime lastAccessed,
			ObjectId subdivisionId,
			VectorLong blockPosition,

			MapCalcSettings mapCalcSettings,
			bool requestWasCompleted,
			bool allRowsHaveEscaped,
			byte[] counts,
			byte[] escapeVelocities
			)
		{
			Id = mapSectionId;
			DateCreatedUtc = dateCreatedUtc;
			LastSavedUtc = lastSavedUtc;
			LastAccessed = lastAccessed;
			SubdivisionId = subdivisionId;

			BlockPosition = blockPosition;

			MapCalcSettings = mapCalcSettings;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			RequestWasCompleted = requestWasCompleted;
			Counts = counts;
			EscapeVelocities = escapeVelocities;
		}

		#region Public Properties

		public ObjectId Id { get; init; }

		public DateTime DateCreatedUtc { get; init; }
		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessed { get; set; }

		public ObjectId SubdivisionId { get; init; }
		public VectorLong BlockPosition { get; init; }

		public MapCalcSettings MapCalcSettings { get; init; }

		public bool AllRowsHaveEscaped { get; init; }
		public byte[] Counts { get; init; }
		public byte[] EscapeVelocities { get; init; }

		public SizeInt BlockSize { get; init; }

		public bool RequestWasCompleted { get; init; }

		#endregion
	}
}
