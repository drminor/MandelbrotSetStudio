using ProtoBuf;
using System;
using System.Diagnostics;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class FPValuesDto
	{
		[ProtoMember(1)]
		public byte[] Signs;

		[ProtoMember(2)]
		public byte[][] ValueArrays;

		[ProtoMember(3)]
		public byte[] Exponents;

		public FPValuesDto(byte[] signs, byte[][] valueArrays, byte[] exponents)
		{
			Signs = signs;
			ValueArrays = valueArrays;
			Exponents = exponents;
		}

		public FPValuesDto(bool[] signs, ulong[][] mantissas, short[] exponents)
		{
			// ValueArrays[0] = Least Significant Limb (Number of 1's)
			// ValuesArray[1] = Number of 2^64s
			// ValuesArray[2] = Number of 2^128s
			// ValuesArray[n] = Most Significant Limb

			var len = signs.Length;
			Debug.Assert(len == 128 * 128);

			Signs = new byte[len];
			Exponents = new byte[len * 2];

			for (var i = 0; i < len; i++)
			{
				BitConverter.TryWriteBytes(new Span<byte>(Signs, i, 1), signs[i]);
				BitConverter.TryWriteBytes(new Span<byte>(Exponents, i * 2, 2), exponents[i]);
			}

			var numberOfByteArrays = mantissas.Length;
			ValueArrays = new byte[numberOfByteArrays][];

			for (var j = 0; j < numberOfByteArrays; j++)
			{
				ValueArrays[j] = new byte[len * 8];
				for (var i = 0; i < len; i++)
				{
					BitConverter.TryWriteBytes(new Span<byte>(ValueArrays[j], i * 8, 8), mantissas[j][i]);
				}
			}
		}

		public ulong[][] GetValues(out bool[] signs, out short[] exponents)
		{
			var len = Signs.Length;

			signs = new bool[len];
			exponents = new short[len];

			for(var i = 0; i < len; i++)
			{
				signs[i] = BitConverter.ToBoolean(Signs, i);
				exponents[i] = BitConverter.ToInt16(Exponents, i * 2);
			}

			var numberOfByteArrays = ValueArrays.Length;
			var result = new ulong[numberOfByteArrays][];

			for (var j = 0; j < numberOfByteArrays; j++)
			{
				result[j] = new ulong[len];

				for (var i = 0; i < len; i++)
				{
					result[j][i] = BitConverter.ToUInt64(ValueArrays[j], i * 8);
				}
			}

			return result;
		}
	}
}
