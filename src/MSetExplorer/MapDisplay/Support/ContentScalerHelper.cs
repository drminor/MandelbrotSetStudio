using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class ContentScalerHelper
	{
		private const double BREAK_DOWN_FACTOR = 0.5;

		#region Static Methods

		public static (double baseFactor, double relativeScale) GetBaseFactorAndRelativeScale(double contentScale)
		{
			var l2Cs = Math.Log2(contentScale);
			var baseFactor = Math.Round(l2Cs, MidpointRounding.ToZero) * -1;
			var relativeScale = contentScale * Math.Pow(2, baseFactor);

			CheckNewGetBaseFactorAndRelativeScale(contentScale, baseFactor, relativeScale);

			return (baseFactor, relativeScale);
		}

		public static double GetCombinedValue(double baseFactor, double relativeScale)
		{
			var combinedValue = Math.Pow(BREAK_DOWN_FACTOR, baseFactor) * relativeScale;
			return combinedValue;
		}

		public static double GetBaseScale(double contentScale)
		{
			var (baseFactor, _) = GetBaseFactorAndRelativeScale(contentScale);
			var result = Math.Pow(BREAK_DOWN_FACTOR, baseFactor);
			return result;
		}

		public static double GetBaseScaleFromBaseFactor(double baseFactor)
		{
			var result = Math.Pow(BREAK_DOWN_FACTOR, baseFactor);
			return result;
		}

		// Convert screen coordinates to content coordinates
		public static RectangleDbl GetContentFromScreen(RectangleDbl displayArea, double contentScale)
		{
			// This method has not been tested -- some notes:

			// NOTE: ContentScale = BaseScale * RelativeScale
			// RelativeScale = ContentScale / BaseScale

			// X = 2500. ContentScale = 0.3. BaseScale = 0.5; relativeScale = 0.6 (0.6 x 0.5 = 0.3)
			// X' = 750 
			// x' / baseScale = x' / 0.5 = 750 x 2.
			// (x' / baseScale) / relativeScale = 1500 / 0.6 = 2500
			// x' / relativeScale = 1250.
			// (x' / relativeScale) / baseScale = 2500

			// When applying a clip to a canvas it needs to be done using the native size of the canvas -- any scaling is done after applying the clip.

			// The size provided to this method is size of the content before any scaling takes place.
			// Because our implementation reduces the actual content by BaseScale before it is displayed,
			// we need to adjust this regular value by BaseScale to get the actual 'raw' canvas size

			// Given a value X' that is the result of scaling X by contentScale
			// Find a value X'' that when scaled by RelativeScale produces X'
			// Basically we want to 'undo' the portion of the scaling already performed that is the RelativeScale

			// X' = X'' * RelativeScale, solve for X''

			// X'' = X' / RelativeScale


			// Dividing both sides by X'
			// 1 = X'' * RelativeScale
			// X'' = 1 / RelativeScale

			//var scaledArea = newValue.Scale(1 / relativeScale);

			//var baseScale = GetBaseScale(contentScale);

			var (baseFactor, relativeScale) = GetBaseFactorAndRelativeScale(contentScale);
			var baseScale = GetBaseScaleFromBaseFactor(baseFactor);
			var screenToRelativeScaleFactor = baseScale / contentScale;

			// The screenToRelativeScaleFactor (how to get from screen coordinates to content coordinates
			// should equal the reciprocal of the relativeScaleFactor (how to get from content coordinates to screen coordinates)
			//
			//		(1 / relativeScale) == BaseScale / ContentScale, because...
			//				relativeScale == ContentScale / BaseScale

			CheckScreenToRelativeScaleFactor(screenToRelativeScaleFactor, contentScale);

			// The displayArea's position (aka offset) is in device pixels.
			// The displayArea's size (aka extent) is also in device pixels

			// If the amount of screen realstate required to display the entire content is < the physical ViewportSize,
			// then displayArea is in physical coordinates.
			//var scaledDisplayArea = displayArea.Scale(screenToRelativeScaleFactor);
			var scaledDisplayArea = displayArea.Scale(1 / relativeScale);

			return scaledDisplayArea;
		}

		/*			Math used to calculate base and relative scales.

			The content coordinates, when multiplied by the relativeScale, produces screen coordinates.
			The contentScale is broken into two parts
			The BaseScale is that ratio between the original content and the 'scaled down' content used to reduce the amount of I/O.
			The RelativeScale is that ratio between the 'scaled down' content and the screen coordinates.
						
				For example, instead of reducing a high-resolution resource by 3/8, use a resource with a resolution 1/2 of of the original.
				the BaseScale is 0.5 and the RelativeScale is 0.75 -- 0.75 x 0.5 -> 0.375 which is 3/8ths.


		 			
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

		#region Diagnostics

		[Conditional("DEBUG2")]
		private static void CheckScreenToRelativeScaleFactor(double screenToRelativeScaleFactor, double contentScale)
		{
			var (_, relativeScale) = GetBaseFactorAndRelativeScale(contentScale);

			var chkRelativeScale = 1 / relativeScale;
			Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(screenToRelativeScaleFactor, chkRelativeScale, 0.000001), "ScreenToRelativeScaleFactor maybe incorrect.");
		}

		[Conditional("DEBUG2")]
		private static void CheckNewGetBaseFactorAndRelativeScale(double contentScale, double baseFactor, double relativeScale)
		{
			var (bf, rs) = GetBaseFactorAndRelativeScaleAlt(contentScale);

			Debug.Assert(bf == baseFactor && rs == relativeScale, $"GetBaseFactorAndRelativeScaleAlt disagree. Compare: {baseFactor} / {relativeScale} to {bf} / {rs}.");
		}

		public static (double baseFactor, double relativeScale) GetBaseFactorAndRelativeScaleAlt(double contentScale)
		{
			double baseFactor;
			double relativeScale;

			// Between 1/2 and 1
			if (contentScale > 0.5)
			{
				baseFactor = 0;
				relativeScale = contentScale;
			}

			// Between 1/4 and 1/2
			else if (contentScale > 0.25)
			{
				baseFactor = 1;
				relativeScale = contentScale / 0.5;
			}

			// Between 1/4 and 1/8
			else if (contentScale > 0.125)
			{
				baseFactor = 2;
				relativeScale = contentScale / 0.25;
			}

			// Between 1/8 and 1/16
			else if (contentScale > 0.0625)
			{
				baseFactor = 3;
				relativeScale = contentScale / 0.125;
			}

			// Between 1/16 and 1/32
			else if (contentScale > 0.03125)
			{
				baseFactor = 4;
				relativeScale = contentScale / 0.0625;
			}

			// Between 1/32 and 1/64
			else if (contentScale > 0.015625)
			{
				baseFactor = 5;
				relativeScale = contentScale / 0.03125;
			}

			// Between 1/64 and 1/128
			else if (contentScale > 0.0078125)
			{
				baseFactor = 6;
				relativeScale = contentScale / 0.015625;
			}

			// Between 1/128 and 1/256
			else if (contentScale > 0.00390625)
			{
				baseFactor = 7;
				relativeScale = contentScale / 0.0078125;
			}

			else if (contentScale > 0.001953125)
			{
				baseFactor = 8;
				relativeScale = contentScale / 0.00390625;
			}
			else
			{
				throw new InvalidOperationException($"Values for the ContentScale of 1/256 and less are not supported.");
			}

			return (baseFactor, relativeScale);
		}

		#endregion
	}
}
