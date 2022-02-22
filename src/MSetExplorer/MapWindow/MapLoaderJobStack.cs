using MapSectionProviderLib;
using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	internal class MapLoaderJobStack : IMapLoaderJobStack
	{
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly Action<MapSection> _onMapSectionReady;
		private readonly Action<SizeInt> _onMapNav;

		private readonly List<GenMapRequestInfo> _requestStack;
		private int _requestStackPointer;

		private readonly object _hmsLock;

		#region Constructor

		public MapLoaderJobStack(MapSectionRequestProcessor mapSectionRequestProcessor, Action<MapSection> onMapSectionReady, Action<SizeInt> onMapNav)
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

		public event EventHandler CurrentJobChanged;

		private GenMapRequestInfo CurrentRequest => _requestStackPointer == -1 ? null : _requestStack[_requestStackPointer];
		private int? CurrentJobNumber => CurrentRequest?.JobNumber;

		public Job CurrentJob => CurrentRequest?.Job;
		public bool CanGoBack => !(CurrentJob?.ParentJob is null);
		public bool CanGoForward
		{
			get
			{
				var result = TryGetNextJobInStack(_requestStackPointer, out var _);
				return result;
			}
		}

		public IEnumerable<Job> Jobs => new ReadOnlyCollection<Job>(_requestStack.Select(x => x.Job).ToList());

		#endregion

		#region Public Methods

		public void LoadJobStack(IEnumerable<Job> jobs)
		{
			foreach (var job in jobs)
			{
				_requestStack.Add(new GenMapRequestInfo(job));
			}

			_requestStackPointer = _requestStack.Count - 1;

			Rerun(_requestStackPointer);
		}

		public void Push(Job job)
		{
			CheckForDuplicateJob(job.Id);
			StopCurrentJob();

			var genMapRequestInfo = PushRequest(job);

			CurrentJobChanged?.Invoke(this, new EventArgs());
			_onMapNav(CurrentJob.CanvasControlOffset);

			genMapRequestInfo.StartLoading();
		}

		public void UpdateJob(Job oldJob, Job newJob)
		{
			if (TryFindByJobId(oldJob.Id, out var genMapRequestInfo))
			{
				genMapRequestInfo.Job = newJob;

				var oldJobId = oldJob.Id;
				foreach (var req in _requestStack)
				{
					if (req.Job?.ParentJob?.Id == oldJobId)
					{
						req.Job.ParentJob = newJob;
					}
				}
			}
			else
			{
				throw new KeyNotFoundException("The old job could not be found.");
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

		#endregion

		#region Private Methods

		private GenMapRequestInfo PushRequest(Job job)
		{
			lock (_hmsLock)
			{
				var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
				var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);
				var result = new GenMapRequestInfo(job, jobNumber, mapLoader);

				_requestStack.Add(result);
				_requestStackPointer = _requestStack.Count - 1;

				return result;
			}
		}

		private void Rerun(int newRequestStackPointer)
		{
			if (newRequestStackPointer < 0 || newRequestStackPointer > _requestStack.Count - 1)
			{
				throw new ArgumentException($"The newRequestStackPointer with value: {newRequestStackPointer} is not valid.", nameof(newRequestStackPointer));
			}

			StopCurrentJob();

			var genMapRequestInfo = RerunRequest(newRequestStackPointer);

			_onMapNav(CurrentJob.CanvasControlOffset);
			CurrentJobChanged?.Invoke(this, new EventArgs());

			genMapRequestInfo.StartLoading();
		}

		private GenMapRequestInfo RerunRequest(int newRequestStackPointer)
		{
			lock (_hmsLock)
			{
				var result = _requestStack[newRequestStackPointer];
				var job = result.Job;

				var jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
				var mapLoader = new MapLoader(job, jobNumber, HandleMapSection, _mapSectionRequestProcessor);
				result.Renew(jobNumber, mapLoader);

				_requestStackPointer = newRequestStackPointer;

				return result;
			}
		}

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
			var lastestDtFound = DateTime.MinValue;

			lock (_hmsLock)
			{
				for (var i = 0; i < _requestStack.Count; i++)
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

		private void CheckForDuplicateJob(ObjectId id)
		{
			if (TryFindByJobId(id, out _))
			{
				throw new InvalidOperationException($"A job with id: {id} has already been pushed.");
			}
		}

		private bool TryFindByJobId(ObjectId id, out GenMapRequestInfo genMapRequestInfo)
		{
			genMapRequestInfo = _requestStack.FirstOrDefault(x => x.Job.Id == id);
			return genMapRequestInfo != null;
		}

		private void StopCurrentJob()
		{
			CurrentRequest?.MapLoader?.Stop();
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
