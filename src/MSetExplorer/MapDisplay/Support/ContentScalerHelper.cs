using System;

namespace MSetExplorer
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

			// Between 1/2 and 1
			if (contentScale > 0.5)
			{
				b = 0;
				r = contentScale;
			}

			// Between 1/4 and 1/2
			else if (contentScale > 0.25)
			{
				b = 1;
				r = contentScale / 0.5;
			}

			// Between 1/4 and 1/8
			else if (contentScale > 0.125)
			{
				b = 2;
				r = contentScale / 0.25;
			}

			// Between 1/8 and 1/16
			else if (contentScale > 0.0625)
			{
				b = 3;
				r = contentScale / 0.125;
			}

			// Between 1/16 and 1/32
			else if (contentScale > 0.03125)
			{
				b = 4;
				r = contentScale / 0.0625;
			}

			// Between 1/32 and 1/64
			else if (contentScale > 0.015625)
			{
				b = 5;
				r = contentScale / 0.03125;
			}

			// Between 1/64 and 1/128
			else if (contentScale > 0.0078125)
			{
				b = 6;
				r = contentScale / 0.015625;
			}

			// Between 1/128 and 1/256
			else if (contentScale > 0.00390625)
			{
				b = 7;
				r = contentScale / 0.0078125;
			}

			else
			{
				throw new InvalidOperationException($"Values for the ContentScale < 1/256 and less are not supported.");
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
		1/16	0.0625		4		1			0.5 x 0.5 * 0.5 * 0.5	8/16 ^4 = 8^4 / 16^4 = 4096 / 65536 = 1/16 = 0.0625



			Scale			Exp   Base Scale	Relative Scale
	---------------------------------------------------------------		
		>	0.5			|	0	|	1	|	x / 1
		>	0.25		|	1	|	2	|	x / 0.5
		>	0.125		|	2	|	4	|	x / 0.25
		>	0.0625		|	3	|   8	|	x / 0.125
		>	0.03125		|	4	|  16	|	x / 0.0625
		>	0.015625	|	5	|  32	|	x / 0.03125
		>	0.0078125	|	6	|  64	|	x / 0.015625
		>   0.00390625  |   7	| 128	|	x / 0.0078125 	

		else throw
		 
		 
		 */

		#endregion


	}
}
