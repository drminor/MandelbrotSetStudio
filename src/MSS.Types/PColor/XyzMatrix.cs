using System;

namespace MSS.Types.PColor
{
	public class XyzMatrix
	{
		public float[,] Value { get; init; }


		public XyzMatrix()
		{
			Value = new float[3, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
		}

		public XyzMatrix(float[] initValues)
		{
			if (initValues.Length != 9)
			{
				throw new ArgumentException("initValues must contain exactly 9 elements.", nameof(initValues));
			}

			Value = new float[3, 3];

			Value[0, 0] = initValues[0];
			Value[0, 1] = initValues[1];
			Value[0, 2] = initValues[2];
			Value[1, 0] = initValues[3];
			Value[1, 1] = initValues[4];
			Value[1, 2] = initValues[5];
			Value[2, 0] = initValues[6];
			Value[2, 1] = initValues[7];
			Value[2, 2] = initValues[8];
		}


		public XyzMatrix(float[] r0, float[] r1, float[] r2)
		{

			if (r0.Length != 3)
			{
				throw new ArgumentException("The r0 array must contain exactly 3 elements.", nameof(r0));
			}

			if (r1.Length != 3)
			{
				throw new ArgumentException("The r0 array must contain exactly 3 elements.", nameof(r1));
			}

			if (r2.Length != 3)
			{
				throw new ArgumentException("The r0 array must contain exactly 3 elements.", nameof(r2));
			}

			Value = new float[3, 3];

			Value[0, 0] = r0[0];
			Value[0, 1] = r0[1];
			Value[0, 2] = r0[2];
			Value[1, 0] = r1[0];
			Value[1, 1] = r1[1];
			Value[1, 2] = r1[2];
			Value[2, 0] = r2[0];
			Value[2, 1] = r2[1];
			Value[2, 2] = r2[2];
		}


	}
}
