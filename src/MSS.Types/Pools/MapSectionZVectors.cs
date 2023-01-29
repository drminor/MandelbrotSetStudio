
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionZVectors : IPoolable
	{
		#region Constructor

		public MapSectionZVectors(SizeInt blockSize, int limbCount)
			: this(
				  blockSize,
				  limbCount,
				  new byte[blockSize.NumberOfCells * limbCount * 4],
				  new byte[blockSize.NumberOfCells * limbCount * 4],
				  new byte[blockSize.NumberOfCells]
				  )
		{ }

		public MapSectionZVectors(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis, byte[] hasEscapedFlags)
		{
			BlockSize = blockSize;
			ValueCount = blockSize.NumberOfCells;
			LimbCount = limbCount;
			Lanes = Vector256<uint>.Count;
			ValuesPerRow = blockSize.Width;

			BytesPerRow = ValuesPerRow * LimbCount * 4;
			TotalByteCount = ValueCount * LimbCount * 4;

			Debug.Assert(zrs.Length == TotalByteCount, $"The length of zrs does not equal the {ValueCount} * {LimbCount} * 4 (values/block) * (limbs/value) x bytes/value).");
			Debug.Assert(zis.Length == TotalByteCount, $"The length of zis does not equal the {ValueCount} * {LimbCount} * 4 (values/block) * (limbs/value) x bytes/value).");

			Zrs = zrs;
			Zis = zis;
			HasEscapedFlags = hasEscapedFlags;
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

		//public int TotalVectorCount => (ValueCount * LimbCount) / Lanes;
		//public int VectorsPerRow => ValuesPerRow / Lanes;

		public byte[] Zrs { get; private set; }

		public byte[] Zis { get; private set; }

		public byte[] HasEscapedFlags { get; set; }

		#endregion

		#region Methods

		public void FillRRow(Vector256<uint>[] mantissas, int rowNumber)
		{
			var sourceStartIndex = BytesPerRow * rowNumber;
			var source = new Span<byte>(Zrs, sourceStartIndex, BytesPerRow);

			Span<byte> destinationByteSpan = MemoryMarshal.Cast<Vector256<uint>, byte>(mantissas);
			source.CopyTo(destinationByteSpan);
		}

		public void FillIRow(Vector256<uint>[] mantissas, int rowNumber)
		{
			var sourceStartIndex = BytesPerRow * rowNumber;
			var source = new Span<byte>(Zis, sourceStartIndex, BytesPerRow);

			Span<byte> destinationByteSpan = MemoryMarshal.Cast<Vector256<uint>, byte>(mantissas);
			source.CopyTo(destinationByteSpan);
		}

		public void UpdateRRowFrom(Vector256<uint>[] mantissas, int rowNumber)
		{
			var source = MemoryMarshal.Cast<Vector256<uint>, byte>(mantissas).ToArray();
			var destinationStartIndex = BytesPerRow * rowNumber;
			Array.Copy(source, 0, Zrs, destinationStartIndex, BytesPerRow);
		}

		public void UpdateIRowFrom(Vector256<uint>[] mantissas, int rowNumber)
		{
			var source = MemoryMarshal.Cast<Vector256<uint>, byte>(mantissas).ToArray();
			var destinationStartIndex = BytesPerRow * rowNumber;
			Array.Copy(source, 0, Zis, destinationStartIndex, BytesPerRow);
		}


		//public Span<Vector256<int>> GetHasEscapedFlagsRow(int start, int length)
		//{
		//	var result = new Span<Vector256<int>>(HasEscapedVectors, start, length);
		//	return result;
		//}

		//public Span<Vector256<int>> GetCountsRow(int start, int length)
		//{
		//	var result = new Span<Vector256<int>>(CountVectors, start, length);
		//	return result;
		//}

		//public Span<Vector256<int>> GetEscapeVelocitiesRow(int start, int length)
		//{
		//	var result = new Span<Vector256<int>>(EscapeVelocityVectors, start, length);
		//	return result;
		//}

		// IPoolable Support
		void IPoolable.ResetObject()
		{
			Array.Clear(Zrs, 0, TotalByteCount);
			Array.Clear(Zis, 0, TotalByteCount);
		}

		//// ICloneable Support

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		//public MapSectionVectors Clone()
		//{
		//	var result = new MapSectionVectors(BlockSize);
		//	return result;
		//}

		void IPoolable.CopyTo(object obj)
		{
			if (obj != null && obj is MapSectionZVectors mszv)
			{
				CopyTo(mszv);
			}
			else
			{
				throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionVectors)}");
			}
		}

		public void CopyTo(MapSectionZVectors mapSectionZVectors)
		{
			var result = mapSectionZVectors;

			Array.Copy(Zrs, result.Zrs, Zrs.Length);
			Array.Copy(Zis, result.Zis, Zis.Length);
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

