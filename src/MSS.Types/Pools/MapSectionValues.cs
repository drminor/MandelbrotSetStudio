using System;
using System.Runtime.InteropServices;

namespace MSS.Types
{
	public class MapSectionValues : IPoolable
	{
		#region Constructor

		public MapSectionValues(SizeInt blockSize)
		{
			BlockSize = blockSize;
			Length = blockSize.NumberOfCells;

			Counts = new ushort[Length];
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public int Length { get; init; }

		public ushort[] Counts { get; private set; }

		#endregion

		#region Methods

		public void Load(MapSectionVectors mapSectionVectors)
		{
			var counts = MemoryMarshal.Cast<byte, int>(mapSectionVectors.Counts);

			for (var i = 0; i < Length; i++)
			{
				Counts[i] = (ushort)counts[i];
			}
		}

		#region IPoolable Support

		void IPoolable.ResetObject()
		{
			Array.Clear(Counts, 0, Length);
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
			Array.Copy(Counts, mapSectionValues.Counts, Length);
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
					//HasEscapedFlags = Array.Empty<bool>();
					Counts = Array.Empty<ushort>();
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
