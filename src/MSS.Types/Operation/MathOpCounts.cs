
namespace MSS.Types
{
	public class MathOpCounts
	{
		public MathOpCounts()
		{
			NumberOfMCarries = 0;
			NumberOfACarries = 0;
			NumberOfConversions = 0;

			NumberOfSplits = 0;
			NumberOfGetCarries = 0;
			NumberOfGrtrThanOps = 0;
		}

		public int NumberOfMCarries { get; set; }
		public int NumberOfACarries { get; set; }
		public long NumberOfConversions { get; set; }

		public long NumberOfSplits { get;  set; }
		public long NumberOfGetCarries { get; set; }
		public long NumberOfGrtrThanOps { get; set; }

		public void Update(MathOpCounts mathOpCounts)
		{
			NumberOfMCarries = mathOpCounts.NumberOfMCarries;
			NumberOfACarries += mathOpCounts.NumberOfACarries;
			NumberOfConversions += mathOpCounts.NumberOfConversions;

			NumberOfSplits += mathOpCounts.NumberOfSplits;
			NumberOfGetCarries += mathOpCounts.NumberOfGetCarries;
			NumberOfGrtrThanOps += mathOpCounts.NumberOfGrtrThanOps;
		}

		public override string ToString()
		{
			var result = $"Splits: {NumberOfSplits}\tCarries: {NumberOfGetCarries}\tGrtrThanOps: {NumberOfGrtrThanOps}" +
				$"\tAdditions: {NumberOfACarries}\tMultiplications: {NumberOfMCarries}\tNegations: {NumberOfConversions}";

			return result;
		}



	}
}
