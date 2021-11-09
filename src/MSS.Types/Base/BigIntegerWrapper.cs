using MongoDB.Bson.Serialization.Attributes;
using System.Numerics;
using System.Text.Json.Serialization;

namespace MSS.Types.Base
{
    /// <summary>
    /// Wraps a BigInteger that presents itself as a byte array.
    /// </summary>
    public struct BigIntegerWrapper
    {
        private byte[] _value;

        [JsonConstructor]
        [BsonConstructor]
        public BigIntegerWrapper(byte[] value) : this()
        {
            _value = value;
        }

        public BigIntegerWrapper(BigInteger bigInteger)
        {
            _value = bigInteger.ToByteArray();
        }

        public byte[] Value
        {
            get => _value;
            set => _value = value;
        }

		[JsonIgnore]
		[BsonIgnore]
		public BigInteger BigInteger => new BigInteger(_value);

		public static implicit operator BigIntegerWrapper(BigInteger value)
		{
			return new BigIntegerWrapper(value);
		}

		public static implicit operator BigInteger(BigIntegerWrapper value)
		{
			return value.BigInteger;
		}
	}

}
