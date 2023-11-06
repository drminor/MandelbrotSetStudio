using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public interface IJobInfo
	{
		ObjectId Id { get; set; }
		DateTime DateCreatedUtc { get; set; }

		ObjectId? ParentJobId { get; set; }
		bool IsCurrentOnOwner { get; set; }
		ObjectId SubdivisionId { get; set; }
		int TransformType { get; set; }
		int MapCoordExponent { get; set; }

		int NumberOfMapSections { get; set; }
		int NumberOfCritical { get; set; }
		int NumberOfNonCritical { get; set; }

		//public int NumberOfFullScale { get; set; }
		//public int NumberOfReducedScale { get; set; }
		//public int NumberOfImage { get; set; }
		//public int NumberOfSizeEditorPreview { get; set; }

		double PercentageMapSectionsShared { get; set; }
		double PercentageMapSectionsSharedWithSameOwner { get; set; }

	}
}