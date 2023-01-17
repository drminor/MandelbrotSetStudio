
using System.Linq;

namespace MSS.Types
{
	public class MathOpCounts
	{
		public MathOpCounts()
		{
			NumberOfMultiplications = 0;
			NumberOfAdditions = 0;
			NumberOfConversions = 0;

			NumberOfSplits = 0;
			NumberOfGetCarries = 0;
			NumberOfGrtrThanOps = 0;
			NumberOfUnusedCalcs = 0;
		}

		public long NumberOfMultiplications { get; set; }
		public long NumberOfAdditions { get; set; }
		public long NumberOfConversions { get; set; }

		public long NumberOfSplits { get;  set; }
		public long NumberOfGetCarries { get; set; }
		public long NumberOfGrtrThanOps { get; set; }

		public long NumberOfUnusedCalcs { get; set; }

		public void Update(MathOpCounts mathOpCounts)
		{
			NumberOfMultiplications += mathOpCounts.NumberOfMultiplications;
			NumberOfAdditions += mathOpCounts.NumberOfAdditions;
			NumberOfConversions += mathOpCounts.NumberOfConversions;

			NumberOfSplits += mathOpCounts.NumberOfSplits;
			NumberOfGetCarries += mathOpCounts.NumberOfGetCarries;
			NumberOfGrtrThanOps += mathOpCounts.NumberOfGrtrThanOps;

			NumberOfUnusedCalcs += mathOpCounts.NumberOfUnusedCalcs;
		}

		public void RollUpNumberOfUnusedCalcs(int[] unusedCalcRowValues)
		{
			NumberOfUnusedCalcs += unusedCalcRowValues.Sum();
		}

		public override string ToString()
		{
			var result = $"Splits: {NumberOfSplits:N0}\tCarries: {NumberOfGetCarries:N0}\tGrtrThanOps: {NumberOfGrtrThanOps:N0}" +
				$"\tAdditions: {NumberOfAdditions:N0}\tMultiplications: {NumberOfMultiplications:N0}\tNegations: {NumberOfConversions:N0}\tUnusedCalcs: {NumberOfUnusedCalcs:N0}";

			return result;
		}



	}
}
