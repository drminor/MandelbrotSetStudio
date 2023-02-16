using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionVectors : IPoolable
	{
		private const int VALUE_SIZE = 4;

		#region Constructor

		public MapSectionVectors(SizeInt blockSize) : this(blockSize, ArrayPool<byte>.Shared.Rent(blockSize.NumberOfCells * VALUE_SIZE))
		{
			FromRepo = false;
		}

		public MapSectionVectors(SizeInt blockSize, byte[] counts)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			ValuesPerRow = blockSize.Width;

			BytesPerRow = ValuesPerRow * VALUE_SIZE;
			TotalByteCount = ValueCount * VALUE_SIZE;

			Counts = counts;
			FromRepo = true;
		}

		#endregion

		#region Public Properties

		public bool FromRepo { get; init; }

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public byte[] Counts { get; init; }

		#endregion

		#region Methods

		public void Load(byte[] counts)
		{
			Array.Copy(counts, Counts, Counts.Length);
		}

		public void FillCountsRow(int rowNumber, Vector256<int>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<int>, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				destBack[i] = Counts[startIndex + i];
			}
		}

		public void FillCountsRow(int rowNumber, int[] dest)
		{
			var destBack = MemoryMarshal.Cast<int, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				destBack[i] = Counts[startIndex + i];
			}
		}

		public void UpdateFromCountsRow(int rowNumber, Vector256<int>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<int>, byte>(source);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				Counts[startIndex + i] = sourceBack[i];
			}
		}

		// From an Array of Ints
		public void UpdateFromCountsRow(int rowNumber, int[] source)
		{
			var sourceBack = MemoryMarshal.Cast<int, byte>(source);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				Counts[startIndex + i] = sourceBack[i];
			}
		}

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

					if (!FromRepo)
					{
						ArrayPool<byte>.Shared.Return(Counts, clearArray: true);
					}
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

