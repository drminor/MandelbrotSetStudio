using MongoDB.Bson;
using System;

namespace MSS.Common.MSet
{
	public class JobInfo : IJobInfo
	{
		public JobInfo(ObjectId id, ObjectId? parentJobId, DateTime dateCreatedUtc, int transformType, ObjectId subDivisionId, int mapCoordExponent)
		{
			Id = id;
			ParentJobId = parentJobId;
			DateCreatedUtc = dateCreatedUtc;
			TransformType = transformType;
			SubDivisionId = subDivisionId;
			MapCoordExponent = mapCoordExponent;
		}

		public ObjectId Id { get; set; }

		public ObjectId? ParentJobId { get; set; }

		public DateTime DateCreatedUtc { get; set; }
		public int TransformType { get; set; }

		public ObjectId SubDivisionId { get; set; }
		public int MapCoordExponent { get; set; }

	}

}
