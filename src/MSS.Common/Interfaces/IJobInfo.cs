using MongoDB.Bson;
using System;

namespace MSS.Common
{
	public interface IJobInfo
	{
		DateTime DateCreatedUtc { get; set; }
		ObjectId Id { get; set; }
		int MapCoordExponent { get; set; }
		ObjectId? ParentJobId { get; set; }
		ObjectId SubDivisionId { get; set; }
		int TransformType { get; set; }
	}
}