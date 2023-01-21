
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionVectors : IPoolable
	{
		#region Constructor

		public MapSectionVectors(SizeInt blockSize)
		{
			BlockSize = blockSize;
			Length = blockSize.NumberOfCells;
			TotalVectorCount = Length / Vector256<uint>.Count;

			HasEscapedVectors = new Vector256<int>[TotalVectorCount];
			HasEscapedMems = new Memory<Vector256<int>>(HasEscapedVectors);

			CountVectors = new Vector256<int>[TotalVectorCount];
			CountMems = new Memory<Vector256<int>>(CountVectors);

			EscapeVelocityVectors = new Vector256<int>[TotalVectorCount];
			EscapeVelocitiyMems = new Memory<Vector256<int>>(EscapeVelocityVectors);
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int Length { get; init; }

		public int TotalVectorCount { get; init; }

		public Vector256<int>[] HasEscapedVectors;
		public Memory<Vector256<int>> HasEscapedMems;

		public Vector256<int>[] CountVectors;
		public Memory<Vector256<int>> CountMems;

		public Vector256<int>[] EscapeVelocityVectors;
		public Memory<Vector256<int>> EscapeVelocitiyMems;

		#endregion

		#region Methods

		public void LoadValuesInto(MapSectionValues mapSectionValues)
		{
			var hasEscapedFlags = new bool[Length];

			var counts = MemoryMarshal.Cast<Vector256<int>, int>(CountVectors);
			ushort[] shortCounts = new ushort[Length];

			for (var i = 0; i < Length; i++)
			{
				shortCounts[i] = (ushort)counts[i];
			}

			ushort[] escapeVelocities = new ushort[Length];

			//var shortCounts = counts.Select(x => (ushort)x).ToArray();
			//var shortEscVels = escapeVelocities.Select(x => (ushort)x).ToArray();
			//var hasEscapedFlaggs = hasEscapedFlags.Select(x => x == 1).ToArray();


			mapSectionValues.Load(hasEscapedFlags, shortCounts, escapeVelocities);
		}

		public Span<Vector256<int>> GetHasEscapedFlagsRow(int start, int length)
		{
			var result = new Span<Vector256<int>>(HasEscapedVectors, start, length);
			return result;
		}

		public Span<Vector256<int>> GetCountsRow(int start, int length)
		{
			var result = new Span<Vector256<int>>(CountVectors, start, length);
			return result;
		}

		public Span<Vector256<int>> GetEscapeVelocitiesRow(int start, int length)
		{
			var result = new Span<Vector256<int>>(EscapeVelocityVectors, start, length);
			return result;
		}
		void IPoolable.ResetObject()
		{
			//Array.Clear(HasEscapedFlags, 0, Length);
			//Array.Clear(Counts, 0, Length);
			//Array.Clear(EscapeVelocities, 0, Length);
		}

		#endregion

		#region IDisposable Support

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					//HasEscapedFlags = Array.Empty<bool>();
					//Counts = Array.Empty<ushort>();
					//EscapeVelocities = Array.Empty<ushort>();
				}

				_disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~MapSectionValues()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}

