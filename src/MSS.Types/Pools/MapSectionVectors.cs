
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
			CountsMemory = new Memory<byte>(Counts);
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int ValuesPerRow { get; init; }
		public int Lanes {get; init; }
		public int VectorsPerRow { get; init; }

		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public byte[] Counts { get; init; }
		public Memory<byte> CountsMemory { get; init; }

		#endregion

		#region Methods

		public void Load(byte[] counts)
		{
			Array.Copy(counts, Counts, Counts.Length);
		}

		public Span<Vector256<int>> GetCountVectors()
		{
			var result = MemoryMarshal.Cast<byte, Vector256<int>>(CountsMemory.Span);
			return result;
		}

		//public void FillCountsRow(Vector256<int>[] mantissas, int rowNumber)
		//{
		//	var sourceStartIndex = BytesPerRow * rowNumber;
		//	var source = new Span<byte>(Counts, sourceStartIndex, BytesPerRow);

		//	Span<byte> destinationByteSpan = MemoryMarshal.Cast<Vector256<int>, byte>(mantissas);
		//	source.CopyTo(destinationByteSpan);
		//}

		//public void UpdateCountsRowFrom(Vector256<int>[] mantissas, int rowNumber)
		//{
		//	var source = MemoryMarshal.Cast<Vector256<int>, byte>(mantissas).ToArray();
		//	var destinationStartIndex = BytesPerRow * rowNumber;
		//	Array.Copy(source, 0, Counts, destinationStartIndex, BytesPerRow);
		//}

		// IPoolable Support
		void IPoolable.ResetObject()
		{
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

