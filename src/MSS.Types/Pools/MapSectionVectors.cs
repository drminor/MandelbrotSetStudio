
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

			CountVectors = new Vector256<int>[TotalVectorCount];
			CountMems = new Memory<Vector256<int>>(CountVectors);
		}

		#endregion

		#region Public Properties

		public int Lanes => Vector256<int>.Count;
		public int Length { get; init; }
		public int TotalVectorCount => Length / Lanes;

		public SizeInt BlockSize { get; init; }
		public int VectorsPerRow => BlockSize.Width / Lanes;

		public Vector256<int>[] CountVectors;
		public Memory<Vector256<int>> CountMems;

		#endregion

		#region Methods

		public void UpdateCountsFrom(ushort[] counts)
		{
			var dest = MemoryMarshal.Cast<Vector256<int>, int>(CountVectors);

			for (var i = 0; i < Length; i++)
			{
				dest[i] = counts[i];
			}
		}

		// IPoolable Support
		void IPoolable.ResetObject()
		{
			Array.Clear(CountVectors, 0, TotalVectorCount);
		}

		void IPoolable.CopyTo(object obj)
		{
			if (obj != null && obj is MapSectionVectors msv)
			{
				CopyTo(msv);
			}
			else
			{
				throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionVectors)}");
			}
		}

		public void CopyTo(MapSectionVectors mapSectionVectors)
		{
			CountMems.CopyTo(mapSectionVectors.CountMems);
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

