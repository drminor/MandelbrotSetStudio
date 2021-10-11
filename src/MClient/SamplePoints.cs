
namespace MClient
{
	public class SamplePoints<T>
	{
		public readonly T[][] XValueSections;
		public readonly T[][] YValueSections;

		public readonly int NumberOfHSections;
		public readonly int NumberOfVSections;

		public readonly int LastSectionWidth;
		public readonly int LastSectionHeight;

		public SamplePoints(T[][] xValueSections, T[][] yValueSections)
		{
			XValueSections = xValueSections;
			YValueSections = yValueSections;

			if (xValueSections != null)
			{
				NumberOfHSections = XValueSections.GetUpperBound(0) + 1;
				LastSectionWidth = XValueSections[this.NumberOfHSections - 1].Length;
			}

			if (yValueSections != null)
			{
				NumberOfVSections = YValueSections.GetUpperBound(0) + 1;
				LastSectionHeight = YValueSections[this.NumberOfVSections - 1].Length;
			}
		}


	}
}
