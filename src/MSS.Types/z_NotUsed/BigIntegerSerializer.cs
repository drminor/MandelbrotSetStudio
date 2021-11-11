using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Numerics;

namespace MSS.Types.MSetRepo
{
	public class BigIntegerSerializer : SerializerBase<BigInteger>
	{
		public override BigInteger Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			byte[] buf = context.Reader.ReadBytes();
			return new BigInteger(buf);
		}

		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BigInteger value)
		{
			context.Writer.WriteBytes(value.ToByteArray());
		}
	}

}
