using MongoDB.Bson;
using System;

namespace MSS.Common
{
	public interface IJobInfo
	{
		DateTime DateCreatedUtc { get; set; }
		ObjectId Id { get; set; }
		ObjectId? ParentJobId { get; set; }
		ObjectId SubdivisionId { get; set; }
		int TransformType { get; set; }
		int MapCoordExponent { get; set; }
	}
}