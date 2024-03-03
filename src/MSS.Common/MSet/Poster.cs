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

//using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Common.MSet.Job>;

namespace MSS.Common.MSet
{
	using JobPathType = ITreePath<JobTreeNode, Job>;

	public class Poster : IDisposable, INotifyPropertyChanged, ICloneable, IJobOwner
	{
		#region Private Fields

		private string _name;
		private string? _description;
		private string _sizeAsString;

		private readonly IJobTree _jobTree;
		private readonly List<ColorBandSet> _colorBandSets;
		private readonly IDictionary<int, TargetIterationColorMapRecord> _lookupColorMapByTargetIteration;
		private ColorBandSet _currentColorBandSet;

		//private readonly ReaderWriterLockSlim _stateLock;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		private ObjectId? _originalCurrentJobId;

		#endregion

		#region Constructor

		//public Poster(string name, string? description, ObjectId sourceJobId,
		//	List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId
		//	)
		//	: this(ObjectId.GenerateNewId(), name, description, sourceJobId,
		//		  jobs, colorBandSets, currentJobId,
		//		  posterSize: RMapConstants.DEFAULT_POSTER_SIZE, displayPosition: new VectorInt(), displayZoom: RMapConstants.DEFAULT_POSTER_DISPLAY_ZOOM,
		//		  DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow)
		//{
		//	OnFile = false;
		//}

		public Poster(ObjectId id, string name, string? description, 
			ObjectId sourceJobId, 
			List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets,
			IDictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration,

			ObjectId currentJobId,
			SizeDbl posterSize, VectorDbl displayPosition, double displayZoom,
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Id = id;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			SourceJobId = sourceJobId;

			PosterSize = posterSize;
			_sizeAsString = GetFormattedPosterSize(PosterSize);

			DisplayPosition = displayPosition;
			DisplayZoom = displayZoom;

			_jobTree = BuildJobTree(jobs, useFlat: false, checkHomeJob: false);

			_colorBandSets = new List<ColorBandSet>(colorBandSets);
			//_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			DateCreatedUtc = dateCreatedUtc;

			LastUpdatedUtc = DateTime.MinValue;
			_lastSavedUtc =  lastSavedUtc;
			OnFile = true;

			LastAccessedUtc = lastAccessedUtc;

			_originalCurrentJobId = currentJobId;

			var jobsFromTree = _jobTree.GetItems().ToList();

			//var currentJob = jobs.FirstOrDefault(x => x.Id == currentJobId);
			var currentJob = jobsFromTree.FirstOrDefault(x => x.Id == currentJobId);

			if (currentJob == null)
			{
				Debug.WriteLine($"WARNING: The Poster has a CurrentJobId of {Id}, but this job cannot be found. Setting the current job to be the last job.");
				//currentJob = jobs.Last();
				currentJob = jobsFromTree.Last();
			}

			var targetIterations = currentJob.MapCalcSettings.TargetIterations;
			_currentColorBandSet = JobOwnerHelper.LoadColorBandSet(targetIterations, operationDescription: "as the poster is being constructed", _colorBandSets, lookupColorMapByTargetIteration);
			
			_jobTree.CurrentItem = currentJob;

			//_ = _jobTree.MakePreferred(_jobTree.GetCurrentPath());
			//JobNodes = _jobTree.Nodes;

			_lookupColorMapByTargetIteration = lookupColorMapByTargetIteration;

			Debug.WriteLine($"Poster is loaded. CurrentJobId: {CurrentJob.Id}, Current ColorBandSetId: {CurrentColorBandSet.Id}. IsDirty: {IsDirty}");
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

		public bool IsDirty => LastUpdatedUtc > LastSavedUtc || _jobTree.IsDirty; // || _jobTree.AnyItemIsDirty;

		public bool IsCurrentJobIdChanged => CurrentJobId != _originalCurrentJobId;

		public ObjectId Id { get; init; }

		public bool OnFile { get; private set; }

		public OwnerType OwnerType => OwnerType.Poster;

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

		public SizeDbl PosterSize { get; set; }

		public string SizeAsString
		{
			get => _sizeAsString;
			private set
			{
				if (value != _sizeAsString)
				{
					_sizeAsString = value;
					OnPropertyChanged();
				}
			}
		}

		public VectorDbl DisplayPosition { get; set; }
		public double DisplayZoom { get; set; }

		public ObjectId SourceJobId { get; init; }

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

		public DateTime LastAccessedUtc { get; init; }

		//public Job CurrentJob
		//{
		//	get => _jobTree.CurrentItem;
		//	set
		//	{
		//		if (CurrentJob != value)
		//		{
		//			if (!value.IsEmpty)
		//			{
		//				if (!value.OnFile)
		//				{
		//					LastUpdatedUtc = DateTime.UtcNow;
		//				}

		//				var colorBandSetIdBeforeUpdate = _jobTree.CurrentItem.ColorBandSetId;

		//				//_ = LoadColorBandSet(value, operationDescription: "as the Current Job is being updated");
		//				_ = JobOwnerHelper.LoadColorBandSet(value, operationDescription: "as the Current Job is being updated", _colorBandSets, _lookupColorMapByTargetIteration);


		//				_jobTree.CurrentItem = value;

		//				if (_jobTree.CurrentItem.ColorBandSetId != colorBandSetIdBeforeUpdate)
		//				{
		//					OnPropertyChanged(nameof(CurrentColorBandSet));
		//				}
		//			}

		//			OnPropertyChanged();
		//		}
		//	}
		//}

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
						var targetIterations = value.MapCalcSettings.TargetIterations;
						CurrentColorBandSet = JobOwnerHelper.LoadColorBandSet(targetIterations, operationDescription: "as the Current Job is being updated", _colorBandSets, _lookupColorMapByTargetIteration);

						_jobTree.CurrentItem = value;

						//if (_jobTree.CurrentItem.ColorBandSetId != colorBandSetIdBeforeUpdate)
						//{
						//	OnPropertyChanged(nameof(CurrentColorBandSet));
						//}
					}
					else
					{
						Debug.WriteLine($"Poster. The CurrentJob is being updated to be EMPTY. The JobTree CurrentItem is {_jobTree.CurrentItem}. The JobTree CurrentItem IsEmpty = {_jobTree.CurrentItem.IsEmpty}.");
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

					if (newCbs.Id != CurrentJob.ColorBandSetId)
					{
						if (!_colorBandSets.Contains(newCbs))
						{
							if (newCbs.ProjectId != Id)
							{
								// Make a copy of the incoming ColorBandSet
								// and set it's ProjectId to this Project's Id
								// and give it a new SerialNumber.
								newCbs = newCbs.CreateNewCopy();
								newCbs.AssignNewSerialNumber();
								newCbs.ProjectId = Id;
							}
							_colorBandSets.Add(newCbs);
						}

						JobOwnerHelper.AddIteratationColorMapRecord(newCbs, _lookupColorMapByTargetIteration, makeDefault: true);

						CurrentJob.ColorBandSetId = newCbs.Id;
						LastUpdatedUtc = DateTime.UtcNow;

						OnPropertyChanged(nameof(CurrentColorBandSet));
					}
				}
				else
				{
					Debug.WriteLine($"Not setting the CurrentColorBandSet, the CurrentColorBandSet is already updated.");
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

		#endregion

		#region Public Methods 

		public void Add(Job job)
		{
			var colorBandSet = _colorBandSets.FirstOrDefault(x => x.Id == job.ColorBandSetId);

			if (colorBandSet == null)
			{
				throw new InvalidOperationException("Cannot add this job, the job's ColorBandSet has not yet been added.");
			}

			JobOwnerHelper.AddIteratationColorMapRecord(colorBandSet, _lookupColorMapByTargetIteration, makeDefault: true);

			_jobTree.Add(job, selectTheAddedItem: true);

			LastUpdatedUtc = DateTime.UtcNow;

			Debug.Assert(IsDirty, "After adding a job to the poster, the Poster IsDirty flag is still false.");

			OnPropertyChanged(nameof(CurrentJob));
		}

		public void Add(ColorBandSet colorBandSet, bool makeDefault)
		{
			_colorBandSets.Add(colorBandSet);

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

		//public IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		//{
		//	var result = _jobTree.RemoveJobs(path, nodeSelectionType);
		//	return result;
		//}

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

		private string GetFormattedPosterSize(SizeDbl size)
		{
			var roundedPosterSize = size.Round(MidpointRounding.AwayFromZero);

			var result = $"{roundedPosterSize.Width} x {roundedPosterSize.Height}";
			return result;
		}

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		Poster Clone()
		{
			return new Poster(Id, Name, Description, SourceJobId,
				_jobTree.GetItems().ToList(), _colorBandSets, _lookupColorMapByTargetIteration, _jobTree.CurrentItem.Id,
				PosterSize, DisplayPosition, DisplayZoom,
				DateCreatedUtc, LastSavedUtc, LastAccessedUtc)
			{
				OnFile = OnFile
			};
		}

		public Poster CreateNewCopy()
		{
			return new Poster(ObjectId.GenerateNewId(), Name, Description, SourceJobId,
				_jobTree.GetItems().ToList(), _colorBandSets, _lookupColorMapByTargetIteration, _jobTree.CurrentItem.Id,
				PosterSize, DisplayPosition, DisplayZoom, 
				dateCreatedUtc: DateTime.UtcNow, lastSavedUtc: DateTime.MinValue, lastAccessedUtc: DateTime.UtcNow)
			{
				OnFile = false
			};
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
