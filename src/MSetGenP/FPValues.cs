using MSS.Types.DataTransferObjects;

namespace MSetGenP
{
	public class FPValues
	{
		#region Constructors

		public FPValues(FPValuesDto fPValuesDto)
		{
			Mantissas = fPValuesDto.GetValues(out var signs, out var exponents);
			Signs = signs;
			Exponents = exponents;
		}

		public FPValues(bool[] signs, ulong[][] mantissas, short[] exponents)
		{
			// ValueArrays[0] = Least Significant Limb (Number of 1's)
			// ValuesArray[1] = Number of 2^64s
			// ValuesArray[2] = Number of 2^128s
			// ValuesArray[n] = Most Significant Limb

			var len = exponents.Length;	
			if (signs.Length != len)
			{
				throw new ArgumentException("The number of sign values is different from the number of exponentsl.");
			}

			for(int i = 0; i < mantissas.Length; i++)
			{
				if (mantissas[i].Length != len)
				{
					throw new ArgumentException($"The number of values in the {i}th array of mantissas is differnt from the number of exponents.");
				}
			}

			Signs = signs;
			Mantissas = mantissas;
			Exponents = exponents;
		}

		public FPValues(Smx[] smxes)
		{
			Signs = new bool[smxes.Length];
			Exponents = new short[smxes.Length];

			var numberOfLimbs = smxes[0].Mantissa.Length;
			Mantissas = new ulong[numberOfLimbs][];

			for(var i = 0; i < smxes.Length; i++)
			{
				Signs[i] = smxes[i].Sign;
				Exponents[i] = (short)smxes[i].Exponent;
			}

			for(var j = 0; j < numberOfLimbs; j++)
			{
				Mantissas[j] = new ulong[smxes.Length];

				for (var i = 0; i < smxes.Length; i++)
				{
					Mantissas[j][i] = smxes[i].Mantissa[j];
				}
			}

		}

		#endregion

		#region Public Properties

		public bool[] Signs { get; init; }

		public ulong[][] Mantissas { get; init; } 

		public short[] Exponents { get; init; }

		#endregion
	}
}
