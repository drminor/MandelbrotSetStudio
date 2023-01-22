﻿
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionVectors : IPoolable
	{
		#region Constructor

		public MapSectionVectors(SizeInt blockSize)
		{
			BlockSize = blockSize;
			Length = blockSize.NumberOfCells;

			HasEscapedVectors = new Vector256<int>[TotalVectorCount];
			HasEscapedMems = new Memory<Vector256<int>>(HasEscapedVectors);

			CountVectors = new Vector256<int>[TotalVectorCount];
			CountMems = new Memory<Vector256<int>>(CountVectors);

			EscapeVelocityVectors = new Vector256<int>[TotalVectorCount];
			EscapeVelocitiyMems = new Memory<Vector256<int>>(EscapeVelocityVectors);
		}

		#endregion

		#region Public Properties

		public int Lanes => Vector256<int>.Count;
		public int Length { get; init; }
		public int TotalVectorCount => Length / Lanes;

		public SizeInt BlockSize { get; init; }
		public int VectorsPerRow => BlockSize.Width / Lanes;


		public Vector256<int>[] HasEscapedVectors;
		public Memory<Vector256<int>> HasEscapedMems;

		public Vector256<int>[] CountVectors;
		public Memory<Vector256<int>> CountMems;

		public Vector256<int>[] EscapeVelocityVectors;
		public Memory<Vector256<int>> EscapeVelocitiyMems;

		#endregion

		#region Methods

		public void LoadValuesInto(MapSectionValues mapSectionValues)
		{
			var hefs = MemoryMarshal.Cast<Vector256<int>, int>(HasEscapedVectors);
			var hasEscapedFlags = new bool[Length];

			var counts = MemoryMarshal.Cast<Vector256<int>, int>(CountVectors);
			var shortCounts = new ushort[Length];

			var ecvs = MemoryMarshal.Cast<Vector256<int>, int>(EscapeVelocityVectors);
			var shortEscVels = new ushort[Length];

			for (var i = 0; i < Length; i++)
			{
				hasEscapedFlags[i] = hefs[i] != 0;
				shortCounts[i] = (ushort)counts[i];
				shortEscVels[i] = (ushort)ecvs[i];
			}

			//var shortCounts = counts.Select(x => (ushort)x).ToArray();
			//var shortEscVels = escapeVelocities.Select(x => (ushort)x).ToArray();
			//var hasEscapedFlaggs = hasEscapedFlags.Select(x => x == 1).ToArray();

			mapSectionValues.Load(hasEscapedFlags, shortCounts, shortEscVels);
		}

		public Span<Vector256<int>> GetHasEscapedFlagsRow(int start, int length)
		{
			var result = new Span<Vector256<int>>(HasEscapedVectors, start, length);
			return result;
		}

		public Span<Vector256<int>> GetCountsRow(int start, int length)
		{
			var result = new Span<Vector256<int>>(CountVectors, start, length);
			return result;
		}

		public Span<Vector256<int>> GetEscapeVelocitiesRow(int start, int length)
		{
			var result = new Span<Vector256<int>>(EscapeVelocityVectors, start, length);
			return result;
		}

		// IPoolable Support
		void IPoolable.ResetObject()
		{
			Array.Clear(HasEscapedVectors, 0, TotalVectorCount);
			Array.Clear(CountVectors, 0, TotalVectorCount);
			Array.Clear(EscapeVelocityVectors, 0, TotalVectorCount);
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

		object IPoolable.DuplicateFrom(object obj)
		{
			if (obj != null && obj is MapSectionVectors msv)
			{
				return DuplicateFrom(msv);
			}
			else
			{
				throw new ArgumentException($"DuplicateFrom required an object of type {nameof(MapSectionVectors)}");
			}
		}

		MapSectionVectors DuplicateFrom(MapSectionVectors mapSectionVectors)
		{
			var result = mapSectionVectors;

			HasEscapedMems.CopyTo(result.HasEscapedMems);
			CountMems.CopyTo(result.CountMems);
			EscapeVelocitiyMems.CopyTo(result.EscapeVelocitiyMems);

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

