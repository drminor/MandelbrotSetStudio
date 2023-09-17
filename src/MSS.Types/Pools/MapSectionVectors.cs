using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionVectors : IPoolable
	{
		private const int VALUE_SIZE = 2;

		#region Constructor

		public MapSectionVectors(SizeInt blockSize) 
			: this(
				  blockSize, 
				  ArrayPool<ushort>.Shared.Rent(blockSize.NumberOfCells), 
				  ArrayPool<ushort>.Shared.Rent(blockSize.NumberOfCells), 
				  ArrayPool<byte>.Shared.Rent(blockSize.NumberOfCells * 4)
				  )
		{ }

		public MapSectionVectors(SizeInt blockSize, ushort[] counts, ushort[] escapeVelocities, byte[] backBuffer)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			ValuesPerRow = blockSize.Width;

			BytesPerRow = ValuesPerRow * VALUE_SIZE;
			TotalByteCount = ValueCount * VALUE_SIZE;

			Counts = counts;
			EscapeVelocities = escapeVelocities;
			BackBuffer = backBuffer;

			ReferenceCount = 0;
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public ushort[] Counts { get; init; }
		public ushort[] EscapeVelocities { get; init; }
		public byte[] BackBuffer { get; init; }

		#endregion

		#region Block Level Methods

		public void Load(byte[] counts, byte[] escapeVelocites)
		{
			var destBackCounts = MemoryMarshal.Cast<ushort, byte>(Counts);
			var destBackEscapeVelocities = MemoryMarshal.Cast<ushort, byte>(EscapeVelocities);

			for (var i = 0; i < counts.Length; i++)
			{
				destBackCounts[i] = counts[i];
				destBackEscapeVelocities[i] = escapeVelocites[i];
			}
		}

		public void LoadCounts(byte[] counts)
		{
			var destBackCounts = MemoryMarshal.Cast<ushort, byte>(Counts);

			for (var i = 0; i < counts.Length; i++)
			{
				destBackCounts[i] = counts[i];
			}
		}

		public void LoadEscapeVelocities(byte[] escapeVelocites)
		{
			var destBackEscapeVelocities = MemoryMarshal.Cast<ushort, byte>(EscapeVelocities);

			for (var i = 0; i < escapeVelocites.Length; i++)
			{
				destBackEscapeVelocities[i] = escapeVelocites[i];
			}
		}

		public byte[] GetSerializedCounts()
		{
			var result = new byte[TotalByteCount];

			var destSource = MemoryMarshal.Cast<ushort, byte>(Counts);

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = destSource[i];
			}

			return result;
		}

		public byte[] GetSerializedEscapeVelocities()
		{
			var result = new byte[TotalByteCount];

			var destSource = MemoryMarshal.Cast<ushort, byte>(EscapeVelocities);

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = destSource[i];
			}

			return result;
		}

		#endregion

		#region Row Level Methods

		// Vector256<int>
		public void FillCountsRow(int rowNumber, Vector256<int>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<int>, uint>(dest);

			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				destBack[i] = Counts[startIndex + i];
			}
		}

		public void UpdateFromCountsRow(int rowNumber, Vector256<int>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<int>, uint>(source);

			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				Counts[startIndex + i] = (ushort)sourceBack[i];
			}
		}


		public void UpdateFromEscapeVelocitiesRow(int rowNumber, ushort[] source)
		{
			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				EscapeVelocities[startIndex + i] = source[i];
			}
		}

		// Vector256<float>
		public void FillCountsRow(int rowNumber, Vector256<float>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<float>, float>(dest);

			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				destBack[i] = Counts[startIndex + i];
			}
		}

		public void UpdateFromCountsRow(int rowNumber, Vector256<float>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<float>, float>(source);

			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				Counts[startIndex + i] = (ushort)  Math.Round(sourceBack[i]);
			}
		}

		// Integer
		public void FillCountsRow(int rowNumber, int[] dest)
		{
			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				dest[i] = Counts[startIndex + i];
			}
		}

		public void UpdateFromCountsRow(int rowNumber, int[] source)
		{
			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				Counts[startIndex + i] = (ushort)source[i];
			}
		}

		// Byte
		public void FillCountsRow(int rowNumber, byte[] dest)
		{
			var destBack = MemoryMarshal.Cast<byte, uint>(dest);

			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				destBack[i] = Counts[startIndex + i];
			}
		}

		public void UpdateFromCountsRow(int rowNumber, byte[] source)
		{
			var sourceBack = MemoryMarshal.Cast<byte, uint>(source);

			var startIndex = ValuesPerRow * rowNumber;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				Counts[startIndex + i] = (ushort)sourceBack[i];
			}
		}


		#endregion

		#region IPoolable Support

		public int ReferenceCount { get; private set; }

		public int IncreaseRefCount()
		{
			ReferenceCount++;
			return ReferenceCount;
		}

		public int DecreaseRefCount()
		{
			ReferenceCount--;
			return ReferenceCount;
		}

		public void ResetObject()
		{
			Array.Clear(Counts);
			Array.Clear(EscapeVelocities);
		}

		//void IPoolable.CopyTo(object obj)
		//{
		//	if (obj != null && obj is MapSectionVectors destination)
		//	{
		//		CopyTo(destination);
		//	}
		//	else
		//	{
		//		throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionVectors)}");
		//	}
		//}

		//public void CopyTo(MapSectionVectors destination)
		//{
		//	Array.Copy(Counts, destination.Counts, ValueCount);
		//	Array.Copy(EscapeVelocities, destination.EscapeVelocities, ValueCount);
		//}

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

					//if (!FromRepo)
					//{
					//	ArrayPool<ushort>.Shared.Return(Counts, clearArray: true);
					//}

					ArrayPool<ushort>.Shared.Return(Counts, clearArray: false);
					ArrayPool<ushort>.Shared.Return(EscapeVelocities, clearArray: false);
					ArrayPool<byte>.Shared.Return(BackBuffer, clearArray: false);

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

