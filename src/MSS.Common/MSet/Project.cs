using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

//using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Common.MSet.Job>;

namespace MSS.Common.MSet
{
	using JobPathType = ITreePath<JobTreeNode, Job>;

	public class Project : IDisposable, INotifyPropertyChanged, IJobOwner
	{
		#region Private Fields

		private string _name;
		private string? _description;

		private readonly IJobTree _jobTree;

		private readonly List<ColorBandSet> _colorBandSets;
		private readonly IDictionary<int, TargetIterationColorMapRecord> _lookupColorMapByTargetIteration;
		private ColorBandSet _currentColorBandSet;

		private readonly ReaderWriterLockSlim _stateLock;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		private ObjectId? _originalCurrentJobId;

		#endregion

		#region Constructor

		public Project(string name, string? description, Job job, ColorBandSet colorBandSet)
			: this(
				  ObjectId.GenerateNewId(),
				  name, 
				  description, 
				  new List<Job> { job }, 
				  new List<ColorBandSet> { colorBandSet },
				  JobOwnerHelper.CreateLookupColorMapByTargetIteration(job, colorBandSet),
				  currentJobId: job.Id,
				  dateCreatedUtc: DateTime.UtcNow,
				  lastSavedUtc: DateTime.MinValue, 
				  lastAccessedUtc: DateTime.UtcNow
				  )
		{ 
			OnFile = false;
		}

		//public Project(string name, string? description, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId)
		//	: this(
		//		  ObjectId.GenerateNewId(), 
		//		  name, 
		//		  description, 
		//		  jobs, 
		//		  colorBandSets,
		//		  new Dictionary<int, TargetIterationColorMapRecord>(),
		//		  currentJobId,
		//		  dateCreatedUtc: DateTime.UtcNow,
		//		  lastSavedUtc: DateTime.MinValue, 
		//		  lastAccessedUtc: DateTime.UtcNow
		//		  )
		//{
		//	OnFile = false;
		//}



		public Project(ObjectId id, string name, string? description, 
			List<Job> jobs, 
			IEnumerable<ColorBandSet> colorBandSets,
			IDictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration,
			ObjectId currentJobId, 
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			if (!jobs.Any())
			{
				throw new InvalidOperationException("Cannot create a project using an empty list of jobs.");
			}

			Id = id;
			OnFile = true;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			_jobTree = BuildJobTree(jobs, useFlat: false, checkHomeJob: true);

			_colorBandSets = new List<ColorBandSet>(colorBandSets);
			_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			LastUpdatedUtc = DateTime.MinValue;
			DateCreatedUtc = dateCreatedUtc;
			LastSavedUtc = lastSavedUtc;
			LastAccessedUtc = lastAccessedUtc;

			_originalCurrentJobId = currentJobId;

			var jobsFromTree = _jobTree.GetItems().ToList();

			//var currentJob = jobs.FirstOrDefault(x => x.Id == currentJobId);
			var currentJob = jobsFromTree.FirstOrDefault(x => x.Id == currentJobId);

			if (currentJob == null)
			{
				Debug.WriteLine($"WARNING: The Project has a CurrentJobId of {Id}, but this job cannot be found. Setting the current job to be the last job.");
				//currentJob = jobs.Last();
				currentJob = jobsFromTree.Last();
			}

			//_  = JobOwnerHelper.LoadColorBandSet(currentJob, operationDescription: "as the project is being constructed", _colorBandSets, out var wasUpdated, out var wasCreated);
			var targetIterations = currentJob.MapCalcSettings.TargetIterations;
			_currentColorBandSet = JobOwnerHelper.LoadColorBandSet(null, targetIterations, operationDescription: "as the project is being constructed", _colorBandSets, lookupColorMapByTargetIteration);

			//if (wasUpdated)
			//{
			//	LastUpdatedUtc = DateTime.UtcNow;
			//}

			_jobTree.CurrentItem = currentJob;
			_ = _jobTree.MakePreferred(_jobTree.GetCurrentPath());

			JobNodes = _jobTree.Nodes;

			_lookupColorMapByTargetIteration = lookupColorMapByTargetIteration;

			Debug.WriteLine($"Project is loaded. CurrentJobId: {_jobTree.CurrentItem.Id}, Current ColorBandSetId: {currentJob.ColorBandSetId}. IsDirty = {IsDirty}");
		}

		private IJobTree BuildJobTree(List<Job> jobs, bool useFlat, bool checkHomeJob)
		{
			IJobTree result;

			if (useFlat)
			{
				result = new JobTreeFlat(jobs, checkHomeJob);
			}
			else
			{
				result = new JobTreeSimple(jobs, checkHomeJob);
			}

			return result;
		}

		#endregion

		#region Public Properties

		public DateTime DateCreated => Id == ObjectId.Empty ? LastSavedUtc : Id.CreationTime;

		private ObservableCollection<JobTreeNode>? _jobItems;

		public ObservableCollection<JobTreeNode>? JobNodes
		{
			get => _jobItems;
			set
			{
				_jobItems = value;
				OnPropertyChanged();
			}
		}

		private bool _anyColorBandSetIsDirty => _colorBandSets.Any(x => x.IsDirty);

		public bool IsDirty => LastUpdatedUtc > LastSavedUtc || _anyColorBandSetIsDirty || _jobTree.IsDirty; // || _jobTree.AnyItemIsDirty;

		public bool IsCurrentJobIdChanged => CurrentJobId != _originalCurrentJobId;

		public ObjectId Id { get; init; }

		public bool OnFile { get; private set; }

		public OwnerType OwnerType => OwnerType.Project;

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public string? Description
		{
			get => _description;
			set
			{
				if (_description != value)
				{
					_description = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public DateTime DateCreatedUtc { get; init; }

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			private set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
				OnFile = true;
			}
		}

		public DateTime LastUpdatedUtc
		{
			get => _lastUpdatedUtc;

			private set
			{
				var isDirtyBefore = IsDirty;
				_lastUpdatedUtc = value;

				if (IsDirty != isDirtyBefore)
				{
					OnPropertyChanged(nameof(IsDirty));
				}
			}
		}

		public DateTime LastAccessedUtc { get; init; } // TODO: finish incorporating this property into this class.

		public Job CurrentJob
		{
			get => _jobTree.CurrentItem;
			set
			{
				if (CurrentJob != value)
				{
					if (!value.IsEmpty)
					{
						if (!value.OnFile)
						{
							LastUpdatedUtc = DateTime.UtcNow;
						}

						//var colorBandSetIdBeforeUpdate = _jobTree.CurrentItem.ColorBandSetId;

						//var colorBandSetIdBeforeUpdate = _currentColorBandSet.Id;

						var targetIterations = value.MapCalcSettings.TargetIterations;
						_currentColorBandSet = JobOwnerHelper.LoadColorBandSet(_currentColorBandSet, targetIterations, operationDescription: "as the Current Job is being updated", _colorBandSets, _lookupColorMapByTargetIteration);

						_jobTree.CurrentItem = value;

						//if (_jobTree.CurrentItem.ColorBandSetId != colorBandSetIdBeforeUpdate)
						//{
						//	OnPropertyChanged(nameof(CurrentColorBandSet));
						//}
					}
					else
					{
						Debug.WriteLine($"Project. The CurrentJob is being updated to be EMPTY. The JobTree CurrentItem is {_jobTree.CurrentItem}. The JobTree CurrentItem IsEmpty = {_jobTree.CurrentItem.IsEmpty}.");
					}

					OnPropertyChanged();
				}
			}
		}

		public ObjectId CurrentJobId
		{
			get
			{
				var currentJob = CurrentJob;

				if (currentJob.IsEmpty)
				{
					throw new InvalidOperationException("The current job is empty.");
				}

				return currentJob.Id;
			}
		}

		//public ColorBandSet CurrentColorBandSet
		//{
		//	get => _colorBandSets.FirstOrDefault(x => x.Id == CurrentJob.ColorBandSetId) ?? new ColorBandSet(Name, CurrentJob.MapCalcSettings.TargetIterations);
		//	set
		//	{
		//		if (!CurrentJob.IsEmpty)
		//		{
		//			var newCbs = value;

		//			if (newCbs.Id != CurrentJob.ColorBandSetId)
		//			{
		//				if (!_colorBandSets.Contains(newCbs))
		//				{
		//					if (newCbs.ProjectId != Id)
		//					{
		//						// Make a copy of the incoming ColorBandSet
		//						// and set it's ProjectId to this Project's Id
		//						// and give it a new SerialNumber.
		//						newCbs = newCbs.CreateNewCopy();
		//						newCbs.AssignNewSerialNumber();
		//						newCbs.ProjectId = Id;
		//					}

		//					_colorBandSets.Add(newCbs);
		//				}

		//				JobOwnerHelper.AddIteratationColorMapRecord(newCbs, _lookupColorMapByTargetIteration, makeDefault: true);

		//				CurrentJob.ColorBandSetId = newCbs.Id;
		//				LastUpdatedUtc = DateTime.UtcNow;

		//				OnPropertyChanged(nameof(CurrentColorBandSet));
		//			}
		//		}
		//		else
		//		{
		//			Debug.WriteLine($"Not setting the CurrentColorBandSet, the CurrentJob is empty.");
		//		}
		//	}
		//}

		public ColorBandSet CurrentColorBandSet
		{
			get => _currentColorBandSet;
			set
			{
				if (value != _currentColorBandSet)
				{
					var newCbs = value;

					if (!_colorBandSets.Contains(newCbs))
					{
						if (newCbs.OwnerId != Id)
						{
							// Make a copy of the incoming ColorBandSet
							// and set it's ProjectId to this Project's Id
							// and give it a new SerialNumber.
							newCbs = newCbs.CreateNewCopy(ObjectId.GenerateNewId());
							newCbs.AssignNewSerialNumber();
							newCbs.OwnerId = Id;
						}

						_colorBandSets.Add(newCbs);
					}

					JobOwnerHelper.AddIteratationColorMapRecord(newCbs, _lookupColorMapByTargetIteration, makeDefault: true);

					//CurrentJob.ColorBandSetId = newCbs.Id;

					_currentColorBandSet = newCbs;
					LastUpdatedUtc = DateTime.UtcNow;

					OnPropertyChanged(nameof(CurrentColorBandSet));
				}
				else
				{
					ObjectId currentCbsIdForTargetIterations = ObjectId.Empty;

					if (_lookupColorMapByTargetIteration.TryGetValue(value.TargetIterations, out var ticmr))
					{
						currentCbsIdForTargetIterations = ticmr.ColorBandSetId;
					}

					if (value.Id != currentCbsIdForTargetIterations)
					{
						JobOwnerHelper.AddIteratationColorMapRecord(value, _lookupColorMapByTargetIteration, makeDefault: true);
						Debug.WriteLine($"WARNING: The Default ColorBandSet for {value.TargetIterations} is being set HOWEVER the CurrentColorBandSet already had this same value.");
					}
					else
					{
						Debug.WriteLine($"Not setting the CurrentColorBandSet, the CurrentColorBandSet is already updated.");
					}
				}
			}
		}

		public ObjectId CurrentColorBandSetId => CurrentColorBandSet.Id;

		public JobTreeNode? SelectedViewItem
		{
			get => _jobTree.SelectedNode;
			set
			{
				_jobTree.SelectedNode = value;
				OnPropertyChanged();
			}
		}

		//TODO: Add a "PreferredPath property to the Project class.

		#endregion

		#region Public Methods

		public void Add(Job job)
		{
			var colorBandSet = _colorBandSets.FirstOrDefault(x => x.Id == job.ColorBandSetId);

			if (colorBandSet == null) 
			{
				throw new InvalidOperationException("Cannot add this job, the job's ColorBandSet has not yet been added.");
			}

			JobOwnerHelper.AddIteratationColorMapRecord(colorBandSet, _lookupColorMapByTargetIteration, makeDefault:true);

			_ = _jobTree.Add(job, selectTheAddedItem: true);

			LastUpdatedUtc = DateTime.UtcNow;
		}

		public void Add(ColorBandSet colorBandSet, bool makeDefault)
		{
			if (!_colorBandSets.Any(x => x.Id == colorBandSet.Id))
			{
				_colorBandSets.Add(colorBandSet);
			}

			JobOwnerHelper.AddIteratationColorMapRecord(colorBandSet, _lookupColorMapByTargetIteration, makeDefault);

			LastUpdatedUtc = DateTime.UtcNow;
		}

		public void MarkAsSaved()
		{
			LastSavedUtc = DateTime.UtcNow;
			_originalCurrentJobId = CurrentJobId;
			_jobTree.IsDirty = false;
		}

		public void MarkAsDirty()
		{
			LastUpdatedUtc = DateTime.UtcNow;
		}

		public List<TargetIterationColorMapRecord> GetTargetIterationColorMapRecords()
		{
			List<TargetIterationColorMapRecord> result = _lookupColorMapByTargetIteration.Values.ToList();

			return result;
		}

		#endregion

		#region Public Methods - Job Tree

		public bool GoBack(bool skipPanJobs)
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (_jobTree.TryGetPreviousJob(out var job, skipPanJobs))
				{
					//int idx = index;
					DoWithWriteLock(() =>
					{
						CurrentJob = job;
					});

					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_stateLock.ExitUpgradeableReadLock();
			}
		}

		public bool GoForward(bool skipPanJobs)
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (_jobTree.TryGetNextJob(out var job, skipPanJobs))
				{
					DoWithWriteLock(() =>
					{
						CurrentJob = job;
					});

					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_stateLock.ExitUpgradeableReadLock();
			}
		}

		public bool CanGoBack(bool skipPanJobs)
		{
			var result = _jobTree.TryGetPreviousJob(out _, skipPanJobs);
			return result;
		}

		public bool CanGoForward(bool skipPanJobs)
		{
			var result = _jobTree.TryGetNextJob(out _, skipPanJobs);
			return result;
		}

		public IEnumerable<Job> GetJobs()
		{
			return _jobTree.GetItems();
		}

		public List<ColorBandSet> GetColorBandSets()
		{
			return _colorBandSets;
		}

		public JobPathType? GetCurrentPath() => _jobTree.GetCurrentPath();
		public JobPathType? GetPath(ObjectId jobId) => _jobTree.GetPath(jobId);

		public Job? GetJob(ObjectId jobId) => _jobTree.GetItem(jobId);
		public Job? GetParent(Job job) => _jobTree.GetParentItem(job);
		//public List<Job>? GetJobAndDescendants(ObjectId jobId) => _jobTree.GetItemAndDescendants(jobId);

		public bool MarkBranchAsPreferred(ObjectId jobId)
		{
			var result = _jobTree.MakePreferred(jobId);
			return result;
		}

		public IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			var saveCurrentPath = _jobTree.GetCurrentPath();
			var saveCurrentItem = _jobTree.CurrentItem;
			var nodesRemoved = _jobTree.RemoveJobs(path, nodeSelectionType);
			var newCurrentPath = _jobTree.GetCurrentPath();
			var newCurrentItem = _jobTree.CurrentItem;

			var wasCurrentJobRemoved = nodesRemoved.Any(x => x.Id == CurrentJob?.Id);
			//if (wasCurrentJobRemoved || newCurrentPath != saveCurrentPath)
			//{
			//	Debug.WriteLine($"RemoveJobs has changed the current path. Old: {saveCurrentPath}, new: {newCurrentPath}");
			//	OnPropertyChanged(nameof(CurrentJob));
			//}

			if (wasCurrentJobRemoved || newCurrentItem != saveCurrentItem)
			{
				Debug.WriteLine($"RemoveJobs has changed the current path. Old: {saveCurrentItem}, new: {newCurrentItem}");
				OnPropertyChanged(nameof(CurrentJob));
			}

			return nodesRemoved;
		}

		public int GetNumberOfDirtyJobs()
		{
			var result = _jobTree.GetItems().Count(x => !x.OnFile || x.IsDirty);
			return result;
		}

		#endregion

		#region Private Methods

		#endregion

		#region Lock Helpers

		//private T DoWithReadLock<T>(Func<T> function)
		//{
		//	_stateLock.EnterReadLock();

		//	try
		//	{
		//		return function();
		//	}
		//	finally
		//	{
		//		_stateLock.ExitReadLock();
		//	}
		//}

		private void DoWithWriteLock(Action action)
		{
			_stateLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)

					if (_jobTree != null)
					{
						_jobTree.Dispose();
					}

					if (_colorBandSets != null)
					{
						//_colorBandSetCollection.Dispose();
						//_colorBandSetCollection = null;
					}

				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
