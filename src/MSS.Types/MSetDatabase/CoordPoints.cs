using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types.MSetDatabase
{
	[Serializable]
	public record CoordPoints(byte[] X1, byte[] X2, byte[] Y1, byte[] Y2, int Exponent)
	{
		public CoordPoints() : this(new BigInteger[] { 0, 0, 0, 0 }, 0)
		{ }

		public CoordPoints(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => v.ToByteArray()).ToArray(), exponent)
		{ }

		public CoordPoints(byte[][] values, int exponent) : this(values[0], values[1], values[2], values[3], exponent)
		{ }

		public byte[][] GetValues()
		{
			byte[][] result = new byte[][] { X1, X2, Y1, Y2 };
			return result;
		}
	}

	//public class BigIntegerSerializer : SerializerBase<BigInteger>
	//{
	//	public override BigInteger Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	//	{
	//		byte[] buf = context.Reader.ReadBytes();
	//		return new BigInteger(buf);
	//	}

	//	public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BigInteger value)
	//	{
	//		context.Writer.WriteBytes(value.ToByteArray());
	//	}
	//}

}
