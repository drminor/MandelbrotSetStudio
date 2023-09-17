using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionVectors2 //: IPoolable
	{
		private const int VALUE_SIZE = 2;
		private readonly bool _arrayPoolWasUsed = false;

		#region Constructor

		public MapSectionVectors2(SizeInt blockSize) 
			: this(
				  blockSize, 
				  ArrayPool<byte>.Shared.Rent(blockSize.NumberOfCells * VALUE_SIZE), 
				  ArrayPool<byte>.Shared.Rent(blockSize.NumberOfCells * VALUE_SIZE) 
				  )
		{
			_arrayPoolWasUsed = true;
		}

		public MapSectionVectors2(SizeInt blockSize, byte[] counts, byte[] escapeVelocities)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			ValuesPerRow = blockSize.Width;

			BytesPerRow = ValuesPerRow * VALUE_SIZE;
			TotalByteCount = ValueCount * VALUE_SIZE;

			Counts = counts;
			EscapeVelocities = escapeVelocities;

			//ReferenceCount = 0;
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public byte[] Counts { get; set; }
		public byte[] EscapeVelocities { get; set; }

		#endregion

		#region Row Level Methods

		// Vector256<int>
		public void FillCountsRow(int rowNumber, Vector256<int>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<int>, byte>(dest);

			var startIndex = ValuesPerRow * rowNumber * 2;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				var bytePtr = i * 2;
				var wordPtr = i * 4;
				destBack[wordPtr] = Counts[startIndex + bytePtr];
				destBack[wordPtr + 1] = Counts[startIndex + bytePtr + 1];
			}
		}

		public void UpdateFromCountsRow(int rowNumber, Vector256<int>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<int>, byte>(source);

			var startIndex = ValuesPerRow * rowNumber * 2;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				var bytePtr = i * 2;
				var wordPtr = i * 4;
				Counts[startIndex + bytePtr] = sourceBack[wordPtr];
				Counts[startIndex + bytePtr + 1] = sourceBack[wordPtr + 1];
			}
		}


		public void UpdateFromEscapeVelocitiesRow(int rowNumber, ushort[] source)
		{
			var sourceBack = MemoryMarshal.Cast<ushort, byte>(source);

			var startIndex = ValuesPerRow * rowNumber * 2;

			for (var i = 0; i < ValuesPerRow; i++)
			{
				var bytePtr = i * 2;
				EscapeVelocities[startIndex + bytePtr] = sourceBack[bytePtr];
				EscapeVelocities[startIndex + bytePtr + 1] = sourceBack[bytePtr + 1];
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
			// TODO: Fix UpdateFromCountsRow -- IterationStateSingleLimb uses this
			//var startIndex = ValuesPerRow * rowNumber;

			//for (var i = 0; i < ValuesPerRow; i++)
			//{
			//	Counts[startIndex + i] = (ushort)source[i];
			//}
		}

		//// Byte
		//public void FillCountsRow(int rowNumber, byte[] dest)
		//{
		//	var destBack = MemoryMarshal.Cast<byte, uint>(dest);

		//	var startIndex = ValuesPerRow * rowNumber;

		//	for (var i = 0; i < ValuesPerRow; i++)
		//	{
		//		destBack[i] = Counts[startIndex + i];
		//	}
		//}

		//public void UpdateFromCountsRow(int rowNumber, byte[] source)
		//{
		//	var sourceBack = MemoryMarshal.Cast<byte, uint>(source);

		//	var startIndex = ValuesPerRow * rowNumber;

		//	for (var i = 0; i < ValuesPerRow; i++)
		//	{
		//		Counts[startIndex + i] = (ushort)sourceBack[i];
		//	}
		//}

		#endregion

		#region IPoolable Support

		//public int ReferenceCount { get; private set; }

		//public int IncreaseRefCount()
		//{
		//	ReferenceCount++;
		//	return ReferenceCount;
		//}

		//public int DecreaseRefCount()
		//{
		//	ReferenceCount--;
		//	return ReferenceCount;
		//}

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

					if (_arrayPoolWasUsed)
					{
						try
						{
							ArrayPool<byte>.Shared.Return(Counts, clearArray: false);
							ArrayPool<byte>.Shared.Return(EscapeVelocities, clearArray: false);
						}
						catch (Exception e)
						{
							Debug.WriteLine($"Got Exception: {e} while disposing the MapSectionVectors2.");
						}
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

