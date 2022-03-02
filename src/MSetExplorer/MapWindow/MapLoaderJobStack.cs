using MapSectionProviderLib;
using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoaderJobStack : IMapLoaderJobStack
	{
		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly List<GenMapRequestInfo> _requestStack;

		private int _requestStackPointer;
		private readonly ReaderWriterLockSlim _stackLock;

		#region Constructor

		public MapLoaderJobStack(MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_synchronizationContext = SynchronizationContext.Current;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_requestStack = new List<GenMapRequestInfo>();
			_requestStackPointer = -1;
			_stackLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		#endregion

		public event EventHandler<MapSection> MapSectionReady;

		#region Public Properties

		public event EventHandler CurrentJobChanged;

		private GenMapRequestInfo CurrentRequest => DoWithReadLock(() => { return _requestStackPointer == -1 ? null : _requestStack[_requestStackPointer]; });

		public Job CurrentJob => CurrentRequest?.Job;
		public bool CanGoBack => !(CurrentJob?.ParentJob is null);

		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextJobInStack(_requestStackPointer, out var _); });

		public IEnumerable<Job> Jobs => DoWithReadLock(() => { return new ReadOnlyCollection<Job>(_requestStack.Select(x => x.Job).ToList()); });

		#endregion

		#region Public Methods

		public void LoadJobStack(IEnumerable<Job> jobs)
		{
			DoWithWriteLock(() =>
			{
				foreach (var job in jobs)
				{
					_requestStack.Add(new GenMapRequestInfo(job));
				}

				_requestStackPointer = _requestStack.Count - 1;

				Rerun(_requestStackPointer);
			});
		}

		public void Push(Job job, IList<MapSection> emptyMapSections)
		{
			DoWithWriteLock(() =>
			{
				CheckForDuplicateJob(job.Id);
				StopCurrentJobInternal();

				var genMapRequestInfo = PushRequest(job);

				CurrentJobChanged?.Invoke(this, new EventArgs());
				//ResetMapDisplay(CurrentJob.CanvasControlOffset);

				genMapRequestInfo.StartLoading(emptyMapSections);
			});
		}

		public void UpdateJob(Job oldJob, Job newJob)
		{
			DoWithWriteLock(() =>
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
			});
		}

		public bool GoBack()
		{
			_stackLock.EnterUpgradeableReadLock();
			try
			{
				var parentJob = CurrentJob?.ParentJob;

				if (!(parentJob is null))
				{
					_stackLock.EnterWriteLock();
					try
					{
						var genMapRequestInfo = _requestStack.FirstOrDefault(x => parentJob.Id == x.Job.Id);
						if (!(genMapRequestInfo is null))
						{
							var idx = _requestStack.IndexOf(genMapRequestInfo);
							Rerun(idx);
							return true;
						}
					}
					finally
					{
						_stackLock.ExitWriteLock();
					}
				}

				return false;
			}
			finally
			{
				_stackLock.ExitUpgradeableReadLock();
			}
		}

		public bool GoForward()
		{
			_stackLock.EnterUpgradeableReadLock();
			try
			{
				if (TryGetNextJobInStack(_requestStackPointer, out var nextRequestStackPointer))
				{
					_stackLock.EnterWriteLock();
					try
					{
						Rerun(nextRequestStackPointer);
						return true;
					}
					finally
					{
						_stackLock.ExitWriteLock();
					}
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_stackLock.ExitUpgradeableReadLock();
			}
		}

		public void StopCurrentJob()
		{
			DoWithWriteLock(StopCurrentJobInternal);
		}

		#endregion

		#region Event Handlers

		private void HandleMapSection(object sender, MapSection mapSection)
		{
			DoWithWriteLock(() =>
			{
				var jobNumber = (sender as MapLoader)?.JobNumber ?? -1;

				if (jobNumber == CurrentRequest.JobNumber)
				{
					_synchronizationContext.Post(o => MapSectionReady?.Invoke(this, mapSection), null);
				}
				else
				{
					Debug.WriteLine($"HandleMapSection is ignoring the new section for job with jobNumber: {jobNumber}. CurJobNum: {CurrentRequest.JobNumber}"); 
				}
			});
		}

		#endregion

		#region Private Methods

		private GenMapRequestInfo PushRequest(Job job)
		{
			var mapLoader = new MapLoader(job, HandleMapSection, _mapSectionRequestProcessor);
			var result = new GenMapRequestInfo(job, mapLoader);

			_requestStack.Add(result);
			_requestStackPointer = _requestStack.Count - 1;

			return result;
		}

		private void Rerun(int newRequestStackPointer)
		{
			if (newRequestStackPointer < 0 || newRequestStackPointer > _requestStack.Count - 1)
			{
				throw new ArgumentException($"The newRequestStackPointer with value: {newRequestStackPointer} is not valid.", nameof(newRequestStackPointer));
			}

			StopCurrentJobInternal();

			var genMapRequestInfo = RenewRequest(newRequestStackPointer);

			//ResetMapDisplay(CurrentJob.CanvasControlOffset);
			CurrentJobChanged?.Invoke(this, new EventArgs());

			genMapRequestInfo.StartLoading();
		}

		private GenMapRequestInfo RenewRequest(int newRequestStackPointer)
		{
			var result = _requestStack[newRequestStackPointer];
			var job = result.Job;

			var mapLoader = new MapLoader(job, HandleMapSection, _mapSectionRequestProcessor);
			result.Renew(mapLoader);

			_requestStackPointer = newRequestStackPointer;

			return result;
		}

		private void StopCurrentJobInternal()
		{
			CurrentRequest?.MapLoader?.Stop();
		}

		#endregion

		#region Request Stack Management 

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
			}

			return false;
		}

		private bool TryGetJobFromStack(int requestStackPointer, out Job job)
		{
			if (requestStackPointer < 0 || requestStackPointer > _requestStack.Count - 1)
			{
				job = null;
				return false;
			}
			else
			{
				job = _requestStack[requestStackPointer].Job;
				return true;
			}
		}

		private bool TryGetLatestChildJobIndex(Job parentJob, out int requestStackPointer)
		{
			requestStackPointer = -1;
			var lastestDtFound = DateTime.MinValue;

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

			var result = requestStackPointer != -1;
			return result;
		}

		private void CheckForDuplicateJob(ObjectId id)
		{
			if (_requestStack.Any(x => x.Job.Id == id))
			{
				throw new InvalidOperationException($"A job with id: {id} has already been pushed.");
			}
		}

		private bool TryFindByJobId(ObjectId id, out GenMapRequestInfo genMapRequestInfo)
		{
			genMapRequestInfo = _requestStack.FirstOrDefault(x => x.Job.Id == id);
			return genMapRequestInfo != null;
		}

		#endregion

		#region Stack Lock Helpers

		private T DoWithReadLock<T>(Func<T> function)
		{
			_stackLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_stackLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_stackLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_stackLock.ExitWriteLock();
			}
		}

		#endregion

		private class GenMapRequestInfo
		{
			public Job Job { get; set; }

			public int JobNumber { get; private set; }
			public MapLoader MapLoader { get; private set; }

			public GenMapRequestInfo(Job job)
			{
				Job = job ?? throw new ArgumentNullException(nameof(job));
				JobNumber = -1;
				MapLoader = null;
			}

			public GenMapRequestInfo(Job job, MapLoader mapLoader)
			{
				Job = job ?? throw new ArgumentNullException(nameof(job));
				MapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
				JobNumber = mapLoader.JobNumber;
			}

			public void Renew(MapLoader mapLoader)
			{
				MapLoader = mapLoader;
				JobNumber = mapLoader.JobNumber;
			}

			public void StartLoading()
			{
				var startTask = MapLoader.Start();
				_ = startTask.ContinueWith(LoadingComplete);
			}

			public void StartLoading(IList<MapSection> emptyMapSections)
			{
				var startTask = MapLoader.Start(emptyMapSections);
				_ = startTask.ContinueWith(LoadingComplete);
			}

			public void LoadingComplete(Task _)
			{
				//// TODO: Use Dispatcher Invoke instead of Thread.Sleep.
				//Thread.Sleep(10 * 1000);
				MapLoader = null;
			}
		}
	}
}
