using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public class IterationState
	{
		#region Private Properties

		private int[] _unusedCalcsBuffer;

		#endregion

		#region Constructor

		public IterationState(int valueCount) 
		{
			ValueCount = valueCount;
			VectorCount = valueCount / Vector256<int>.Count;

			_unusedCalcsBuffer = new int[ValueCount];

			HasEscapedFlagsVectors = new Vector256<int>[VectorCount];
			CountsVectors = new Vector256<int>[VectorCount];
			EscapeVelocitiesVectors = new Vector256<int>[VectorCount];
			DoneFlagsVectors = new Vector256<int>[VectorCount];
			UnusedCalcsVectors = new Vector256<int>[VectorCount];
		}

		#endregion

		#region Public Properties

		public int ValueCount { get; init; }
		public int VectorCount { get; init; }

		public Vector256<int>[] HasEscapedFlagsVectors;
		public Vector256<int>[] CountsVectors;
		public Vector256<int>[] EscapeVelocitiesVectors;
		public Vector256<int>[] DoneFlagsVectors;
		public Vector256<int>[] UnusedCalcsVectors;

		#endregion

		#region Public Methods

		public void CopyBackHasEscapedFlags(Span<int> destination)
		{
			LoadInts(HasEscapedFlagsVectors, destination);
		}

		public void CopyBackCounts(Span<int> destination)
		{
			LoadInts(CountsVectors, destination);
		}

		public void CopyBackEscapeVelocities(Span<int> destination)
		{
			LoadInts(EscapeVelocitiesVectors, destination);
		}

		public void CopyBackUnusedCalcs(Span<int> destination)
		{
			LoadInts(UnusedCalcsVectors, destination);
		}

		public int[] GetUnusedCalcs()
		{
			CopyBackUnusedCalcs(_unusedCalcsBuffer);

			return _unusedCalcsBuffer;
		}

		public void LoadRow(Span<int> hasEscapedFlags, Span<int> counts, Span<int> escapeVelocities)
		{
			LoadVectors(hasEscapedFlags, HasEscapedFlagsVectors);
			LoadVectors(counts, CountsVectors);
			LoadVectors(escapeVelocities, EscapeVelocitiesVectors);

			Array.Clear(DoneFlagsVectors, 0, DoneFlagsVectors.Length);
			Array.Clear(UnusedCalcsVectors, 0, UnusedCalcsVectors.Length);
		}

		#endregion

		#region Private Methods

		private void LoadVectors(Span<int> values, Vector256<int>[] vectors)
		{
			var elements = MemoryMarshal.Cast<Vector256<int>, int>(vectors);
			values.CopyTo(elements);
		}

		private void LoadInts(Vector256<int>[] vectors, Span<int> values)
		{
			var elements = MemoryMarshal.Cast<Vector256<int>, int>(vectors);
			elements.CopyTo(values);
		}

		#endregion
	}
}

