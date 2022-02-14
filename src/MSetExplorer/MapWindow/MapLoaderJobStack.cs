using MapSectionProviderLib;
using MongoDB.Bson;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	internal class MapLoaderJobStack
	{
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly Action<MapSection> _onMapSectionReady;
		private readonly Action _onMapNav;

		private readonly List<GenMapRequestInfo> _requestStack;
		private int _requestStackPointer;

		private readonly object _hmsLock;

		#region Constructor

		public MapLoaderJobStack(MapSectionRequestProcessor mapSectionRequestProcessor, Action<MapSection> onMapSectionReady, Action onMapNav)
		{
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_onMapSectionReady = WrapActionWithIProgress(onMapSectionReady);
			_onMapNav = onMapNav;

			_requestStack = new List<GenMapRequestInfo>();
			_requestStackPointer = -1;
			_hmsLock = new object();
		}

		private Action<MapSection> WrapActionWithIProgress(Action<MapSection> rawAction)
		{
			var mapLoadingProgress = new Progress<MapSection>(rawAction);
			Action<MapSection> result = ((IProgress<MapSection>)mapLoadingProgress).Report;

			return result;
		}

		#endregion

		#region Public Properties

		public GenMapRequestInfo CurrentRequest => _requestStackPointer == -1 ? null : _requestStack[_requestStackPointer];
		public Job CurrentJob => CurrentRequest?.Job;
		public int? CurrentJobNumber => CurrentRequest?.JobNumber;

		public bool CanGoBack => !(CurrentJob?.ParentJob is null);

		public bool CanGoForward
		{
			get
			{
				var result = TryGetNextJobInStack(_requestStackPointer, out var _);
				return result;
			}
		}

		public IEnumerable<GenMapRequestInfo> GenMapRequests => new ReadOnlyCollection<GenMapRequestInfo>(_requestStack);

		#endregion

		#region Public Methods

		public void Push(Job job)
		{
			StopCurrentJob();
			_onMapNav();

			lock (_hmsLock)
			{
				var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
				var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);
				var genMapRequestInfo = new GenMapRequestInfo(job, mapLoader, jobNumber);

				_requestStack.Add(genMapRequestInfo);
				_requestStackPointer = _requestStack.Count - 1;
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);
			}
		}

		public void UpdateJob(GenMapRequestInfo genMapRequestInfo, Job job)
		{
			var idx = _requestStack.IndexOf(genMapRequestInfo);
			var oldJobId = _requestStack[idx].Job.Id;

			genMapRequestInfo.UpdateJob(job);

			foreach(var req in _requestStack)
			{
				if(oldJobId == req.Job?.ParentJob?.Id)
				{
					req.Job.ParentJob = job;
				}
			}
		}
		
		public bool GoBack()
		{
			var parentJob = CurrentJob?.ParentJob;

			if (!(parentJob is null))
			{
				var genMapRequestInfo = _requestStack.FirstOrDefault(x => parentJob.Id == x.Job.Id);
				if (!(genMapRequestInfo is null))
				{
					var idx = _requestStack.IndexOf(genMapRequestInfo);
					Rerun(idx);
					return true;
				}
			}

			return false;
		}

		public bool GoForward()
		{
			if (TryGetNextJobInStack(_requestStackPointer, out var nextRequestStackPointer))
			{
				Rerun(nextRequestStackPointer);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void Rerun(int newRequestStackPointer)
		{
			if (newRequestStackPointer < 0 || newRequestStackPointer > _requestStack.Count - 1)
			{
				throw new ArgumentException($"The newRequestStackPointer with value: {newRequestStackPointer} is not valid.", nameof(newRequestStackPointer));
			}

			StopCurrentJob();
			_onMapNav();

			lock (_hmsLock)
			{
				var genMapRequestInfo = _requestStack[newRequestStackPointer];
				var job = genMapRequestInfo.Job;

				var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
				var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);
				genMapRequestInfo.Renew(jobNumber, mapLoader);

				_requestStackPointer = newRequestStackPointer;
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);
			}
		}

		public void StopCurrentJob()
		{
			CurrentRequest?.MapLoader?.Stop();
		}

		#endregion

		#region Private Methods

		private bool TryGetNextJobInStack(int requestStackPointer, out int nextRequestStackPointer)
		{
			nextRequestStackPointer = -1;

			if (TryGetJobFromStack(requestStackPointer, out var job))
			{
				if (TryGetLatestChildJobIndex(job, out var childJobRequestStackPointer))
				{
					nextRequestStackPointer = childJobRequestStackPointer;
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		private bool TryGetJobFromStack(int requestStackPointer, out Job job)
		{
			if (requestStackPointer < 0 || requestStackPointer > _requestStack.Count - 1)
			{
				job = null;
				return false;
			}

			var genMapRequestInfo = _requestStack[requestStackPointer];
			job = genMapRequestInfo.Job;

			return true;
		}

		private bool TryGetLatestChildJobIndex(Job parentJob, out int requestStackPointer)
		{
			requestStackPointer = -1;
			DateTime lastestDtFound = DateTime.MinValue;

			lock (_hmsLock)
			{
				for(var i = 0; i < _requestStack.Count; i++)
				{
					var genMapRequestInfo = _requestStack[i];
					var thisParentJobId = genMapRequestInfo.Job?.ParentJob?.Id ?? ObjectId.Empty;

					if (thisParentJobId.Equals(parentJob.Id))
					{
						var dt = thisParentJobId.CreationTime;
						if (dt > lastestDtFound)
						{
							requestStackPointer = i;
							lastestDtFound = dt;
						}
					}
				}
			}

			var result = requestStackPointer != -1;
			return result;
		}

		private void HandleMapSection(int jobNumber, MapSection mapSection)
		{
			lock (_hmsLock)
			{
				var curJobNumber = CurrentJobNumber;
				if (jobNumber == curJobNumber)
				{
					_onMapSectionReady(mapSection);
				}
				else
				{
					Debug.WriteLine($"HandleMapSection is ignoring the new section. CurJobNum:{curJobNumber}, Handling JobNum: {jobNumber}.");
				}
			}
		}

		#endregion
	}
}
