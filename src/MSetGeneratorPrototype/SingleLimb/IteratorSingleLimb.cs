using MSS.Types;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorSingleLimb
	{
		#region Private Properties

		private double _zrSqr;
		private double _ziSqr;

		#endregion

		#region Constructor

		public IteratorSingleLimb()
		{
			MathOpCounts = new MathOpCounts();
			IsReset = true;
		}

		#endregion

		#region Public Properties

		public bool IsReset { get; private set; }
		public bool IncreasingIterations { get; set; }

		public uint Threshold { get; set; }

		public MathOpCounts MathOpCounts { get; init; }

		#endregion

		#region Public Methods

		public void Reset()
		{
			IsReset = true;
		}

		public bool Iterate(double cr, double ci, double zr, double zi)
		{
			try
			{
				if (IsReset)
				{
					if (!IncreasingIterations)
					{
						// Perform the first iteration. 
						zr = cr;
						zi = ci;
					}
					//else
					//{
					//	_zrSqr = 0;
					//	_ziSqr = 0;
					//}
					IsReset = false;
				}
				else
				{
					// square(z.r + z.i)
					var zRZiSqr = zr + zi;
					zRZiSqr = zRZiSqr * zRZiSqr;

					zi = zRZiSqr - _zrSqr;
					var temp1 = zi - _ziSqr;
					zi = temp1 + ci;
					
					var temp2 = _zrSqr - _ziSqr;
					zr = temp2 + cr;
				}

				_zrSqr = zr * zr;
				_ziSqr = zi * zi;

				var sumOfSqrs = _zrSqr + _ziSqr;

				var result = sumOfSqrs >= Threshold; 
				return result;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"IteratorSingleLimb received exception: {e}.");
				throw;
			}
		}

		#endregion
	}
}
