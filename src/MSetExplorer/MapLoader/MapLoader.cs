using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoader
	{
		private readonly BigVector _mapBlockOffset;
		private readonly Action<object, MapSection> _callback;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly MapSectionHelper _mapSectionHelper;

		private IList<MapSectionRequest>? _mapSectionRequests;
		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionsCompleted;
		private TaskCompletionSource? _tcs;

		#region Constructor

		public MapLoader(BigVector mapBlockOffset, Action<object, MapSection> callback, MapSectionHelper mapSectionHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
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
					if (_sectionsCompleted == _sectionsRequested)
					{
						_tcs?.SetResult();
					}
					break;
				}

				//Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");

				_mapSectionRequestProcessor.AddWork(JobNumber, mapSectionRequest, HandleResponse);
				mapSectionRequest.Sent = true;
				_ = Interlocked.Increment(ref _sectionsRequested);
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			if (mapSectionResponse.Counts != null && !mapSectionResponse.RequestCancelled)
			{
				var mapSection = _mapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionResponse, _mapBlockOffset);

				_callback(this, mapSection);
				mapSectionRequest.Handled = true;
			}

			_ = Interlocked.Increment(ref _sectionsCompleted);
			if (_sectionsCompleted == _mapSectionRequests?.Count || (_isStopping && _sectionsCompleted == _sectionsRequested))
			{
				if (_tcs?.Task.IsCompleted == false)
				{
					_tcs.SetResult();
				}

				var pr = _mapSectionRequestProcessor.GetPendingRequests(JobNumber);
				Debug.WriteLine($"MapLoader is done with Job: {JobNumber}, there are {pr.Count} requests still pending.");
			}
		}

		#endregion
	}
}
