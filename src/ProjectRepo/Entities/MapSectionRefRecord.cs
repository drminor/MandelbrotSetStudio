using MSS.Types;
using MongoDB.Bson;
using System;
using System.Numerics;

namespace ProjectRepo.Entities
{
	public record MapSectionRefRecord(
		ObjectId JobId,
		ObjectId MapSectionId,
		PointInt BlockIndex,

		string StartingX, // TODO: Replace with a structure similar to CoordPoints
		string StartingY,
		SizeInt Size,
		double SamplePointDeltaV,
		double SamplePointDeltaH,

		int TargetIterationCount,
		int Threshold,


		RectangleInt ClippingRectangle
		) : RecordBase();


	/// <summary>
	/// Points to a MapSection and contains info regarding
	/// placement 
	/// A job uses these objects
	/// to track the progress of building the screen output
	/// 
	/// </summary>
	public class MapSectionPtr
	{
		public PointInt BlockIndex = new PointInt();

		public BigInteger X1;
		public BigInteger Y1;
		public int Exponent;

		public MapSectionRecord[] MapSections = new MapSectionRecord[0];

		

	}

}
