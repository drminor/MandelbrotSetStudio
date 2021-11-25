using System.Runtime.Serialization;

namespace MSS.Types.MSet
{
	[DataContract]
	public class MapCalcSettings
	{
		[DataMember(Order = 1)]
		public int MaxIterations { get; init; }

		[DataMember(Order = 2)]
		public int Threshold { get; init; }

		[DataMember(Order = 3)]
		public int IterationsPerStep { get; init; }

		public MapCalcSettings()
		{
			MaxIterations = 0;
			Threshold = 0;
			IterationsPerStep = 0;
		}

		public MapCalcSettings(int maxIterations, int threshold, int iterationsPerStep)
		{
			MaxIterations = maxIterations;
			Threshold = threshold;
			IterationsPerStep = iterationsPerStep;
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string? ToString()
		{
			return base.ToString();
		}

		public static bool operator ==(MapCalcSettings left, MapCalcSettings right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MapCalcSettings left, MapCalcSettings right)
		{
			return !(left == right);
		}
	}

}
