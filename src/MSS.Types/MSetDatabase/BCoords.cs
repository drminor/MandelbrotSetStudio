using System;

namespace MSS.Types.MSetDatabase
{
	[Serializable]
	public record BCoords(string Display, BCoordsPoints BCoordsPoints, int ValueDepth)
	{
		public BCoords() : this(string.Empty, new BCoordsPoints(), 0)
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
