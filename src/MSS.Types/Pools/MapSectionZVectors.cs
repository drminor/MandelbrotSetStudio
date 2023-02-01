﻿using System;
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
				  new byte[blockSize.NumberOfCells * VALUE_SIZE]
				  )
		{ }

		public MapSectionZVectors(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis, byte[] hasEscapedFlags)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			LimbCount = limbCount;
			Lanes = Vector256<uint>.Count;
			ValuesPerRow = blockSize.Width;

			BytesPerRow = ValuesPerRow * LimbCount * VALUE_SIZE;
			TotalByteCount = ValueCount * LimbCount * VALUE_SIZE;

			Debug.Assert(zrs.Length == TotalByteCount, $"The length of zrs does not equal the {ValueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");
			Debug.Assert(zis.Length == TotalByteCount, $"The length of zis does not equal the {ValueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");

			BytesPerFlagRow = ValuesPerRow * VALUE_SIZE;
			TotalBytesForFlags = ValueCount * VALUE_SIZE;

			Debug.Assert(hasEscapedFlags.Length == TotalBytesForFlags, $"The length of hasEscapedFlags does not equal the {ValueCount} * {VALUE_SIZE} (values/block * bytes/value) .");

			Zrs = zrs;
			Zis = zis;
			HasEscapedFlags = hasEscapedFlags;

			ZrsMemory = new Memory<byte>(Zrs);
			ZisMemory = new Memory<byte>(Zis);
			HasEscapedFlagsMemory = new Memory<byte>(HasEscapedFlags);
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int Lanes { get; init; }
		public int ValuesPerRow { get; init; }
		public int BytesPerRow { get; init; }
		public int TotalByteCount { get; init; }

		public int BytesPerFlagRow { get; init; }
		public int TotalBytesForFlags { get; init; }

		public byte[] Zrs { get; init; }
		public byte[] Zis { get; init; }
		public byte[] HasEscapedFlags { get; init; }

		public Memory<byte> ZrsMemory { get; init; }
		public Memory<byte> ZisMemory { get; init; }
		public Memory<byte> HasEscapedFlagsMemory { get; init; }

		#endregion

		#region ZValue Methods

		public void Load(byte[] zrs, byte[] zis, byte[] hasEscapedFlags)
		{
			Array.Copy(zrs, Zrs, TotalByteCount);
			Array.Copy(zis, Zis, TotalByteCount);
			Array.Copy(hasEscapedFlags, HasEscapedFlags, ValueCount);
		}

		public Span<Vector256<uint>> GetZrsRow(int rowNumber)
		{
			var result = MemoryMarshal.Cast<byte, Vector256<uint>>(ZrsMemory.Slice(BytesPerRow * rowNumber, BytesPerRow).Span);
			return result;
		}

		public Span<Vector256<uint>> GetZisRow(int rowNumber)
		{
			var result = MemoryMarshal.Cast<byte, Vector256<uint>>(ZisMemory.Slice(BytesPerRow * rowNumber, BytesPerRow).Span);
			return result;
		}

		public Span<Vector256<int>> GetHasEscapedFlagsRow(int rowNumber)
		{
			var result = MemoryMarshal.Cast<byte, Vector256<int>>(HasEscapedFlagsMemory.Slice(BytesPerFlagRow * rowNumber, BytesPerFlagRow).Span);
			return result;
		}

		#endregion

		#region HasEscapedFlag Methods

		// IPoolable Support
		void IPoolable.ResetObject()
		{
			Array.Clear(Zrs, 0, TotalByteCount);	// TODO: Use Zrs Memory
			Array.Clear(Zis, 0, TotalByteCount);
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
			Array.Copy(HasEscapedFlags, result.HasEscapedFlags, ValueCount);
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

