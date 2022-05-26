using MSS.Types;
using System;
using System.Text;


namespace MSS.Common
{
	public class MapSectionSpIdxItem
	{
		public int Level { get; init; }

		public RValue[] XValues;
		public RValue[] YValues;

		public MapSectionSpIdxItem(RPoint anchor)
		{
			Level = (anchor.Exponent + 1) / 3;

			XValues = new RValue[8];
			YValues = new RValue[8];

			var exp = anchor.Exponent;
			var sX = anchor.X.Value;
			var sY = anchor.Y.Value;

			for (var i = 0; i < 8; i++)
			{
				XValues[i] = new RValue(sX + i, exp);
				YValues[i] = new RValue(sY + i, exp);
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			for(var yPtr = 0; yPtr < 8; yPtr++)
			{
				var den = $"/{Math.Pow(2, XValues[0].Exponent * -1)}";
				for (var xPtr = 0; xPtr < 8; xPtr++)
				{
					//sb.Append(RValueHelper.ConvertToString(XValues[xPtr]));
					sb.Append("[").Append(XValues[xPtr].Value.ToString()).Append(den).Append("]");

					sb.Append(", ");
					//sb.Append(RValueHelper.ConvertToString(YValues[yPtr]));
					sb.Append("[").Append(YValues[yPtr].Value.ToString()).Append(den).Append("]");
					sb.Append("; ");
				}
				sb.AppendLine();
			}

			return sb.ToString();
		}


		//public int GetExponent(int level)
		//{

		//	// Level 0 = -2.5 to 1.5 / 8 = 1/2 = -1

		//	// Level 1 = 1/2 div 8 = 1/16 = -4

		//	// Level 2 = 1/16 div 8 = 1/128 = -7

		//	// Level 3 = 1/128 div 8 = 1/1024 = -10

		//	//-13	8,192

		//	//-16	65,536

		//	//-19	524,288

		//	//-22	4,194,304

		//	return 1;
		//}


	}
}
