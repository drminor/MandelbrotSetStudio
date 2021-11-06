using System;

namespace MSS.Types
{
	[Serializable]
	public record ApCoords(string StartingX, string EndingX, string StartingY, string EndingY, double[] Bin)
	{
		private readonly int _vd = Bin.Length / 4;

		public ApCoords() : this(0, 0, 0, 0)
		{ }

		public ApCoords(double Sx, double Ex, double Sy, double Ey) : this(Sx.ToString(), Ex.ToString(), Sy.ToString(), Ey.ToString(), new double[] { Sx, Ex, Sy, Ey })
		{ }

		public double[] Bin { get; init; } = Bin;
		public int ValueDepth => _vd;

	}

	public class ApCoordHelper
	{
		public ApCoords BuildCoord(double[] bin)
		{
			string[] stringVals = GetStringVals(bin);

			ApCoords result = new ApCoords(stringVals[0], stringVals[1], stringVals[2], stringVals[3], bin);
			return result;
		}

		private string[] GetStringVals(double[] bin)
		{
			string[] result;
			if (bin.Length == 4)
			{
				result = new string[4];
				result[0] = bin[0].ToString();
				result[1] = bin[1].ToString();
				result[2] = bin[2].ToString();
				result[3] = bin[3].ToString();
			}
			else
			{
				throw new ArgumentException("At this time, only a bin argument with 4 elements is supported.");
			}

			return result;
		}
	}
}
