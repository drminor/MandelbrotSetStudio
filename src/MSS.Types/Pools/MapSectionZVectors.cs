
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionZVectors : IPoolable
	{
		#region Constructor

		public MapSectionZVectors(SizeInt blockSize, int limbCount)
		{
			BlockSize = blockSize;
			Length = blockSize.NumberOfCells;
			LimbCount = limbCount;

			Zrs = new Vector256<uint>[TotalVectorCount];
			ZrsMems = new Memory<Vector256<uint>>(Zrs);

			Zis = new Vector256<uint>[TotalVectorCount];
			ZisMems = new Memory<Vector256<uint>>(Zrs);
		}

		#endregion

		#region Public Properties

		public int Lanes => Vector256<int>.Count;
		public int Length { get; init; }
		public int LimbCount { get; init; }
		public int TotalVectorCount => (Length * LimbCount) / Lanes;

		public SizeInt BlockSize { get; init; }
		public int VectorsPerRow => BlockSize.Width / Lanes;

		public Vector256<uint>[] Zrs;
		public Memory<Vector256<uint>> ZrsMems;

		public Vector256<uint>[] Zis;
		public Memory<Vector256<uint>> ZisMems;

		#endregion

		#region Methods

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
			Array.Clear(Zrs, 0, TotalVectorCount);
			Array.Clear(Zis, 0, TotalVectorCount);
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

		object IPoolable.CopyTo(object obj)
		{
			if (obj != null && obj is MapSectionZVectors msv)
			{
				return CopyTo(msv);
			}
			else
			{
				throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionVectors)}");
			}
		}

		MapSectionZVectors CopyTo(MapSectionZVectors mapSectionVectors)
		{
			var result = mapSectionVectors;

			Zrs.CopyTo(result.ZrsMems);
			Zis.CopyTo(result.ZisMems);

			return result;
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

