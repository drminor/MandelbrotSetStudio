using MSS.Types.Base;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public class DRectangle : Rectangle<double>
	{
		public DRectangle(double[] values) : base(values)
		{ }

		public DRectangle(long[] values) : base(values.Select(x => ConvertLongToDouble(x)).ToArray())
		{ }

		public DRectangle(BigInteger[] values) : base(values.Select(x => ConvertBigIntegerToDouble(x)).ToArray())
		{ }

		public DRectangle(double x1, double x2, double y1, double y2) : base(x1, x2, y1, y2)
		{ }

		public double Width => X2 - X1;

		public double Height => Y2 - Y1;

		public DSize Size => new DSize(Width, Height);

		// TODO: Catch overflow exceptions
		public DRectangle Scale(double factor)
		{
			return new DRectangle(Values.Select(x => MultiplyWithCheck(x, factor)).ToArray());
		}

		private double MultiplyWithCheck(double val, double factor)
		{
			checked {
				try
				{
					double result = Math.FusedMultiplyAdd(val, factor, 0);

					if (!DoubleHelper.HasPrecision(result))
					{
						throw new OverflowException($"{val} x {factor} is too small.");
					}

					return result;
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Got exception: {e.GetType()}:{e.Message}");
					return 0;
				}
			}
		}

		private static double ConvertLongToDouble(long val)
		{
			try
			{
				double result = Convert.ToDouble(val);

				if (!DoubleHelper.HasPrecision(result))
				{
					throw new OverflowException($"When converting long: {val} to a double, precision was lost.");
				}

				return result;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e.GetType()}:{e.Message}");
				return 0;
			}
		}

		private static double ConvertBigIntegerToDouble(BigInteger val)
		{
			try
			{
				if(!SafeCastToDouble(val))
				{
					throw new OverflowException($"It is not safe to cast BigInteger: {val} to a double.");
				}

				double result = (double)val;

				if (!DoubleHelper.HasPrecision(result))
				{
					throw new OverflowException($"When converting BigInteger: {val} to a double, precision was lost.");
				}

				return result;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e.GetType()}:{e.Message}");
				return 0;
			}
		}

		private static bool SafeCastToDouble(BigInteger value)
		{
			BigInteger s_bnDoubleMinValue = (BigInteger)double.MinValue;
			BigInteger s_bnDoubleMaxValue = (BigInteger)double.MaxValue;
			return s_bnDoubleMinValue <= value && value <= s_bnDoubleMaxValue;
		}

	}
}
