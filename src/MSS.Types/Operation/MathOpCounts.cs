using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MSS.Types
{
	[DataContract]
	public class MathOpCounts : ICloneable
	{
		#region Public Propeties

		[DataMember(Order = 1)]
		public long NumberOfMultiplications { get; set; }
		public long NumberOfAdditions { get; set; }
		public long NumberOfNegations { get; set; }
		public long NumberOfConversions { get; set; }

		public long NumberOfSplits { get;  set; }
		public long NumberOfComparisons { get; set; }

		public double NumberOfCalcs { get; set; }
		public double NumberOfUnusedCalcs { get; set; }

		#endregion

		#region Pubic Methods

		public void Update(MathOpCounts mathOpCounts)
		{
			NumberOfMultiplications += mathOpCounts.NumberOfMultiplications;
			NumberOfAdditions += mathOpCounts.NumberOfAdditions;
			NumberOfConversions += mathOpCounts.NumberOfConversions;

			NumberOfSplits += mathOpCounts.NumberOfSplits;
			NumberOfNegations += mathOpCounts.NumberOfNegations;
			NumberOfComparisons += mathOpCounts.NumberOfComparisons;

			NumberOfCalcs += mathOpCounts.NumberOfCalcs;
			NumberOfUnusedCalcs += mathOpCounts.NumberOfUnusedCalcs;
		}

		public void Reset()
		{
			NumberOfMultiplications = 0;
			NumberOfAdditions = 0;
			NumberOfConversions = 0;

			NumberOfSplits = 0;
			NumberOfNegations = 0;
			NumberOfComparisons = 0;

			NumberOfCalcs = 0;
			NumberOfUnusedCalcs = 0;
		}

		public void RollUpNumberOfCalcs(long[] usedCalcRowValues, long[] unusedCalcRowValues)
		{
			//var used = usedCalcRowValues.Sum();
			//var unused = unusedCalcRowValues.Sum();

			//if (unused > 0)
			//{
			//	Debug.WriteLine($"Hi3");
			//}

			NumberOfCalcs += usedCalcRowValues.Sum();
			NumberOfUnusedCalcs += unusedCalcRowValues.Sum();
		}


		#endregion

		#region ToString and IClonable Support

		public override string ToString()
		{
			var result = $"Splits: {NumberOfSplits:N0}\tCarries: {NumberOfNegations:N0}\tGrtrThanOps: {NumberOfComparisons:N0}" +
				$"\tAdditions: {NumberOfAdditions:N0}\tMultiplications: {NumberOfMultiplications:N0}\tNegations: {NumberOfConversions:N0}\tUnusedCalcs: {NumberOfUnusedCalcs:N0}";

			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MathOpCounts Clone()
		{
			return (MathOpCounts)MemberwiseClone();
		}

		#endregion
	}
}
