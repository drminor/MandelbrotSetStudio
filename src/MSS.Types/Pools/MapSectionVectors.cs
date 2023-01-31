
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionVectors : IPoolable
	{
		private const int VALUE_SIZE = 4;

		#region Constructor

		public MapSectionVectors(SizeInt blockSize) : this(blockSize, new byte[blockSize.NumberOfCells * VALUE_SIZE])
		{ }

		public MapSectionVectors(SizeInt blockSize, byte[] counts)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			ValuesPerRow = blockSize.Width;
			Lanes = Vector256<uint>.Count;
			VectorsPerRow = BlockSize.Width / Lanes;

			BytesPerRow = ValuesPerRow * VALUE_SIZE;
			TotalByteCount = ValueCount * VALUE_SIZE;

			Debug.Assert(counts.Length == TotalByteCount, $"The length of counts does not equal the {ValueCount} * {VALUE_SIZE} (values/block * bytes/value) .");

			Counts = counts;

			//CountVectors = new Vector256<int>[TotalVectorCount];
			//CountMems = new Memory<Vector256<int>>(CountVectors);
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int Lanes {get; init; }

		public int ValuesPerRow { get; init; }
		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public byte[] Counts { get; init; }

		//public int TotalVectorCount => ValueCount / Lanes;
		public int VectorsPerRow { get; init; } 

		//public Vector256<int>[] CountVectors;
		//public Memory<Vector256<int>> CountMems;

		#endregion

		#region Methods

		public void FillCountsRow(Vector256<int>[] mantissas, int rowNumber)
		{
			var sourceStartIndex = BytesPerRow * rowNumber;
			var source = new Span<byte>(Counts, sourceStartIndex, BytesPerRow);

			Span<byte> destinationByteSpan = MemoryMarshal.Cast<Vector256<int>, byte>(mantissas);
			source.CopyTo(destinationByteSpan);
		}

		public void UpdateCountsRowFrom(Vector256<int>[] mantissas, int rowNumber)
		{
			var source = MemoryMarshal.Cast<Vector256<int>, byte>(mantissas).ToArray();
			var destinationStartIndex = BytesPerRow * rowNumber;
			Array.Copy(source, 0, Counts, destinationStartIndex, BytesPerRow);
		}

		//public void UpdateCountsFromOld(ushort[] counts)
		//{
		//	var dest = MemoryMarshal.Cast<Vector256<int>, int>(CountVectors);

		//	for (var i = 0; i < ValueCount; i++)
		//	{
		//		dest[i] = counts[i];
		//	}
		//}

		// IPoolable Support
		void IPoolable.ResetObject()
		{
			//Array.Clear(CountVectors, 0, TotalVectorCount);
			Array.Clear(Counts, 0, TotalByteCount);
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
			//CountMems.CopyTo(mapSectionVectors.CountMems);
			Array.Copy(Counts, mapSectionVectors.Counts, TotalByteCount);
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

