namespace ProjectRepo.Entities
{
	public record ReservedColorBandRecord(
		string StartCssColor, 
		string BlendStyle, 
		string EndCssColor
		)
	{
		public string BlendMethod { get; set; } = "Rgb";

	}

}
