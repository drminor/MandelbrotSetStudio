using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MapSectionProviderLib
{
	public class MapLoaderManager : IMapLoaderManager
	{
		#region Private Fields

		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private const int PRECSION_PADDING = 4;
		private const int MIN_LIMB_COUNT = 1;

		private int _currentPrecision;
		private int _currentLimbCount;

		private bool _useDetailedDebug = false;

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

		public MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, MapPositionSizeAndDelta mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var msrJob = CreateMapSectionRequestJob(jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(),
				mapAreaInfo.MapBlockOffset, mapAreaInfo.Precision, mapAreaInfo.Coords.CrossesYZero, mapCalcSettings);

			return msrJob;
		}

		public MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, Subdivision subdivision, string originalSourceSubdivisionId,
			VectorLong mapBlockOffset, int precision, bool crossesYZero, MapCalcSettings mapCalcSettings)
		{
			var limbCount = GetLimbCount(precision);
			var mapLoaderJobNumber = GetNextJobNumber();
			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, subdivision, originalSourceSubdivisionId, mapBlockOffset,	precision, limbCount, mapCalcSettings, crossesYZero);

			return msrJob;
		}

		public MsrJob CreateNewCopy(MsrJob s)
		{
			var result = new MsrJob
				(
					mapLoaderJobNumber: GetNextJobNumber(),
					jobType: s.JobType,
					jobId: s.JobId,
					ownerType: s.OwnerType,
					subdivision: s.Subdivision,
					originalSourceSubdivisionId: s.OriginalSourceSubdivisionId,
					jobBlockOffset: s.JobBlockOffset,
					precision: s.Precision,
					limbCount: s.LimbCount,
					mapCalcSettings: s.MapCalcSettings,
					crossesYZero: s.CrossesYZero
				);

			return result;
		}

		public List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback, CancellationToken ct, out List<MapSectionRequest> requestsPendingGeneration)
		{
			var totalSectionsRequested = _mapSectionBuilder.GetTotalNumberOfRequests(mapSectionRequests);
			var sectionsCancelled = _mapSectionBuilder.GetNumberOfSectionsCancelled(mapSectionRequests);
			msrJob.Start(totalSectionsRequested, sectionsCancelled, mapSectionReadyCallback, mapViewUpdateCompleteCallback);

			List<MapSection> mapSections = _mapSectionRequestProcessor.SubmitRequests(msrJob, mapSectionRequests, msrJob.HandleResponse, ct, out requestsPendingGeneration);

			msrJob.SectionsFoundInRepo = mapSections.Count;
			CheckPendingGenerationCount(msrJob, requestsPendingGeneration);

			var mapLoaderJobNumber = msrJob.MapLoaderJobNumber;
			RequestAdded?.Invoke(this, msrJob);

			return mapSections;
		}

		#endregion

		#region Private Methods

		private int GetNextJobNumber() => _mapSectionRequestProcessor.GetNextJobNumber();

		private int GetLimbCount(int precision)
		{
			if (precision != _currentPrecision)
			{
				var adjustedPrecision = precision + PRECSION_PADDING;
				var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: adjustedPrecision);

				var adjustedLimbCount = Math.Max(apFixedPointFormat.LimbCount, MIN_LIMB_COUNT);

				if (_currentLimbCount == adjustedLimbCount)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount remains the same at {adjustedLimbCount}.");
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount is being updated to {adjustedLimbCount}.");
				}

				_currentLimbCount = adjustedLimbCount;
				_currentPrecision = precision;
			}

			return _currentLimbCount;
		}

		[Conditional("DEBUG")]
		private void CheckPendingGenerationCount(MsrJob msrJob, List<MapSectionRequest> pendingGeneration)
		{
			var sectionsPendingGeneration = _mapSectionBuilder.GetTotalNumberOfRequests(pendingGeneration);
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
