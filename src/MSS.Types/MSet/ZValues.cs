using ProtoBuf;
using System;
using System.Diagnostics;

namespace MSS.Types.MSet
{
	[ProtoContract(SkipConstructor = true)]
	public class ZValues
	{
		[ProtoMember(1)]
		public byte[][] ValuesArray;

		public ZValues(byte[][] valuesArray)
		{
			ValuesArray = valuesArray;
		}

		public ZValues(double[] zValues, int numberOfByteArrays = 4)
		{
			// ValuesArray[0] = zX High Part
			// ValuesArray[1] = zX Low Part
			// ValuesArray[2] = zY High Part
			// ValuesArray[3] = zY Low Part

			var len = zValues.Length / numberOfByteArrays;
			Debug.Assert(len == 128 * 128);

			ValuesArray = new byte[numberOfByteArrays][];

			for (var j = 0; j < numberOfByteArrays; j++)
			{
				ValuesArray[j] = new byte[len * 8];
			}

			var sourcePtr = 0;

			for (var i = 0; i < len; i++)
			{
				for (var j = 0; j < numberOfByteArrays; j++)
				{
					BitConverter.TryWriteBytes(new Span<byte>(ValuesArray[j], i * 8, 8), zValues[sourcePtr++]);
				}
			}
		}

		public double[] GetZValuesAsDoubleArray()
		{
			var len = ValuesArray[0].Length / 8;
			var numberOfByteArrays = ValuesArray.Length;

			var result = new double[len * numberOfByteArrays];
			var resultPtr = 0;

			for(var i = 0; i < len; i++)
			{
				for(var j = 0; j < numberOfByteArrays; j++)
				{
					result[resultPtr++] = BitConverter.ToDouble(ValuesArray[j], i * 8);
				}
			}

			return result;
		}
	}
}
