using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionZVectors : IPoolable
	{
		private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

		#region Constructor

		private const int VALUE_SIZE = 4;

		//public MapSectionZVectors(SizeInt blockSize, int limbCount)
		//{
		//	BlockSize = blockSize;
		//	_limbCount = limbCount;

		//	ValueCount = blockSize.NumberOfCells;
		//	Lanes = Vector256<uint>.Count;
		//	ValuesPerRow = blockSize.Width;
		//	RowCount = blockSize.Height;

		//	VectorsPerRow = ValuesPerRow / Lanes;
		//	BytesPerRow = ValuesPerRow * VALUE_SIZE;
		//	TotalBytesForFlags = ValueCount * VALUE_SIZE;

		//	VectorsPerZValueRow = VectorsPerRow * limbCount; // ValuesPerRow * LimbCount / Lanes;
		//	BytesPerZValueRow = BytesPerRow * limbCount; // ValuesPerRow * LimbCount * VALUE_SIZE;
		//	TotalByteCount = TotalBytesForFlags * limbCount; // ValueCount * LimbCount * VALUE_SIZE;

		//	Zrs = _arrayPool.Rent(TotalByteCount);
		//	Zis = _arrayPool.Rent(TotalByteCount);

		//	Array.Clear(Zrs);
		//	Array.Clear(Zis);

		//	HasEscapedFlags = new byte[TotalBytesForFlags];
		//	RowHasEscaped = new bool[RowCount];
		//}

		public MapSectionZVectors(SizeInt blockSize, int limbCount)
			: this(
				  blockSize,
				  limbCount,
				  _arrayPool.Rent(blockSize.NumberOfCells * VALUE_SIZE * limbCount),
				  _arrayPool.Rent(blockSize.NumberOfCells * VALUE_SIZE * limbCount),
				  new byte[blockSize.NumberOfCells * VALUE_SIZE],
				  new bool[blockSize.Height]
				  )
		{
			Array.Clear(Zrs);
			Array.Clear(Zis);
		}

		public MapSectionZVectors(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis, byte[] hasEscapedFlags, bool[] rowHasEscaped)
		{
			BlockSize = blockSize;
			_limbCount = limbCount;

			ValueCount = blockSize.NumberOfCells;
			Lanes = Vector256<uint>.Count;
			ValuesPerRow = blockSize.Width;
			RowCount = blockSize.Height;

			VectorsPerRow = ValuesPerRow / Lanes;
			BytesPerRow = ValuesPerRow * VALUE_SIZE;
			TotalBytesForFlags = ValueCount * VALUE_SIZE;

			VectorsPerZValueRow = VectorsPerRow * limbCount; // ValuesPerRow * LimbCount / Lanes;
			BytesPerZValueRow = BytesPerRow * limbCount; // ValuesPerRow * LimbCount * VALUE_SIZE;
			TotalByteCount = TotalBytesForFlags * limbCount; // ValueCount * LimbCount * VALUE_SIZE;

			Zrs = zrs;
			Zis = zis;

			HasEscapedFlags = hasEscapedFlags;
			RowHasEscaped = rowHasEscaped;
		}

		#endregion

		#region Public Properties

		private int _limbCount;
		public int LimbCount
		{
			get => _limbCount;
			set
			{
				if (value != _limbCount)
				{
					_limbCount = value;

					VectorsPerZValueRow = ValuesPerRow * value / Lanes;
					BytesPerZValueRow = ValuesPerRow * value * VALUE_SIZE;
					TotalByteCount = ValueCount * value * VALUE_SIZE;

					_arrayPool.Return(Zrs);
					_arrayPool.Return(Zis);

					Zrs = _arrayPool.Rent(TotalByteCount);
					Zis = _arrayPool.Rent(TotalByteCount);

					Array.Clear(Zrs);
					Array.Clear(Zis);
				}
			}
		}

		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; init; }

		public bool[] RowHasEscaped { get; init; }

		// ---- Supporting Properties ------ //

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int Lanes { get; init; }
		public int ValuesPerRow { get; init; }
		public int RowCount { get; init; }
		public int VectorsPerZValueRow { get; private set; }	
		public int BytesPerZValueRow { get; private set; }
		public int TotalByteCount { get; private set; }

		public int BytesPerRow { get; init; }
		public int TotalBytesForFlags { get; init; }
		public int VectorsPerRow { get; init; }
		
		#endregion

		#region Block Level Methods

		public void Load(byte[] zrs, byte[] zis, byte[] hasEscapedFlags, byte[] rowHasEscaped)
		{
			if (zrs.Length != TotalByteCount)
			{
				throw new ArgumentException($"MapSectionZVectors has a limbcount of {LimbCount}, but is reciving a zrs with length: {zrs.Length}.");
			}

			Array.Copy(zrs, Zrs, TotalByteCount);
			Array.Copy(zis, Zis, TotalByteCount);
			Array.Copy(hasEscapedFlags, HasEscapedFlags, TotalBytesForFlags);

			FillRowHasEscaped(rowHasEscaped, RowHasEscaped);
		}

		#endregion

		#region Row Level Methods

		public void FillZrsRow(int rowNumber, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerZValueRow * rowNumber;

			for (var i = 0; i < BytesPerZValueRow; i++)
			{
				destBack[i] = Zrs[startIndex + i];
			}
		}

		public void FillZisRow(int rowNumber, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerZValueRow * rowNumber;

			for (var i = 0; i < BytesPerZValueRow; i++)
			{
				destBack[i] = Zis[startIndex + i];
			}
		}

		public void FillHasEscapedFlagsRow(int rowNumber, Vector256<int>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<int>, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				destBack[i] = HasEscapedFlags[startIndex + i];
			}
		}

		// Fill an Array of Ints
		public void FillHasEscapedFlagsRow(int rowNumber, int[] dest)
		{
			var destBack = MemoryMarshal.Cast<int, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				destBack[i] = HasEscapedFlags[startIndex + i];
			}
		}

		public void UpdateFromZrsRow(int rowNumber, Vector256<uint>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(source);

			var startIndex = BytesPerZValueRow * rowNumber;

			for (var i = 0; i < BytesPerZValueRow; i++)
			{
				Zrs[startIndex + i] = sourceBack[i];
			}
		}

		public void UpdateFromZisRow(int rowNumber, Vector256<uint>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(source);

			var startIndex = BytesPerZValueRow * rowNumber;

			for (var i = 0; i < BytesPerZValueRow; i++)
			{
				Zis[startIndex + i] = sourceBack[i];
			}
		}

		public void UpdateFromHasEscapedFlagsRow(int rowNumber, Vector256<int>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<int>, byte>(source);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				HasEscapedFlags[startIndex + i] = sourceBack[i];
			}
		}

		// From an Array of Ints
		public void UpdateFromHasEscapedFlagsRow(int rowNumber, int[] source)
		{
			var sourceBack = MemoryMarshal.Cast<int, byte>(source);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				HasEscapedFlags[startIndex + i] = sourceBack[i];
			}
		}

		public byte[] GetBytesForRowHasEscaped()
		{
			var result = new byte[RowHasEscaped.Length];

			for (var i = 0; i < RowCount; i++)
			{
				result[i] = RowHasEscaped[i] ? (byte)1 : (byte)0;
			}

			return result;
		}

		public void FillRowHasEscaped(byte[] source, bool[] dest)
		{
			for (var i = 0; i < RowCount; i++)
			{
				dest[i] = source[i] == 1;
			}
		}

		private bool[] CompressHasEscapedFlags(int[] hasEscapedFlags)
		{
			bool[] result;

			if (!hasEscapedFlags.Any(x => !(x == 0)))
			{
				// All have escaped
				result = new bool[] { true };
			}
			else if (!hasEscapedFlags.Any(x => x > 0))
			{
				// none have escaped
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = hasEscapedFlags.Select(x => x == 0 ? false : true).ToArray();
			}

			return result;
		}

		#endregion

		#region Limb Set Methods

		public void FillZrsLimbSet(int rowNumber, int vecPtr, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerZValueRow * rowNumber;
			startIndex += 32  * LimbCount * vecPtr;

			for (var i = 0; i < destBack.Length; i++)
			{
				destBack[i] = Zrs[startIndex + i];
			}
		}

		public void FillZisLimbSet(int rowNumber, int vecPtr, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerZValueRow * rowNumber;
			startIndex += 32 * LimbCount * vecPtr;

			for (var i = 0; i < destBack.Length; i++)
			{
				destBack[i] = Zis[startIndex + i];
			}
		}

		public void UpdateFromZrsLimbSet(int rowNumber, int vecPtr, Vector256<uint>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(source);

			var startIndex = BytesPerZValueRow * rowNumber;
			startIndex += 32 * LimbCount * vecPtr;

			for (var i = 0; i < sourceBack.Length; i++)
			{
				Zrs[startIndex + i] = sourceBack[i];
			}
		}

		public void UpdateFromZisLimbSet(int rowNumber, int vecPtr, Vector256<uint>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(source);

			var startIndex = BytesPerZValueRow * rowNumber;
			startIndex += 32 * LimbCount * vecPtr;

			for (var i = 0; i < sourceBack.Length; i++)
			{
				Zis[startIndex + i] = sourceBack[i];
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
			Array.Clear(Zrs);
			Array.Clear(Zis);
			Array.Clear(HasEscapedFlags);
			Array.Clear(RowHasEscaped);
		}

		//void IPoolable.CopyTo(object obj)
		//{
		//	if (obj != null && obj is MapSectionZVectors destination)
		//	{
		//		CopyTo(destination);
		//	}
		//	else
		//	{
		//		throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionZVectors)}");
		//	}
		//}

		public void CopyTo(MapSectionZVectors destination)
		{
			var result = destination;

			Array.Copy(Zrs, result.Zrs, TotalByteCount);
			Array.Copy(Zis, result.Zis, TotalByteCount);
			Array.Copy(HasEscapedFlags, result.HasEscapedFlags, TotalBytesForFlags);
			Array.Copy(RowHasEscaped, result.RowHasEscaped, RowCount);
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

					//_arrayPool.Return(Zrs, clearArray: true);
					//_arrayPool.Return(Zis, clearArray: true);

					_arrayPool.Return(Zrs);
					_arrayPool.Return(Zis);
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

