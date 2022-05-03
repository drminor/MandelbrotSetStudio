using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace MSetExplorer
{
	public class SingleCoordEditorViewModel : ViewModelBase
	{
		private string _stringVal;
		private string? _stringValOut;

		private RValue _rValue;

		private string? _numerator;
		private int _exponent;

		private long _long1;
		private long _long2;

		private string _valueName;

		public SingleCoordEditorViewModel(string stringVal)
		{
			_stringVal = stringVal;
			_stringValOut = null;

			_rValue = new RValue();
			SignManExp = new SignManExp();
			_numerator = null;
			_exponent = 0;
			_long1 = 0;
			_long2 = 0;

			_valueName = "XX";

			UpdateOurValues(stringVal);
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
					UpdateOurValues(value);

					OnPropertyChanged();
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
					OnPropertyChanged();
				}
			}
		}

		public SignManExp SignManExp { get; private set; }

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
						var bNumerator = BigInteger.Parse(_numerator, CultureInfo.InvariantCulture);
						_rValue = new RValue(bNumerator, RValue.Exponent);
					}
					else
					{
						_rValue = new RValue(0, RValue.Exponent);
					}

					_stringValOut = RValueHelper.ConvertToString(_rValue);

					OnPropertyChanged();
					OnPropertyChanged(nameof(StringValOut));
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

					OnPropertyChanged();
					OnPropertyChanged(nameof(StringValOut));
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

		public string ValueName
		{
			get => _valueName;
			set
			{
				if (value != _valueName)
				{
					_valueName = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		private void UpdateOurValues(string s)
		{
			SignManExp = new SignManExp(s);
			_rValue = RValueHelper.ConvertToRValue(SignManExp);
			_numerator = _rValue.Value.ToString(CultureInfo.InvariantCulture);
			_exponent = _rValue.Exponent;

			var longVals = BigIntegerHelper.ToLongs(_rValue.Value);
			_long1 = longVals[0];
			_long2 = longVals[1];

			_stringValOut = RValueHelper.ConvertToString(_rValue);

			//var rrNum = BigIntegerHelper.FromLongs(longVals);
			//var rr = new RValue(rrNum, _exponent);
			//_stringValOut = RValueHelper.ConvertToString(rr);
		}

		#endregion

	}
}
