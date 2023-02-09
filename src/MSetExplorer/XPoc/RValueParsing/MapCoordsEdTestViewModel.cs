using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace MSetExplorer.XPoc
{
	public class MapCoordsEdTestViewModel : ViewModelBase
	{
		private string _stringVal;
		private string? _stringValOut;

		private RValue _rValue;

		private string? _numerator;
		private int _exponent;

		private double _double1;
		private double _double2;

		private long _long1;
		private long _long2;

		//private long _zoom;
		//private int _precision;


		public MapCoordsEdTestViewModel()
		{
			_stringVal = string.Empty; //RValueHelper.ConvertToString(_coords.Right);
			_stringValOut = null;

			_rValue = new RValue();
			_numerator = null;
			_exponent = 0;
			_double1 = 0;
			_double2 = 0;
			_long1 = 0;
			_long2 = 0;

			//_zoom = 0;
			//_precision = 0;
		}

		#region Public Properties

		public string StringVal
		{
			get => _stringVal;
			set
			{
				if (value != _stringVal)
				{
					_stringVal = value;
					//_double1 = double.Parse(value);

					_rValue = RValueHelper.ConvertToRValue(value);
					_numerator = _rValue.Value.ToString();
					_exponent = _rValue.Exponent;


					//_stringValOut = RValueHelper.ConvertToString(_rValue);
					//_double2 = BigIntegerHelper.ConvertToDouble(_rValue);

					var longVals = BigIntegerHelper.ToLongsDeprecated(_rValue.Value);
					_long1 = longVals[0];
					_long2 = longVals[1];

					_double1 = _long1 * Math.Pow(2, 53 + Exponent);
					_double2 = _long2 * Math.Pow(2, Exponent);


					var rrNum = BigIntegerHelper.FromLongsDeprecated(longVals);
					var rr = new RValue(rrNum, _exponent, _rValue.Precision);
					_stringValOut = RValueHelper.ConvertToString(rr);

					OnPropertyChanged();
					OnPropertyChanged(nameof(Double1));
					OnPropertyChanged(nameof(Double2));
					//OnPropertyChanged(nameof(RValue));
					OnPropertyChanged(nameof(Numerator));
					OnPropertyChanged(nameof(Exponent));
					OnPropertyChanged(nameof(StringValOut));

					OnPropertyChanged(nameof(Long1));
					OnPropertyChanged(nameof(Long2));
				}
			}
		}

		public RValue RValue
		{
			get => _rValue;
			set
			{
				if (value != _rValue)
				{
					_rValue = value;
					//_numerator = _rValue.Value.ToString();
					//_exponent = _rValue.Exponent;

					//_stringValOut = RValueHelper.ConvertToString(value);

					OnPropertyChanged();
					//OnPropertyChanged(nameof(Numerator));
					//OnPropertyChanged(nameof(Exponent));
					//OnPropertyChanged(nameof(StringValOut));
				}
			}
		}

		public string? Numerator
		{
			get => _numerator;
			set
			{
				if (value != _numerator)
				{
					_numerator = value;

					if (_numerator != null)
					{
						var bNumerator = BigInteger.Parse(_numerator);
						_rValue = new RValue(bNumerator, RValue.Exponent);
					}
					else
					{
						_rValue = new RValue(0, RValue.Exponent);
					}

					_stringValOut = RValueHelper.ConvertToString(_rValue);
					_double2 = BigIntegerHelper.ConvertToDouble(_rValue);

					OnPropertyChanged();
					//OnPropertyChanged(nameof(RValue));
					OnPropertyChanged(nameof(StringValOut));
					OnPropertyChanged(nameof(Double2));

				}
			}
		}

		public int Exponent
		{
			get => _exponent;
			set
			{
				if (value != _exponent)
				{
					_exponent = value;
					_rValue = new RValue(RValue.Value, value);

					_stringValOut = RValueHelper.ConvertToString(_rValue);
					_double2 = BigIntegerHelper.ConvertToDouble(_rValue);

					OnPropertyChanged();
					//OnPropertyChanged(nameof(RValue));
					OnPropertyChanged(nameof(StringValOut));
					OnPropertyChanged(nameof(Double2));
				}
			}
		}

		public double Double1
		{
			get => _double1;
			set
			{
				if (value != _double1)
				{
					_double1 = value;
					_stringValOut = _double1.ToString("G17");
					OnPropertyChanged();
					OnPropertyChanged(nameof(StringValOut));
				}
			}
		}

		public double Double2
		{
			get => _double2;
			set
			{
				if (value != _double2)
				{
					_double2 = value;
					OnPropertyChanged();
				}
			}
		}

		public long Long1
		{
			get => _long1;
			set
			{
				if (value != _long1)
				{
					_long1 = value;
					OnPropertyChanged();
				}
			}
		}

		public long Long2
		{
			get => _long2;
			set
			{
				if (value != _long2)
				{
					_long2 = value;
					OnPropertyChanged();
				}
			}
		}

		public string? StringValOut
		{
			get => _stringValOut;
			set
			{
				if (value != _stringValOut)
				{
					_stringValOut = value;
					OnPropertyChanged();
				}
			}
		}


		#endregion

		#region Public Methods

		public string Test(string s)
		{
			if (RValueHelper.TryConvertToRValue(s, out var rValue))
			{
				//var s3 = rValue.ToString();
				var s2 = RValueHelper.ConvertToString(rValue);
				return s2;
			}
			else
			{
				return "bad RVal";
			}
		}

		#endregion

		#region Private Methods

		private void TestMulti(string[] mVals)
		{
			var rt = RValueHelper.BuildRRectangle(mVals);

			Debug.WriteLine($"The rr result is {rt}");
		}


		#endregion

		#region OLD Test Methods

		public static void Test(RValue rValue)
		{
			// "0.535575821681765930306959274776606";
			// "0.535575821681765
			//	0.000000000000000930306959274776606

			// "0.000000000000000000930306959274776606
			// "0000000000000000306959274776606";
			// "00000000000000000306959274776606
			// "0.0000000000000000306959274776606"

			//0.00000000000000930306959274776606

			// "5355758216817659000000000000000";

			//var dVals = ConvertToDoubles(rValue.Value, rValue.Exponent).ToArray();

			//var rVals = dVals.Select(x => ConvertToRValue(x, 0)).ToArray();

			//var c = Sum(rVals);

			//Debug.WriteLine($"C = {c}.");
		}

		public static RValue Test2(string s)
		{
			var result = RValueHelper.ConvertToRValue(s);
			Debug.WriteLine($"The final result from Test2 is {result}.");

			return result;
		}

		//public void TestSumOld()
		//{
		//	var s = "0.3323822021484375";
		//	var rVal = RValueHelper.ConvertToRValue(s, 0);
		//	RValueHelper.Test(rVal);
		//}

		public void TestSum()
		{
			//var s = "0.152816772460937588";
			var s = "0.53557582168176593030695927477";

			var rVal = Test2(s);
		}

		public void TestDivOld()
		{
			//var rectDividen = RMapConstants.TEST_RECTANGLE_HALF;

			//var x1 = rectDividen.LeftBot.X;  // 1/2 -- 0.5
			//var x2 = rectDividen.RightTop.X; // 2/2 -- 1.0

			//var v1 = new RValue(x2.Value - x1.Value, x2.Exponent); // 1/2

			//var divisorTarget = 128 * 10; // Not an integer power of 2
			//var divisorUsed = 128 * 16; // 2 ^ 7 + 4

			//var ratFromUsedToTarget = divisorTarget / (double)divisorUsed;

			//var divisorUsedRRecprical = new RValue(1, 11);
			//var spdUsed = new RValue(1, 12);
			//var spdTarget = new RValue(5, 20); // 1 / 2 ^ 11 * 5 / 8

			//// new X2 = X1 + rat * width / divisorUsed == 5/8 * (0.5 / (128 * 16))

			////var newX2 = new RValue

			//// 1280 * 5 / ( 1 / 2^20)


			//// 1/2 divided into 1280 parts
			//// vs

			//// 1/2 * 8/5 / 128 * 10 * 8/5
			//// 1/2 / 128 * 16


			//// X2 = 1/2 + 1/2
			//// == 1/2 + 128 * 16 * (1/2 / (128 * 16))

			//// == 1/2 + 


			//// 1/12 = 2/24 / 4/48 / 8/96 / 16/192


		}

		#endregion
	}
}
