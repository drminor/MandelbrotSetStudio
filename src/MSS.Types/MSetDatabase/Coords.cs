using System;

namespace MSS.Types.MSetDatabase
{
	[Serializable]
	public record Coords(string Display, CoordPoints CoordsPoints, int ValueDepth)
	{
		public Coords() : this(string.Empty, new CoordPoints(), 0)
		{ }
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
