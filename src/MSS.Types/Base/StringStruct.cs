using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MSS.Types.Base
{
	public struct StringStruct
    {
        private string _value;

        [JsonConstructor]
        [BsonConstructor]
        public StringStruct(string value) : this()
        {
            _value = value;
        }

        public string Value
        {
            get => _value;
            init => _value = value ?? string.Empty;
        }

		public override string? ToString()
		{
            return _value;
		}

		public static implicit operator StringStruct(string value)
        {
            return new StringStruct(value);
        }

        public static implicit operator string(StringStruct value)
        {
            return value.Value;
        }
    }

}
