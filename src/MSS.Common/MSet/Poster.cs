using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;

namespace MSS.Common.MSet
{

	// TODO: Dispose the Job Tree
	public class Poster : INotifyPropertyChanged, ICloneable, IJobOwner
	{
		private string _name;
		private string? _description;
		private string _size;

		private readonly IJobTree _jobTree;
		private readonly List<ColorBandSet> _colorBandSets;

		//private readonly ReaderWriterLockSlim _stateLock;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		private ObjectId? _originalCurrentJobId;

		private VectorInt _dispPosition;
		private double _displayZoom;

		#region Constructor

		public Poster(string name, string? description, ObjectId sourceJobId,
			List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId
			)
			: this(ObjectId.GenerateNewId(), name, description, sourceJobId,
				  jobs, colorBandSets, currentJobId,
				  new VectorInt(), 1.0d,
				  DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow)
		{
			OnFile = false;
		}

		public Poster(ObjectId id, string name, string? description, ObjectId sourceJobId,
			List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId,
			VectorInt displayPosition, double displayZoom,
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Id = id;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			SourceJobId = sourceJobId;

			_dispPosition = displayPosition;
			_displayZoom = displayZoom;

			_jobTree = BuildJobTree(jobs, useFlat: false, checkHomeJob: true);

			_colorBandSets = new List<ColorBandSet>(colorBandSets);
			//_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			DateCreatedUtc = dateCreatedUtc;
			LastUpdatedUtc = DateTime.MinValue;
			LastSavedUtc = lastSavedUtc;
			LastAccessedUtc = lastAccessedUtc;

			_originalCurrentJobId = currentJobId;

			var currentJob = jobs.FirstOrDefault(x => x.Id == currentJobId);

			if (currentJob == null)
			{
				currentJob = jobs.Last();
				Debug.WriteLine($"WARNING: The Project has a CurrentJobId of {Id}, but this job cannot be found. Setting the current job to be the last job.");
			}

			_jobTree.CurrentItem = currentJob;
			var colorBandSet = LoadColorBandSet(currentJob, operationDescription: "as the project is being constructed");

			PosterSize = new SizeInt(4096);

			_size = string.Empty;

			SizeAsString = GetFormattedPosterSize(PosterSize);

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

		public ObservableCollection<JobTreeNode>? JobNodes => _jobTree.Nodes;

		public bool IsDirty => LastUpdatedUtc > LastSavedUtc || _jobTree.IsDirty; // || _jobTree.AnyItemIsDirty;

		public int GetNumberOfDirtyJobs()
		{
			var result = _jobTree.GetItems().Count(x => !x.OnFile || x.IsDirty);
			return result;
		}

		public bool IsCurrentJobIdChanged => CurrentJobId != _originalCurrentJobId;

		public ObjectId Id { get; init; }

		public bool OnFile { get; private set; }

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

		public SizeInt PosterSize { get; set; }

		public string SizeAsString
		{
			get => _size;
			private set
			{
				if (value != _size)
				{
					_size = value;
					OnPropertyChanged();
				}
			}
		}

		public ObjectId SourceJobId { get; init; }

		public VectorInt DisplayPosition
		{
			get => _dispPosition;
			set
			{
				if (value != _dispPosition)
				{
					_dispPosition = value;
					//LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Value between 1.0 and a maximum, where the maximum is posterSize / displaySize
		/// 1.0 presents 1 map "pixel" to 1 screen pixel
		/// 2.0 presents 2 map "pixels" to 1 screen pixel
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - _displayZoom) > 0.1)
				{
					_displayZoom = value;
					//LastUpdatedUtc = DateTime.UtcNow;
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
						if (value != CurrentJob)
						{
							var colorBandSetIdBeforeUpdate = _jobTree.CurrentItem.ColorBandSetId;

							_ = LoadColorBandSet(value, operationDescription: "as the Current Job is being updated");

							_jobTree.CurrentItem = value;

							if (_jobTree.CurrentItem.ColorBandSetId != colorBandSetIdBeforeUpdate)
							{
								OnPropertyChanged(nameof(CurrentColorBandSet));
							}
						}
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
			set
			{
				if (value != CurrentJobId)
				{
					CurrentJobId = value;
					OnPropertyChanged();
				}
			}
		}

		public ColorBandSet CurrentColorBandSet
		{
			get => _colorBandSets.FirstOrDefault(x => x.Id == CurrentJob.ColorBandSetId) ?? new ColorBandSet();
			set
			{
				if (!CurrentJob.IsEmpty)
				{
					var newCbs = value;

					if (newCbs.Id != CurrentJob.ColorBandSetId)
					{
						if (!_colorBandSets.Contains(newCbs))
						{
							if (newCbs.ProjectId != Id)
							{
								// Make a copy of the incoming ColorBandSet
								// and set it's ProjectId to this Project's Id.
								newCbs = newCbs.Clone();
								newCbs.ProjectId = Id;
							}
							_colorBandSets.Add(newCbs);
						}

						CurrentJob.ColorBandSetId = newCbs.Id;
						LastUpdatedUtc = DateTime.UtcNow;

						OnPropertyChanged(nameof(CurrentColorBandSet));
					}
				}
				else
				{
					Debug.WriteLine($"Not setting the CurrentColorBandSet, the CurrentJob is empty.");
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
			if (!_colorBandSets.Any(x => x.Id == job.ColorBandSetId))
			{
				throw new InvalidOperationException("Cannot add this job, the job's ColorBandSet has not yet been added.");
			}

			_jobTree.Add(job, selectTheAddedItem: true);

			LastUpdatedUtc = DateTime.UtcNow;
		}

		public void Add(ColorBandSet colorBandSet)
		{
			_colorBandSets.Add(colorBandSet);
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

		//public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt newCanvasSizeInBlocks, [MaybeNullWhen(false)] out Job matchingProxy)
		//{
		//	return _jobTree.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out matchingProxy);
		//}

		public bool MarkBranchAsPreferred(ObjectId jobId)
		{
			var result = _jobTree.MakePreferred(jobId);
			return result;
		}

		public IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			var result = _jobTree.RemoveJobs(path, nodeSelectionType);
			return result;
		}

		#endregion

		#region Private Methods

		private ColorBandSet LoadColorBandSet(Job job, string operationDescription)
		{
			var colorBandSetId = job.ColorBandSetId;
			var targetIterations = job.MapCalcSettings.TargetIterations;

			var result = GetColorBandSetForJob(colorBandSetId);

			if (result == null || result.HighCutoff != targetIterations)
			{
				string msg;
				if (result == null)
				{
					msg = $"WARNING: The ColorBandSetId {colorBandSetId} of the current job was not found {operationDescription}."; //as the project is being constructed
				}
				else
				{
					msg = $"WARNING: The Current Job's ColorBandSet {colorBandSetId} has a HighCutoff that is different than that Job's target iteration." +
						$"Loading the best matching ColorBandSet from the same project {operationDescription}.";
				}

				Debug.WriteLine(msg);

				result = FindOrCreateSuitableColorBandSetForJob(targetIterations);
				_colorBandSets.Add(result);
				job.ColorBandSetId = result.Id;
				LastUpdatedUtc = DateTime.UtcNow;
			}

			return result;
		}

		private ColorBandSet? GetColorBandSetForJob(ObjectId colorBandSetId)
		{
			var result = _colorBandSets.FirstOrDefault(x => x.Id == colorBandSetId);
			if (result == null)
			{
				Debug.WriteLine($"WARNING: The job's current ColorBandSet: {colorBandSetId} does not exist in the Project list of ColorBandSets.");
			}

			return result;
		}

		private ColorBandSet FindOrCreateSuitableColorBandSetForJob(int targetIterations)
		{
			var colorBandSet = ColorBandSetHelper.GetBestMatchingColorBandSet(targetIterations, _colorBandSets);

			if (colorBandSet.HighCutoff != targetIterations)
			{
				var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, targetIterations);
				Debug.WriteLine($"WARNING: Creating new adjusted ColorBandSet: {adjustedColorBandSet.Id} to replace {colorBandSet.Id} for job: {CurrentJobId}.");
				colorBandSet = adjustedColorBandSet;
			}

			return colorBandSet;
		}

		private string GetFormattedPosterSize(SizeInt size)
		{
			var result = $"{size.Width} x {size.Height}";
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
			return new Poster(Name, Description, SourceJobId,
				//MapAreaInfo.Clone(), ColorBandSet, MapCalcSettings
				_jobTree.GetItems().ToList(), _colorBandSets, _jobTree.CurrentItem.Id
				);
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
