using MongoDB.Bson;
using MSS.Common;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MapSectionProviderLib
{
	// TODO: Consider deleting the MapLoaderManager class after moving its logic to the MapSectionRequestProcessor
	// The RequestAdded event would then be raised by the caller of the Push method for subscribers interested in only that 'clients' jobs.
	// The MapSectionRequestProcess could also raise a RequestAdded event for subscribers interested in all jobs.
	
	public class MapLoaderManager : IMapLoaderManager, IDisposable
	{
		#region Private Fields

		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		#endregion

		#region Constructor

		public MapLoaderManager(MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			//_cts = new CancellationTokenSource();
			_mapSectionBuilder = new MapSectionBuilder();
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
		}

		#endregion

		#region Public Events

		public event EventHandler<MsrJob>? RequestAdded;

		#endregion

		#region Public Methods

		public MsrJob CreateMapSectionRequestJob(JobType jobType, ObjectId jobId, OwnerType jobOwnerType, MapPositionSizeAndDelta mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var mapLoaderJobNumber = GetNextJobNumber();
			var msrJob = _mapSectionBuilder.CreateMapSectionRequestJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo, mapCalcSettings);

			return msrJob;
		}

		public MsrJob CreateNewCopy(MsrJob s)
		{
			var mapLoaderJobNumber = GetNextJobNumber();
			var result = _mapSectionBuilder.CreateNewCopy(s, mapLoaderJobNumber);

			return result;
		}

		public List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback, CancellationToken ct, out List<MapSectionRequest> requestsPendingGeneration)
		{
			var totalSectionsRequested = _mapSectionBuilder.GetNumberOfRequests(mapSectionRequests);
			var sectionsCancelled = _mapSectionBuilder.GetNumberOfSectionsCancelled(mapSectionRequests);
			msrJob.Start(totalSectionsRequested, sectionsCancelled, mapSectionReadyCallback, mapViewUpdateCompleteCallback);

			List<MapSection> mapSections = _mapSectionRequestProcessor.SubmitRequests(msrJob, mapSectionRequests, msrJob.HandleResponse, ct, out requestsPendingGeneration);

			CheckPendingGenerationCount(msrJob, requestsPendingGeneration);

			RequestAdded?.Invoke(this, msrJob);

			return mapSections;
		}

		#endregion

		#region Private Methods

		private int GetNextJobNumber() => _mapSectionRequestProcessor.GetNextJobNumber();

		[Conditional("DEBUG2")]
		private void CheckPendingGenerationCount(MsrJob msrJob, List<MapSectionRequest> pendingGeneration)
		{
			var sectionsPendingGeneration = _mapSectionBuilder.GetNumberOfRequests(pendingGeneration);
			if (msrJob.SectionsPending != sectionsPendingGeneration)
			{
				Debug.WriteLine($"The MapSectionRequestProcessor ({sectionsPendingGeneration}) and the MsrJob {msrJob.SectionsPending} disagree on the number of MapSections pending.");
			}
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
					_mapSectionRequestProcessor.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
