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

		private readonly ColorBandSetStore _colorBandSetStore;

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
			IDictionary<int, TargetIterationColorMapRecord> lookupColorBandSetByTargetIteration,

			ObjectId currentJobId,
			SizeDbl posterSize, VectorDbl displayPosition, double displayZoom,
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Id = id;
			OnFile = true;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			SourceJobId = sourceJobId;

			PosterSize = posterSize;
			_sizeAsString = GetFormattedPosterSize(PosterSize);

			DisplayPosition = displayPosition;
			DisplayZoom = displayZoom;

			_jobTree = BuildJobTree(jobs, useFlat: false, checkHomeJob: false);

			if (jobs.Any(x => x.IsDirty))
			{
				if (!_jobTree.IsDirty)
				{
					Debug.WriteLine("WARNING: One or more jobs is dirty but the JobTree is clean upon construction.");
				}
			}

			//_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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

			_colorBandSetStore = new ColorBandSetStore(colorBandSets.ToList(), lookupColorBandSetByTargetIteration.Values.ToList(), currentJob.ColorBandSetName, currentJob.TargetIterations, currentJob.ColorBandSetVersion);
			_colorBandSetStore.ColorBandSetResolutionStrategy = ColorBandSetResolutionStrategy.PerJobWithVersion;

			LastUpdatedUtc = DateTime.MinValue;
			DateCreatedUtc = dateCreatedUtc;
			_lastSavedUtc =  lastSavedUtc;
			LastAccessedUtc = lastAccessedUtc;

			_jobTree.CurrentItem = currentJob;

			//_ = _jobTree.MakePreferred(_jobTree.GetCurrentPath());
			//JobNodes = _jobTree.Nodes;

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

		public ColorBandSetStore ColorBandSetStore => _colorBandSetStore;

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

						var targetIterations = value.MapCalcSettings.TargetIterations;
						var possiblyNewColorBandSet = _colorBandSetStore.Load(value.ColorBandSetName, value.ColorBandSetVersion, targetIterations, "Updating the Poster's CurrentJob", out var wasUpdated);

						if (wasUpdated)
						{
							if (value.ColorBandSetName != possiblyNewColorBandSet.Name)
							{
								value.ColorBandSetName = possiblyNewColorBandSet.Name;
								value.ColorBandSetVersion = null;
							}
							else
							{
								if (value.ColorBandSetVersion != possiblyNewColorBandSet.Version)
								{
									value.ColorBandSetVersion = null;
								}
							}
							//OnPropertyChanged(nameof(CurrentColorBandSet));
						}

						_jobTree.CurrentItem = value;
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

		public ColorBandSet CurrentColorBandSet
		{
			get => _colorBandSetStore.CurrentColorBandSet;
			set
			{
				if (value != CurrentColorBandSet)
				{
					if (value.OwnerId != Id)
					{
						Debug.WriteLine("WARNING: The new ColorBandSet has a different OwnerId.");
					}

					_colorBandSetStore.CurrentColorBandSet = value;
					LastUpdatedUtc = DateTime.UtcNow;

					if (!CurrentJob.IsEmpty)
					{
						CurrentJob.ColorBandSetName = value.Name;
						CurrentJob.ColorBandSetVersion = value.Version;
					}

					OnPropertyChanged(nameof(CurrentColorBandSet));
				}
				else
				{
					CheckCurrentJobsColorBandSet(CurrentJob, value);

					if (_colorBandSetStore.MakeDefault(value))
					{
						Debug.WriteLine($"WARNING: The Default ColorBandSet for {value.TargetIterations} is being set HOWEVER the CurrentColorBandSet already had this same value.");

						if (!CurrentJob.IsEmpty)
						{
							Debug.Assert(CurrentJob.ColorBandSetName == value.Name, "CurrentJob / CurrentColorBandSet name mismatch");
						}
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

		public ColorBandSetResolutionStrategy ColorBandSetResolutionStrategy
		{
			get => _colorBandSetStore.ColorBandSetResolutionStrategy;
			set
			{
				if (value != _colorBandSetStore.ColorBandSetResolutionStrategy)
				{
					_colorBandSetStore.ColorBandSetResolutionStrategy = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods 

		public void Add(Job job)
		{
			//var colorBandSet = _colorBandSets.FirstOrDefault(x => x.Id == job.ColorBandSetId);

			//if (colorBandSet == null)
			//{
			//	throw new InvalidOperationException("Cannot add this job, the job's ColorBandSet has not yet been added.");
			//}

			//JobOwnerHelper.AddIteratationColorMapRecord(colorBandSet, _lookupColorMapByTargetIteration, makeDefault: true);

			_jobTree.Add(job, selectTheAddedItem: true);

			LastUpdatedUtc = DateTime.UtcNow;

			Debug.Assert(IsDirty, "After adding a job to the poster, the Poster IsDirty flag is still false.");

			OnPropertyChanged(nameof(CurrentJob));
		}

		public void Add(ColorBandSet colorBandSet, bool makeDefault)
		{
			_colorBandSetStore.Add(colorBandSet, makeDefault);
			LastUpdatedUtc = DateTime.UtcNow;
		}

		//public bool RemoveColorBandSet(ColorBandSet colorBandSet/*, ObjectId newId*/)
		//{
		//	var result = _colorBandSetStore.RemoveColorBandSet(colorBandSet);
		//	return result;
		//}

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
			List<TargetIterationColorMapRecord> result = _colorBandSetStore.GetTargetIterationColorMapRecords();

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
			return _colorBandSetStore.GetColorBandSets();
		}

		public ColorBandSet? GetColorBandSet(ObjectId id)
		{
			return _colorBandSetStore.GetColorBandSet(id);
		}

		public ColorBandSet? GetColorBandSet(string name, int targetIterations, int? version)
		{
			return _colorBandSetStore.GetColorBandSet(name, targetIterations, version);
		}

		public bool ColorBandSetExists(ColorBandSet colorBandSet) => _colorBandSetStore.ColorBandSetExists(colorBandSet);

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
				_jobTree.GetItems().ToList(),
				_colorBandSetStore.GetColorBandSets(), 
				JobOwnerHelper.LoadTargetIterationColorMapRecords(_colorBandSetStore.GetTargetIterationColorMapRecords()),
				_jobTree.CurrentItem.Id,
				PosterSize, DisplayPosition, DisplayZoom,
				DateCreatedUtc, LastSavedUtc, LastAccessedUtc)
			{
				OnFile = OnFile
			};
		}

		public Poster CreateNewCopy()
		{
			return new Poster(ObjectId.GenerateNewId(), Name, Description, SourceJobId,
				_jobTree.GetItems().ToList(),
				_colorBandSetStore.GetColorBandSets(),
				JobOwnerHelper.LoadTargetIterationColorMapRecords(_colorBandSetStore.GetTargetIterationColorMapRecords()),
				_jobTree.CurrentItem.Id,
				PosterSize, DisplayPosition, DisplayZoom, 
				dateCreatedUtc: DateTime.UtcNow, lastSavedUtc: DateTime.MinValue, lastAccessedUtc: DateTime.UtcNow)
			{
				OnFile = false
			};
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void CheckCurrentJobsColorBandSet(Job currentJob, ColorBandSet colorBandSet)
		{
			if (currentJob.OwnerId != colorBandSet.OwnerId)
			{
				Debug.WriteLine($"CheckCurrentJobsColorBandSet OwnerId Mismatch.");
			}

			if (colorBandSet.Name != currentJob.ColorBandSetName)
			{
				Debug.WriteLine($"CheckCurrentJobsColorBandSet Name Mismatch.");
			}

			if (currentJob.ColorBandSetVersion.HasValue && currentJob.ColorBandSetVersion.Value != colorBandSet.Version)
			{
				Debug.WriteLine($"CheckCurrentJobsColorBandSet Name Mismatch.");
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

					if (_colorBandSetStore != null)
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
