﻿using MongoDB.Bson;
using MSS.Types.MSet;
using System;

namespace MSS.Types
{
	public class MapSectionBytes
	{
		public MapSectionBytes(
			DateTime dateCreatedUtc,
			DateTime lastSavedUtc,
			DateTime lastAccessed,
			ObjectId subdivisionId,
			BigVector blockPosition,

			MapCalcSettings mapCalcSettings,
			bool allRowsHaveEscaped,
			bool isComplete,
			byte[] counts,
			byte[] escapeVelocities
			)
		{
			DateCreatedUtc = dateCreatedUtc;
			LastSavedUtc = lastSavedUtc;
			LastAccessed = lastAccessed;
			SubdivisionId = subdivisionId;

			BlockPosition = blockPosition;

			MapCalcSettings = mapCalcSettings;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			Complete = isComplete;
			Counts = counts;
			EscapeVelocities = escapeVelocities;
		}

		#region Public Properties

		public ObjectId Id { get; init; }

		public DateTime DateCreatedUtc { get; init; }
		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessed { get; set; }

		public ObjectId SubdivisionId { get; init; }
		public BigVector BlockPosition { get; init; }

		public MapCalcSettings MapCalcSettings { get; init; }

		public bool AllRowsHaveEscaped { get; init; }
		public byte[] Counts { get; init; }
		public byte[] EscapeVelocities { get; init; }

		public SizeInt BlockSize { get; init; }

		public bool Complete { get; init; }

		#endregion
	}
}
