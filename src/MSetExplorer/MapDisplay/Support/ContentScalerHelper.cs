using System;

namespace MSetExplorer.MapDisplay.Support
{
	internal class ContentScalerHelper
	{
		private const double BREAK_DOWN_FACTOR = 0.5;

		#region Static Methods

		public static (double baseScale, double relativeScale) GetBaseAndRelative(double contentScale)
		{
			//var t = value / BREAK_DOWN_FACTOR;

			//var b = (double)(int)t;
			//b = b *= BREAK_DOWN_FACTOR;

			//var r = t - b;

			//r = r *= BREAK_DOWN_FACTOR;

			//return (b, r);

			double b;
			double r;

			if (contentScale > 0.5)
			{
				b = 0;
				r = contentScale;
			}
			else if (contentScale == 0.5)
			{
				b = 1;
				r = 1;
			}
			else if (contentScale == 0.4375)
			{
				b = 1;
				r = 0.875;
			}
			else if (contentScale == 0.375)
			{
				b = 1;
				r = 0.75;
			}
			else if (contentScale == 0.3125)
			{
				b = 1;
				r = 0.625;
			}
			else if (contentScale == 0.25)
			{
				b = 2;
				r = 1;
			}
			else if (contentScale == 0.1875)
			{
				b = 2;
				r = 0.75;
			}
			else if (contentScale == 0.125)
			{
				b = 3;
				r = 1;
			}
			else if (contentScale == 0.0625)
			{
				b = 4;
				r = 1;
			}
			else
			{
				throw new InvalidOperationException($"The value: {contentScale} is not a supported value for ContentScale.");
			}

			return (b, r);
		}

		public static double GetCombinedValue(double baseScale, double relativeScale)
		{
			var combinedValue = Math.Pow(BREAK_DOWN_FACTOR, baseScale) * relativeScale;
			return combinedValue;
		}

		public static double GetScaleFactor(double contentScale)
		{
			var (baseScale, _) = GetBaseAndRelative(contentScale);
			var result = Math.Pow(BREAK_DOWN_FACTOR, baseScale);
			return result;
		}

		public static double GetScaleFactorFromBase(double baseScale)
		{
			var result = Math.Pow(BREAK_DOWN_FACTOR, baseScale);
			return result;
		}

		/*			Math used to calculate base and relative scales.
		 			
				combined	base	relative
		1		1			0		1			1 * 1
		15/16	0.9375		0		0.9375		1 * 0.9375
		14/16	0.875		0		0.875		1 * 0.875
		8/16	0.5			1		1			0.5 * 1.0				= 0.5			1/2
		7/16	0.4375		1		0.875		0.5 * 0.875				= 0.5 x ((2 * 7) / 16) 14/16 = 0.875
		6/16	0.375		1		0.75		0.5 * 3/4				= 1.5/4 = 3/8 = 6/16
		5/16	0.3125		1		0.625		0.5 * 5/8
		4/16	0.25		2		1			0.5 * 0.5 * 1		4	= 0.25 * 1 = 0.25			
		3/16	0.1875		2		0.75		0.5 * 0.5 * 12/16		= 0.25 x 12/16 = (0.25 * 12) / 16 = 3/16
		2/16	0.125		3		1			0.5 * 0.5 * 0.5 x 1
		1/16				4		1			0.5 x 0.5 * 0.5 * 0.5	8/16 ^4 = 8^4 / 16^4 = 4096 / 65536 = 1/16 = 0.0625

		*/

		#endregion


	}
}
