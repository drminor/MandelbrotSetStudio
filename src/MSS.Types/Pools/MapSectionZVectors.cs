using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
		//	: this(
		//		  blockSize,
		//		  limbCount,
		//		  zrs: new byte[blockSize.NumberOfCells * limbCount * VALUE_SIZE],
		//		  zis: new byte[blockSize.NumberOfCells * limbCount * VALUE_SIZE],
		//		  hasEscapedFlags: new byte[blockSize.NumberOfCells * VALUE_SIZE],
		//		  rowHasEscaped: new byte[blockSize.Height * VALUE_SIZE]
		//		  )
		//{ }

		public MapSectionZVectors(SizeInt blockSize, int limbCount)
		{
			BlockSize = blockSize;
			_limbCount = limbCount;

			ValueCount = blockSize.NumberOfCells;
			Lanes = Vector256<uint>.Count;
			ValuesPerRow = blockSize.Width;
			RowCount = blockSize.Height;

			VectorsPerRow = ValuesPerRow * LimbCount / Lanes;
			BytesPerRow = ValuesPerRow * LimbCount * VALUE_SIZE;
			TotalByteCount = ValueCount * LimbCount * VALUE_SIZE;

			BytesPerFlagRow = ValuesPerRow * VALUE_SIZE;
			TotalBytesForFlags = ValueCount * VALUE_SIZE;
			VectorsPerFlagRow = ValuesPerRow / Lanes;

			Zrs = _arrayPool.Rent(TotalByteCount);
			Zis = _arrayPool.Rent(TotalByteCount);

			HasEscapedFlags = new byte[TotalBytesForFlags];
			RowHasEscaped = new byte[RowCount * VALUE_SIZE];

			RowHasEscapedMemory = new Memory<byte>(RowHasEscaped);

			//if (Zrs.Length != TotalByteCount || TotalByteCount != ( 128 * 128 * 4 * 3)) 
			//{
			//	Debug.WriteLine("Hi22");
			//}
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

					VectorsPerRow = ValuesPerRow * value / Lanes;
					BytesPerRow = ValuesPerRow * value * VALUE_SIZE;
					TotalByteCount = ValueCount * value * VALUE_SIZE;

					_arrayPool.Return(Zrs, clearArray: true);
					_arrayPool.Return(Zis, clearArray: true);

					Zrs = _arrayPool.Rent(TotalByteCount);
					Zis = _arrayPool.Rent(TotalByteCount);
				}
			}
		}

		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; init; }
		public byte[] RowHasEscaped { get; set; }

		// ---- Supporting Properties ------ //

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int Lanes { get; init; }
		public int ValuesPerRow { get; init; }
		public int RowCount { get; init; }
		public int VectorsPerRow { get; private set; }	
		public int BytesPerRow { get; private set; }
		public int TotalByteCount { get; private set; }

		public int BytesPerFlagRow { get; init; }
		public int TotalBytesForFlags { get; init; }
		public int VectorsPerFlagRow { get; init; }

		public Memory<byte> RowHasEscapedMemory { get; init; }

		#endregion

		#region ZValue Methods

		public void Load(byte[] zrs, byte[] zis, byte[] hasEscapedFlags, byte[] rowHasEscaped)
		{
			Array.Copy(zrs, Zrs, TotalByteCount);
			Array.Copy(zis, Zis, TotalByteCount);
			Array.Copy(hasEscapedFlags, HasEscapedFlags, TotalBytesForFlags);
			Array.Copy(hasEscapedFlags, HasEscapedFlags, TotalBytesForFlags);
			Array.Copy(rowHasEscaped, RowHasEscaped, RowCount * VALUE_SIZE);
		}

		//public Span<Vector256<uint>> GetZrsRow(int rowNumber)
		//{
		//	var result = MemoryMarshal.Cast<byte, Vector256<uint>>(ZrsMemory.Slice(BytesPerRow * rowNumber, BytesPerRow).Span);
		//	return result;
		//}

		//public Span<Vector256<uint>> GetZisRow(int rowNumber)
		//{
		//	var result = MemoryMarshal.Cast<byte, Vector256<uint>>(ZisMemory.Slice(BytesPerRow * rowNumber, BytesPerRow).Span);
		//	return result;
		//}

		//public Span<Vector256<byte>> GetHasEscapedFlagsRow(int rowNumber)
		//{
		//	var result = MemoryMarshal.Cast<byte, Vector256<byte>>(HasEscapedFlagsMemory.Slice(BytesPerFlagRow * rowNumber, BytesPerFlagRow).Span);
		//	return result;
		//}


		public void FillZrsRow(int rowNumber, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				destBack[i] = Zrs[startIndex + i];
			}
		}

		public void FillZisRow(int rowNumber, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				destBack[i] = Zis[startIndex + i];
			}
		}

		public void FillHasEscapedFlagsRow(int rowNumber, Vector256<int>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<int>, byte>(dest);

			var startIndex = BytesPerFlagRow * rowNumber;

			for (var i = 0; i < BytesPerFlagRow; i++)
			{
				destBack[i] = HasEscapedFlags[startIndex + i];
			}
		}

		// Fill an Array of Ints
		public void FillHasEscapedFlagsRow(int rowNumber, int[] dest)
		{
			var destBack = MemoryMarshal.Cast<int, byte>(dest);

			var startIndex = BytesPerFlagRow * rowNumber;

			for (var i = 0; i < BytesPerFlagRow; i++)
			{
				destBack[i] = HasEscapedFlags[startIndex + i];
			}
		}

		public void UpdateFromZrsRow(int rowNumber, Vector256<uint>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(source);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				Zrs[startIndex + i] = sourceBack[i];
			}
		}

		public void UpdateFromZisRow(int rowNumber, Vector256<uint>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(source);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				Zis[startIndex + i] = sourceBack[i];
			}
		}

		public void UpdateFromHasEscapedFlagsRow(int rowNumber, Vector256<int>[] source)
		{
			var sourceBack = MemoryMarshal.Cast<Vector256<int>, byte>(source);

			var startIndex = BytesPerFlagRow * rowNumber;

			for (var i = 0; i < BytesPerFlagRow; i++)
			{
				HasEscapedFlags[startIndex + i] = sourceBack[i];
			}
		}

		// From an Array of Ints
		public void UpdateFromHasEscapedFlagsRow(int rowNumber, int[] source)
		{
			var sourceBack = MemoryMarshal.Cast<int, byte>(source);

			var startIndex = BytesPerFlagRow * rowNumber;

			for (var i = 0; i < BytesPerFlagRow; i++)
			{
				HasEscapedFlags[startIndex + i] = sourceBack[i];
			}
		}

		public Span<bool> GetRowHasEscaped()
		{
			var result = MemoryMarshal.Cast<byte, bool>(RowHasEscapedMemory.Span);
			return result;
		}

		#endregion

		#region IPoolable Support

		public void ResetObject()
		{
			Array.Clear(Zrs);
			Array.Clear(Zis);
			Array.Clear(HasEscapedFlags);
			Array.Clear(RowHasEscaped);
		}

		void IPoolable.CopyTo(object obj)
		{
			if (obj != null && obj is MapSectionZVectors mszv)
			{
				CopyTo(mszv);
			}
			else
			{
				throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionZVectors)}");
			}
		}

		public void CopyTo(MapSectionZVectors mapSectionZVectors)
		{
			var result = mapSectionZVectors;

			Array.Copy(Zrs, result.Zrs, TotalByteCount);
			Array.Copy(Zis, result.Zis, TotalByteCount);
			Array.Copy(HasEscapedFlags, result.HasEscapedFlags, TotalBytesForFlags);
			RowHasEscapedMemory.CopyTo(result.RowHasEscapedMemory);
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

					_arrayPool.Return(Zrs, clearArray: true);
					_arrayPool.Return(Zis, clearArray: true);
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

