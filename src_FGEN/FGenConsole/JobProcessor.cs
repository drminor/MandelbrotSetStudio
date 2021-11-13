using qdDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FGenConsole
{
	class JobProcessor : IDisposable
	{
		private FGenerator _fGenerator { get; set; }
		private readonly CancellationTokenSource _cts;

		public JobProcessor(FGenJob fGenJob)
		{
			_fGenerator = new FGenerator(fGenJob);
			_cts = new CancellationTokenSource();
		}

		public FGenJob FGenJob => _fGenerator.Job;

		public IEnumerable<float[]> GenerateByLineF()
		{
			int h = _fGenerator.Job.Area.H();
			for (int j = 0; j < h; j++)
			{
				if (_cts.Token.IsCancellationRequested)
				{
					break;
				}

				float[] counts = _fGenerator.GetXCountsF(j);
				Debug.WriteLine($"Returning line: {j} with {counts.Length} result counts for Job {FGenJob.JobId}.");

				yield return counts;
			}
		}

		public IEnumerable<uint[]> GenerateByLine()
		{
			int h = _fGenerator.Job.Area.H();
			for (int j = 0; j < h; j++)
			{
				if (_cts.Token.IsCancellationRequested)
				{
					break;
				}

				uint[] counts = _fGenerator.GetXCounts(j);
				Debug.WriteLine($"Returning line: {j} with {counts.Length} result counts for Job {FGenJob.JobId}.");

				//int[] iCounts = new int[counts.Length];
				//for(int ptr = 0; ptr < counts.Length; ptr++)
				//{
				//	iCounts[ptr] = counts[ptr];
				//}

				yield return counts;
			}
		}

		public void Stop()
		{
			_cts.Cancel();
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
