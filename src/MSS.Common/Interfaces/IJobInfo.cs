using MongoDB.Bson;
using System;

namespace MSS.Common
{
	public interface IJobInfo
	{
		ObjectId Id { get; set; }
		ObjectId? ParentJobId { get; set; }
		ObjectId SubdivisionId { get; set; }
		int TransformType { get; set; }
		int MapCoordExponent { get; set; }
		DateTime DateCreatedUtc { get; set; }

		int Stat1 { get; set; }
		int Stat2 { get; set; }
		int Stat3 { get; set; }

	}
}