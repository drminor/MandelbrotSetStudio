using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoader
	{
		private readonly BigVector _mapBlockOffset;
		private readonly Action<MapSection, int, bool> _callback;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly MapSectionHelper _mapSectionHelper;

		private IList<MapSectionRequest>? _mapSectionRequests;
		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionsCompleted;
		private TaskCompletionSource? _tcs;

		#region Constructor

		public MapLoader(BigVector mapBlockOffset, Action<MapSection, int, bool> callback, MapSectionHelper mapSectionHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_mapBlockOffset = mapBlockOffset;
			_callback = callback;
			_mapSectionHelper = mapSectionHelper;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));
			JobNumber = _mapSectionRequestProcessor.GetNextRequestId();

			_mapSectionRequests = null;
			_isStopping = false;
			_sectionsRequested = 0;
			_sectionsCompleted = 0;
			_tcs = null;
		}

		#endregion

		#region Public Properties

		public event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		public int JobNumber { get; init; }

		#endregion

		#region Public Methods

		public Task Start(IList<MapSectionRequest> mapSectionRequests)
		{
			if (_tcs != null)
			{
				throw new InvalidOperationException("This MapLoader has already been started.");
			}

			_mapSectionRequests = mapSectionRequests;

			_ = Task.Run(SubmitSectionRequests);

			_tcs = new TaskCompletionSource();
			return _tcs.Task;
		}

		public void Stop()
		{
			if (_tcs == null)
			{
				throw new InvalidOperationException("This MapLoader has not been started.");
			}

			if (!_isStopping && _tcs.Task.Status != TaskStatus.RanToCompletion)
			{
				_mapSectionRequestProcessor.CancelJob(JobNumber);
				_isStopping = true;
			}
		}

		#endregion

		#region Private Methods

		private void SubmitSectionRequests()
		{
			if (_mapSectionRequests == null)
			{
				return;
			}

			foreach(var mapSectionRequest in _mapSectionRequests)
			{
				if (_isStopping)
				{
					if (_sectionsCompleted == _sectionsRequested && _tcs?.Task.IsCompleted == false)
					{
						Debug.WriteLine($"The MapLoader is stopping and the completed cnt = requested cnt = {_sectionsCompleted}.");
						_tcs.SetResult();
					}
					break;
				}

				//Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");

				mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
				_mapSectionRequestProcessor.AddWork(JobNumber, mapSectionRequest, HandleResponse);
				mapSectionRequest.Sent = true;

				_ = Interlocked.Increment(ref _sectionsRequested);
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse? mapSectionResponse)
		{
			var mapSectionResult = MapSection.Empty;
			bool isLastSection;

			if (mapSectionResponse == null || mapSectionResponse.IsEmpty)
			{
				Debug.WriteLine("The MapSectionResponse is empty in the HandleResponse callback for the MapLoader.");
			}

			if (mapSectionResponse != null && mapSectionResponse.Counts != null && !mapSectionResponse.RequestCancelled)
			{
				mapSectionResult = _mapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionResponse, _mapBlockOffset);

				if (mapSectionRequest.ClientEndPointAddress != null && mapSectionRequest.TimeToCompleteGenRequest != null)
				{
					//Log: MapSection BlockPosition and TimeToCompleteRequest
					//Debug.WriteLine($"MapSection for {mapSectionResult.BlockPosition}, using client: {mapSectionRequest.ClientEndPointAddress}, took: {mapSectionRequest.TimeToCompleteGenRequest.Value.TotalSeconds}.");
				}
			}
			else
			{
				Debug.WriteLine($"Cannot create a mapSectionResult from the mapSectionResponse. The request's block position is {mapSectionRequest.BlockPosition}. " +
					$"IsCancelled: {mapSectionResponse?.RequestCancelled}, HasCounts: {mapSectionResponse?.Counts != null}.");
			}

			_ = Interlocked.Increment(ref _sectionsCompleted);

			if (_sectionsCompleted >= _mapSectionRequests?.Count || (_isStopping && _sectionsCompleted >= _sectionsRequested))
			{
				isLastSection = true;

				_callback(mapSectionResult, JobNumber, isLastSection);

				if (!mapSectionResult.IsEmpty)
				{
					SectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest));
				}

				mapSectionRequest.Handled = true;

				if (_tcs?.Task.IsCompleted == false)
				{
					_tcs.SetResult();
				}

				var numberOfPendingRequests = _mapSectionRequestProcessor.GetNumberOfPendingRequests(JobNumber);
				var notHandled = _mapSectionRequests?.Count(x => !x.Handled) ?? 0;
				var notSent = _mapSectionRequests?.Count(x => !x.Sent) ?? 0;
				// Log: MapLoader is done with Job:
				//Debug.WriteLine($"MapLoader is done with Job: {JobNumber}. Completed {_sectionsCompleted} sections. There are {numberOfPendingRequests}/{notHandled}/{notSent} requests still pending, not handled, not sent.");
			}
			else
			{
				isLastSection = false;
				if (!mapSectionResult.IsEmpty)
				{
					_callback(mapSectionResult, JobNumber, isLastSection);
					SectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest));
				}
				else
				{
					Debug.WriteLine("Not calling the callback, the mapSectionResult is empty.");
				}

				mapSectionRequest.Handled = true;
			}

		}

		private MapSectionProcessInfo CreateMSProcInfo(MapSectionRequest mapSectionRequest)
		{
			var result = new MapSectionProcessInfo(JobNumber, _sectionsCompleted, mapSectionRequest.TimeToCompleteGenRequest, mapSectionRequest.ProcessingDuration, mapSectionRequest.FoundInRepo);
			return result;
		}

		#endregion
	}
}
