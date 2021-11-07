using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MSS.Types.Base
{
	/// <summary>
	/// Imutable, value-type that contains a single string value. 
	/// This was created so that a rectangle using string values can be created.
	/// </summary>
	public struct StringStruct
    {
        private readonly string _stringValue;

        [JsonConstructor]
        [BsonConstructor]
        public StringStruct(string value) : this()
        {
            _stringValue = value;
        }

        public string StringValue
        {
            get => _stringValue;
            init => _stringValue = value ?? string.Empty;
        }

		public override string? ToString()
		{
            return _stringValue;
		}

		public static implicit operator StringStruct(string value)
        {
            return new StringStruct(value);
        }

        public static implicit operator string(StringStruct value)
        {
            return value.StringValue;
        }
    }

}
