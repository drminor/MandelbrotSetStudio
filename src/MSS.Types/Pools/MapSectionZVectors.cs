using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionZVectors : IPoolable
	{
		#region Constructor

		private const int VALUE_SIZE = 4;

		public MapSectionZVectors(SizeInt blockSize, int limbCount)
			: this(
				  blockSize,
				  limbCount,
				  new byte[blockSize.NumberOfCells * limbCount * VALUE_SIZE],
				  new byte[blockSize.NumberOfCells * limbCount * VALUE_SIZE],
				  new byte[blockSize.NumberOfCells * VALUE_SIZE],
				  new byte[blockSize.Height * VALUE_SIZE]
				  )
		{ }

		public MapSectionZVectors(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis, byte[] hasEscapedFlags, byte[] rowHasEscaped)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			LimbCount = limbCount;
			Lanes = Vector256<uint>.Count;
			ValuesPerRow = blockSize.Width;
			RowCount = blockSize.Height;
			VectorsPerRow = ValuesPerRow * LimbCount / Lanes;

			BytesPerRow = ValuesPerRow * LimbCount * VALUE_SIZE;
			TotalByteCount = ValueCount * LimbCount * VALUE_SIZE;

			Debug.Assert(zrs.Length == TotalByteCount, $"The length of zrs does not equal the {ValueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");
			Debug.Assert(zis.Length == TotalByteCount, $"The length of zis does not equal the {ValueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");

			BytesPerFlagRow = ValuesPerRow * VALUE_SIZE;
			TotalBytesForFlags = ValueCount * VALUE_SIZE;
			VectorsPerFlagRow = ValuesPerRow / Lanes;

			Debug.Assert(hasEscapedFlags.Length == TotalBytesForFlags, $"The length of hasEscapedFlags does not equal the {ValueCount} * {VALUE_SIZE} (values/block * bytes/value) .");

			Zrs = zrs;
			Zis = zis;
			HasEscapedFlags = hasEscapedFlags;
			RowHasEscaped = rowHasEscaped;

			//ZrsMemory = new Memory<byte>(Zrs);
			//ZisMemory = new Memory<byte>(Zis);
			//HasEscapedFlagsMemory = new Memory<byte>(HasEscapedFlags);
			RowHasEscapedMemory = new Memory<byte>(RowHasEscaped);
		}

		#endregion

		#region Public Properties

		public byte[] Zrs { get; init; }
		public byte[] Zis { get; init; }
		public byte[] HasEscapedFlags { get; init; }
		public byte[] RowHasEscaped { get; set; }

		// ---- Supporting Properties ------ //

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int Lanes { get; init; }
		public int ValuesPerRow { get; init; }
		public int RowCount { get; init; }
		public int VectorsPerRow { get; init; }	
		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public int BytesPerFlagRow { get; init; }
		public int TotalBytesForFlags { get; init; }
		public int VectorsPerFlagRow { get; init; }

		//public Memory<byte> ZrsMemory { get; init; }
		//public Memory<byte> ZisMemory { get; init; }
		//public Memory<byte> HasEscapedFlagsMemory { get; init; }
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

		public void FillHasEscapedFlagsRow(int rowNumber, Vector256<byte>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<byte>, byte>(dest);

			var startIndex = BytesPerFlagRow * rowNumber;

			for (var i = 0; i < BytesPerFlagRow; i++)
			{
				destBack[i] = HasEscapedFlags[startIndex + i];
			}
		}

		public void UpdateFromZrsRow(int rowNumber, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				Zrs[startIndex + i] = destBack[i];
			}
		}

		public void UpdateFromZisRow(int rowNumber, Vector256<uint>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(dest);

			var startIndex = BytesPerRow * rowNumber;

			for (var i = 0; i < BytesPerRow; i++)
			{
				Zis[startIndex + i] = destBack[i];
			}
		}

		public void UpdateFromHasEscapedFlagsRow(int rowNumber, Vector256<byte>[] dest)
		{
			var destBack = MemoryMarshal.Cast<Vector256<byte>, byte>(dest);

			var startIndex = BytesPerFlagRow * rowNumber;

			for (var i = 0; i < BytesPerFlagRow; i++)
			{
				HasEscapedFlags[startIndex + i] = destBack[i];
			}
		}


		public Span<bool> GetRowHasEscaped()
		{
			var result = MemoryMarshal.Cast<byte, bool>(RowHasEscapedMemory.Span);
			return result;
		}

		#endregion

		#region IPoolable Support

		void IPoolable.ResetObject()
		{
			Array.Clear(Zrs, 0, TotalByteCount);	// TODO: Use Zrs Memory
			Array.Clear(Zis, 0, TotalByteCount);
			Array.Clear(HasEscapedFlags, 0, TotalBytesForFlags);

			RowHasEscapedMemory.Span.Clear();
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

			Array.Copy(Zrs, result.Zrs, TotalByteCount);						// TODO: Use ZrsMemory
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

