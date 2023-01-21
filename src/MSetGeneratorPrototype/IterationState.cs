using MSS.Types;
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

		//public IterationState(int valueCount) 
		//{
		//	ValueCount = valueCount;
		//	VectorCount = valueCount / Vector256<int>.Count;

		//	_unusedCalcsBuffer = new int[ValueCount];

		//	HasEscapedFlagsVectors = new Vector256<int>[VectorCount];
		//	CountsVectors = new Vector256<int>[VectorCount];
		//	EscapeVelocitiesVectors = new Vector256<int>[VectorCount];

		//	DoneFlagsVectors = new Vector256<int>[VectorCount];
		//	UnusedCalcsVectors = new Vector256<int>[VectorCount];
		//}

		public IterationState(MapSectionVectors mapSectionVectors, int stride)
		{
			MapSectionVectors = mapSectionVectors;
			Stride = stride;
			TotalVectorCount = mapSectionVectors.TotalVectorCount;
			VectorsPerRow = stride / Vector256<int>.Count;

			//HasEscapedFlagsVectors = new Vector256<int>[VectorCount];
			//CountsVectors = new Vector256<int>[VectorCount];
			//EscapeVelocitiesVectors = new Vector256<int>[VectorCount];

			DoneFlagsVectors = new Vector256<int>[TotalVectorCount];
			UnusedCalcsVectors = new Vector256<int>[TotalVectorCount];

			_unusedCalcsBuffer = new int[Stride];
		}

		#endregion

		#region Public Properties

		public MapSectionVectors MapSectionVectors;


		public int Stride { get; init; }
		public int TotalVectorCount { get; init; }
		public int VectorsPerRow { get; init; }

		public Vector256<int>[] DoneFlagsVectors { get; private set; }
		public Vector256<int>[] UnusedCalcsVectors { get; private set; }

		//public Vector256<int>[] HasEscapedFlagsVectors;
		//public Vector256<int>[] CountsVectors;
		//public Vector256<int>[] EscapeVelocitiesVectors;

		public Vector256<int>[] HasEscapedFlagsVectors => MapSectionVectors.HasEscapedVectors;
		public Vector256<int>[] CountsVectors => MapSectionVectors.CountVectors;
		public Vector256<int>[] EscapeVelocitiyVectors => MapSectionVectors.EscapeVelocityVectors;

		#endregion

		#region Public Methods

		private int _rowNumber;

		public int RowNumber
		{
			get => _rowNumber;
			set
			{
				_rowNumber = value;

				Array.Clear(DoneFlagsVectors, 0, DoneFlagsVectors.Length);
				Array.Clear(UnusedCalcsVectors, 0, UnusedCalcsVectors.Length);
			}
		}

		public Span<Vector256<int>> GetHasEscapedFlagsRow(int rowNumber)
		{
			var result = MapSectionVectors.GetHasEscapedFlagsRow(rowNumber * VectorsPerRow, VectorsPerRow);
			return result;
		}

		public Span<Vector256<int>> GetCountsRow(int rowNumber)
		{
			var result = MapSectionVectors.GetCountsRow(rowNumber * VectorsPerRow, VectorsPerRow);
			return result;
		}

		public Span<Vector256<int>> GetEscapeVelocitiesRow(int rowNumber)
		{
			var result = MapSectionVectors.GetEscapeVelocitiesRow(rowNumber * VectorsPerRow, VectorsPerRow);
			return result;
		}

		//public void LoadRow(Span<int> hasEscapedFlags, Span<int> counts, Span<int> escapeVelocities)
		//{
		//	LoadVectors(hasEscapedFlags, HasEscapedFlagsVectors);
		//	LoadVectors(counts, CountsVectors);
		//	LoadVectors(escapeVelocities, EscapeVelocitiyVectors);

		//	Array.Clear(DoneFlagsVectors, 0, DoneFlagsVectors.Length);
		//	Array.Clear(UnusedCalcsVectors, 0, UnusedCalcsVectors.Length);
		//}

		//public void CopyBackHasEscapedFlags(Span<int> destination)
		//{
		//	LoadInts(HasEscapedFlagsVectors, destination);
		//}

		//public void CopyBackCounts(Span<int> destination)
		//{
		//	LoadInts(CountsVectors, destination);
		//}

		//public void CopyBackEscapeVelocities(Span<int> destination)
		//{
		//	LoadInts(EscapeVelocitiyVectors, destination);
		//}

		public void CopyBackUnusedCalcs(Span<int> destination)
		{
			LoadInts(UnusedCalcsVectors, destination);
		}

		public int[] GetUnusedCalcs()
		{
			CopyBackUnusedCalcs(_unusedCalcsBuffer);

			return _unusedCalcsBuffer;
		}

		#endregion

		#region Private Methods

		//private void LoadVectors(Span<int> values, Vector256<int>[] vectors)
		//{
		//	var elements = MemoryMarshal.Cast<Vector256<int>, int>(vectors);
		//	values.CopyTo(elements);
		//}

		private void LoadInts(Vector256<int>[] vectors, Span<int> values)
		{
			var elements = MemoryMarshal.Cast<Vector256<int>, int>(vectors);
			elements.CopyTo(values);
		}

		#endregion
	}
}

