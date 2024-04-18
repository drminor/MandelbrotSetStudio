
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectRepo.Entities
{
	public record ColorBandRecord(
		int CutOff, 
		string StartCssColor, 
		string BlendStyle, 
		string EndCssColor, 
		double Percentage
		)
	{
		[BsonDefaultValue("Rgb")]
		[BsonIgnoreIfDefault]
		public string? BlendMethod { get; set; } = "Rgb";
	}

}
