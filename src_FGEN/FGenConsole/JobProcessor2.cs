using qdDotNet;
using System;
using System.Threading;

namespace FGenConsole
{
	class JobProcessor2 : IDisposable
	{
		private FGenerator _fGenerator { get; set; }
		private readonly CancellationTokenSource _cts;

		public JobProcessor2(FGenJob fGenJob, int instanceNum)
		{
			_fGenerator = new FGenerator(fGenJob);
			_cts = new CancellationTokenSource();
			InstanceNum = instanceNum;
		}

		public FGenJob FGenJob => _fGenerator.Job;

		public int InstanceNum { get; }

		//public uint[] GetCountsForLine(int linePtr)
		//{
		//	uint[] result = _fGenerator.GetXCounts(linePtr);
		//	return result;
		//}

		public void FillCountsForBlock(PointInt position, SubJobResult subJobResult)
		{
			uint[] counts = subJobResult.Counts;
			bool[] doneFlags = subJobResult.DoneFlags;
			double[] zValues = subJobResult.ZValues;

			_fGenerator.FillXCounts(position, ref counts, ref doneFlags, ref zValues);

			subJobResult.Counts = counts;
			subJobResult.DoneFlags = doneFlags;
			subJobResult.ZValues = zValues;
		}

		private SubJobResult _emptySubJobResult = null;

		public SubJobResult GetEmptySubJobResult()
		{
			if(_emptySubJobResult == null)
			{
				int size = FGenerator.BLOCK_WIDTH * FGenerator.BLOCK_HEIGHT;
				uint iterationCount = 0;
				_emptySubJobResult = SubJobResult.GetEmptySubJobResult(size, iterationCount, InstanceNum, true);
			}

			SubJobResult.ClearSubJobResult(_emptySubJobResult);
			return _emptySubJobResult;
		}



		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects).
					//_fGenerator.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~JobProcessor() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion

	}
}
