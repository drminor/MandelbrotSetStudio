using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types
{
	public class MapSectionValues : IPoolable
	{
		#region Constructor

		public MapSectionValues(SizeInt blockSize)
		{
			BlockSize = blockSize;
			Length = blockSize.NumberOfCells;

			HasEscapedFlags = new bool[Length];
			Counts = new ushort[Length];
			EscapeVelocities = new ushort[Length];
		}

		//public MapSectionValues(bool[] hasEscapedFlags, ushort[] counts, ushort[] escapeVelocities)
		//{
		//	HasEscapedFlags = hasEscapedFlags ?? throw new ArgumentNullException(nameof(hasEscapedFlags));
		//	Counts = counts ?? throw new ArgumentNullException(nameof(counts));
		//	EscapeVelocities = escapeVelocities ?? throw new ArgumentNullException(nameof(escapeVelocities));
		//}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int Length { get; init; }

		public bool[] HasEscapedFlags { get; private set;}
		public ushort[] Counts { get; private set; }
		public ushort[] EscapeVelocities { get; private set; }

		#endregion

		#region Methods

		//public void Load(bool[] hasEscapedFlags, ushort[] counts, ushort[] escapeVelocities)
		//{
		//	CheckArguments(hasEscapedFlags, counts, escapeVelocities);

		//	Array.Copy(hasEscapedFlags, HasEscapedFlags, Length);
		//	Array.Copy(counts, Counts, Length);
		//	Array.Copy(escapeVelocities, EscapeVelocities, Length);
		//}

		//public void Load(bool[] hasEscapedFlags, int[] counts, int[] escapeVelocities)
		//{
		//	CheckArguments(hasEscapedFlags, counts, escapeVelocities);

		//	Array.Copy(hasEscapedFlags, HasEscapedFlags, Length);
		//	Array.Copy(counts.Select(x => (ushort)x).ToArray(), Counts, Length);
		//	Array.Copy(escapeVelocities.Select(x => (ushort)x).ToArray(), EscapeVelocities, Length);
		//}

		public void Load(MapSectionVectors mapSectionVectors)
		{
			var hefs = MemoryMarshal.Cast<Vector256<int>, int>(mapSectionVectors.HasEscapedVectors);
			var counts = MemoryMarshal.Cast<Vector256<int>, int>(mapSectionVectors.CountVectors);
			var ecvs = MemoryMarshal.Cast<Vector256<int>, int>(mapSectionVectors.EscapeVelocityVectors);

			for (var i = 0; i < Length; i++)
			{
				HasEscapedFlags[i] = hefs[i] != 0;
				Counts[i] = (ushort)counts[i];
				EscapeVelocities[i] = (ushort)ecvs[i];
			}
		}

		private void CheckArguments<T>(bool[] hasEscapedFlags, T[] counts, T[] escapeVelocities)
		{
			if (hasEscapedFlags == null || hasEscapedFlags.Length != BlockSize.NumberOfCells)
			{
				throw new ArgumentException($"The {nameof(hasEscapedFlags)} has a length different than {Length}.");
			}

			if (counts == null || counts.Length != BlockSize.NumberOfCells)
			{
				throw new ArgumentException($"The {nameof(counts)} has a length different than {Length}.");
			}

			if (escapeVelocities == null || escapeVelocities.Length != BlockSize.NumberOfCells)
			{
				throw new ArgumentException($"The {nameof(escapeVelocities)} has a length different than {Length}.");
			}
		}

		#region IPoolable Support

		void IPoolable.ResetObject()
		{
			Array.Clear(HasEscapedFlags, 0, Length);
			Array.Clear(Counts, 0, Length);
			Array.Clear(EscapeVelocities, 0, Length);
		}

		void IPoolable.CopyTo(object obj)
		{
			if (obj != null && obj is MapSectionValues msv)
			{
				CopyTo(msv);
			}
			else
			{
				throw new ArgumentException($"CopyTo required an object of type {nameof(MapSectionValues)}");
			}
		}

		public void CopyTo(MapSectionValues mapSectionValues)
		{
			Array.Copy(HasEscapedFlags, mapSectionValues.HasEscapedFlags, Length);
			Array.Copy(Counts, mapSectionValues.Counts, Length);
			Array.Copy(EscapeVelocities, mapSectionValues.EscapeVelocities, Length);
		}

		#endregion

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
					HasEscapedFlags = Array.Empty<bool>();
					Counts = Array.Empty<ushort>();
					EscapeVelocities = Array.Empty<ushort>();
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
