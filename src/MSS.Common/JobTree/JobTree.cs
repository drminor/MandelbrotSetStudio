using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSS.Common
{
	using JobBranchType = ITreeBranch<JobTreeNode, Job>;
	using JobPathType = ITreePath<JobTreeNode, Job>;
	using JobNodeType = ITreeNode<JobTreeNode, Job>;

	/// <remarks>
	///	New jobs are inserted into order by the date the job was created.
	///	The new job's Parent identifies the job from which this job was created.
	///
	///	New jobs are added as a sibling to it "parent"
	///	a) if the new Job is being added as the last job
	///	b) the job is not a Zoom-In or Zoom-Out and the job that is currently the last is not a Zoom-In or Zoom-Out				
	///  
	///	Otherwise new jobs are added as a "Parked Alternate" to the Job just before the point of insertion.
	///	
	///	Here are the steps:
	///		1. Determine the point of insertion
	///		2. If the job will be added to the end or if it is not a Zoom-In or Zoom-Out and the job currently in the last position is not a Zoom-In or Zoom-Out type job
	///		then insert the new job just before the job at the insertion point.
	///
	///		3. Otherwise
	///			a. Add the new Job as child of job currently at the point of insertion(if the job is a Zoom-In or Zoom-Out type job
	///			or b. Add the job as a child of the job currently in the last position (if the job currently in the last position is Zoom-In or Zoom-Out type job.
	///			c. Make the new newly added node, active by calling MakeBranchActive
	/// </remarks>

	//public class JobTree : ITree<JobTreeNode, Job>, IJobTree
	public class JobTree : Tree<JobTreeNode, Job>, IJobTree
	{
		//protected readonly ReaderWriterLockSlim _treeLock;

		//private readonly JobTreeBranch _root;
		//private JobPathType? _currentPath;
		private JobNodeType? _selectedItem;

		#region Constructor

		public JobTree(List<Job> jobs, bool checkHomeJob) : base(new TreeBranch<JobTreeNode, Job>(new JobTreeNode()))
		{
			//_treeLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

			//_root = new JobTreeBranch();
			var homeJob = GetHomeJob(jobs, checkHomeJob);
			_currentPath = AddJob(homeJob, _root);

			if (_currentPath == null)
			{
				throw new InvalidOperationException("The new JobTreeBranch has a null _currentPath.");
			}

			if (!_currentPath.IsHome)
			{
				//throw new InvalidOperationException("The new JobTreeBranch's CurrentPath is not the HomeNode.");
				Debug.WriteLine("WARNING: The new JobTreeBranch's CurrentPath is not the HomeNode.");
			}

			jobs = jobs.OrderBy(x => x.Id.ToString()).ToList();
			ReportInput(jobs);

			Debug.WriteLine($"Loading {jobs.Count} jobs.");

			// Have BuildTree start with the homeJob, and not the root, so that it will not add the Home Job a second time.
			_currentPath = PopulateTree(jobs, _currentPath);

			ReportOutput(_root, _currentPath);

			//Debug.Assert(_root.RootItem.Job.Id == ObjectId.Empty, "Creating a Root JobTreeItem that has a JobId != ObjectId.Empty.");
			Debug.Assert(!IsDirty, "IsDirty should be false as the constructor is exited.");
		}

		#endregion

		#region Public Properties

		//public ObservableCollection<JobTreeNode> Nodes => _root.Children;

		public Job CurrentJob
		{
			get => CurrentItem;
			set => CurrentItem = value;
		}

		public bool AnyJobIsDirty => AnyItemIsDirty;

		public JobNodeType? SelectedNode
		{
			get => _selectedItem;
			set
			{
				if (value != _selectedItem)
				{
					UpdateIsSelected(_selectedItem, false, UseRealRelationShipsToUpdateSelected, _root);
					_selectedItem = value;
					UpdateIsSelected(_selectedItem, true, UseRealRelationShipsToUpdateSelected, _root);
				}
			}
		}

		public bool UseRealRelationShipsToUpdateSelected { get; set; }

		#endregion

		#region Base Public Properties

		//public Job CurrentItem
		//{
		//	get => DoWithReadLock(() =>
		//	{
		//		var currentItem = _currentPath?.Item;
		//		if (currentItem == null)
		//		{
		//			Debug.WriteLine("WARNING: In CurrentItem:Getter, the CurrentPath is null. Returning the Home Item.");
		//			currentItem = Nodes[0].Item;
		//		}
		//		return currentItem;
		//	});

		//	set => DoWithWriteLock(() =>
		//	{
		//		if (value != _currentPath?.Item)
		//		{
		//			if (value != null)
		//			{
		//				if (!MoveCurrentTo(value, _root, out _currentPath))
		//				{
		//					Debug.WriteLine($"WARNING: Could not MoveCurrent to item: {value}.");
		//				}
		//			}
		//		}
		//	});
		//}

		// TODO: Consider having the ITreeItem<JobTreeItem,Job> Tree keep track of "CanGoBack" / "CanGoForward" as to make these real properties.
		//public bool CanGoBack
		//{
		//	get
		//	{
		//		_treeLock.EnterReadLock();

		//		try
		//		{
		//			return CanMoveBack(_currentPath);
		//		}
		//		finally
		//		{
		//			_treeLock.ExitReadLock();
		//		}
		//	}
		//}

		//public bool CanGoForward
		//{
		//	get
		//	{
		//		_treeLock.EnterReadLock();

		//		try
		//		{
		//			return CanMoveForward(_currentPath);
		//		}
		//		finally
		//		{
		//			_treeLock.ExitReadLock();
		//		}
		//	}
		//}

		//public bool IsDirty { get; set; }

		//public bool AnyItemIsDirty
		//{
		//	get
		//	{
		//		_treeLock.EnterReadLock();

		//		try
		//		{
		//			return GetNodes(_root).Any(x => x.IsDirty);
		//		}
		//		finally
		//		{
		//			_treeLock.ExitReadLock();
		//		}

		//	}
		//}

		#endregion

		#region Public Methods

		public bool RestoreBranch(ObjectId jobId)
		{
			Debug.WriteLine($"Restoring Branch: {jobId}.");

			// TODO: RestoreBranch does not support CanvasSizeUpdateJobs
			if (!TryFindPathById(jobId, _root, out var path))
			{
				throw new InvalidOperationException($"Cannot find job: {jobId} that is being restored.");
			}

			while(path != null && !(path.Count > 1))
			{
				path = path.GetParentPath();
			}

			if (path == null || !path.NodeSafe.IsParkedAlternate)
			{
				throw new InvalidOperationException("Cannot restore this branch, it is not a \"parked\" alternate.");
			}

			var result = RestoreBranch(path);

			return result;
		}

		public bool RestoreBranch(JobPathType path)
		{
			JobPathType newPath;

			if (path.ItemSafe.TransformType == TransformType.CanvasSizeUpdate)
			{
				var parentPath = path.GetParentPath()!;
				newPath = MakeBranchActive(parentPath).Combine(path.NodeSafe);
			}
			else
			{
				newPath = MakeBranchActive(path);
			}

			ExpandAndSetCurrent(newPath);
			_currentPath = newPath;
			IsDirty = true;
			return true;
		}

		public bool TryGetPreviousJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				job = null;
				return false;
			}

			var backPath = GetPreviousItemPath(_currentPath, GetPredicate(skipPanJobs));
			job = backPath?.Item;

			return job != null;
		}

		public bool MoveBack(bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var backPath = GetPreviousItemPath(_currentPath, GetPredicate(skipPanJobs));

			if (backPath != null)
			{
				_currentPath = backPath;
				ExpandAndSetCurrent(backPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetNextJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				job = null;
				return false;
			}

			var forwardPath = GetNextItemPath(_currentPath, GetPredicate(skipPanJobs));
			job = forwardPath?.Item;

			return job != null;
		}

		public bool MoveForward(bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var forwardPath = GetNextItemPath(_currentPath, GetPredicate(skipPanJobs));

			if (forwardPath != null)
			{
				_currentPath = forwardPath;
				ExpandAndSetCurrent(forwardPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy)
		{
			if (job.ParentJobId == null)
			{
				throw new ArgumentException("The job must have a non-null ParentJobId.", nameof(job));
			}

			_treeLock.EnterUpgradeableReadLock();

			try
			{
				if (TryFindParentPath(job, _root, out var parentPath))
				{
					JobTreeNode parentNode;

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						// The parentPath points to the "original job" for which the CanvasSizeUpdate job was created.
						// We need to get its parentPath to continue.
						parentNode = parentPath.GetParentPath()!.NodeSafe;
					}
					else
					{
						parentNode = parentPath.NodeSafe;
					}

					var proxyJobTreeItem = parentNode.AlternateDispSizes?.FirstOrDefault(x => x.Item.CanvasSizeInBlocks == canvasSizeInBlocks);
					proxy = proxyJobTreeItem?.Item;
					return proxy != null;
				}
				else
				{
					proxy = null;
					return false;
				}
			}
			finally
			{
				_treeLock.ExitUpgradeableReadLock();
			}
		}

		public override JobPathType Add(Job item, bool selectTheAddedItem)
		{
			JobPathType newPath;
			_treeLock.EnterWriteLock();

			try
			{
				newPath = AddInternal(item, currentBranch: _root);
				IsDirty = true;
			}
			finally
			{
				_treeLock.ExitWriteLock();
			}

			if (selectTheAddedItem)
			{
				ExpandAndSetCurrent(newPath);
				_currentPath = newPath;
			}

			return newPath;
		}

		#endregion

		//#region Base Public Methods

		//public JobPathType Add(Job item, bool selectTheAddedItem)
		//{
		//	_treeLock.EnterWriteLock();

		//	JobPathType newPath;

		//	try
		//	{
		//		newPath = AddInternal(item, currentBranch: _root);
		//		IsDirty = true;
		//	}
		//	finally
		//	{
		//		_treeLock.ExitWriteLock();
		//	}

		//	if (selectTheAddedItem)
		//	{
		//		ExpandAndSetCurrent(newPath);
		//		_currentPath = newPath;
		//	}

		//	return newPath;
		//}

		//public bool RemoveBranch(ObjectId id)
		//{
		//	// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.
		//	if (!TryFindPathById(id, _root, out var path))
		//	{
		//		return false;
		//	}

		//	var result = RemoveBranch(path);
		//	return result;
		//}

		//public bool RemoveBranch(JobPathType path)
		//{
		//	// TODO: Implement JobTree::RemoveBranch
		//	return true;
		//}

		//public bool TryGetPreviousItem([MaybeNullWhen(false)] out Job item, Func<JobNodeType, bool>? predicate = null)
		//{
		//	if (_currentPath == null)
		//	{
		//		item = null;
		//		return false;
		//	}

		//	var backPath = GetPreviousItemPath(_currentPath, predicate);
		//	item = backPath?.Item;

		//	return item != null;
		//}

		//public bool MoveBack(Func<JobNodeType, bool>? predicate = null)
		//{
		//	if (_currentPath == null)
		//	{
		//		return false;
		//	}

		//	var backPath = GetPreviousItemPath(_currentPath, predicate);

		//	if (backPath != null)
		//	{
		//		_currentPath = backPath;
		//		ExpandAndSetCurrent(backPath);
		//		return true;
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

		//public bool TryGetNextItem([MaybeNullWhen(false)] out Job item, Func<JobNodeType, bool>? predicate = null)
		//{
		//	if (_currentPath == null)
		//	{
		//		item = null;
		//		return false;
		//	}

		//	var forwardPath = GetNextItemPath(_currentPath, predicate);
		//	item = forwardPath?.Item;

		//	return item != null;
		//}

		//public bool MoveForward(Func<JobNodeType, bool>? predicate = null)
		//{
		//	if (_currentPath == null)
		//	{
		//		return false;
		//	}

		//	var forwardPath = GetNextItemPath(_currentPath, predicate);

		//	if (forwardPath != null)
		//	{
		//		_currentPath = forwardPath;
		//		ExpandAndSetCurrent(forwardPath);
		//		return true;
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

		//#endregion

		#region Public Methods - Collection

		//JobPathType? IJobTree.GetCurrentPath()
		//{
		//	var result = GetCurrentPath();

		//	return result;
		//}

		//JobPathType? IJobTree.GetPath(ObjectId jobId)
		//{
		//	var result = GetPath(jobId);
		//	return result;
		//}

		//public IEnumerable<Job> GetJobs() => GetItems();

		//public Job? GetJob(ObjectId id) => GetItem(id);

		//public List<Job>? GetJobAndDescendants(ObjectId id) => GetItemAndDescendants(id);

		public Job? GetParent(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindItem(job.ParentJobId.Value, _root, out var result);
				return result;
			}
		}

		public JobPathType? GetParentPath(Job job, JobBranchType currentBranch)
		{
			return TryFindParentPath(job, currentBranch, out var path) ? path : null;
		}

		public bool TryFindParentPath(Job job, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		{
			path = currentBranch.GetCurrentPath();

			var parentId = job.ParentJobId;
			if (parentId == null)
			{
				return false;
			}

			if (TryFindPathById(parentId.Value, currentBranch, out var path1))
			{
				path = path1;
				return true;
			}
			else
			{
				return false;
			}
		}

		public Job? GetParentItem(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindItem(job.ParentJobId.Value, _root, out var result);
				return result;
			}
		}

		#endregion

		//#region Base Public Methods - Collection

		//public JobPathType? GetCurrentPath()
		//{
		//	_treeLock.EnterReadLock();

		//	try
		//	{
		//		return _currentPath == null ? null : _currentPath;
		//	}
		//	finally
		//	{
		//		_treeLock.ExitReadLock();
		//	}
		//}

		//public JobPathType? GetPath(ObjectId id)
		//{
		//	_treeLock.EnterReadLock();

		//	try
		//	{
		//		return GetPathById(id, _root);
		//	}
		//	finally
		//	{
		//		_treeLock.ExitReadLock();
		//	}
		//}

		//public IEnumerable<Job> GetItems()
		//{
		//	_treeLock.EnterReadLock();

		//	try
		//	{
		//		var result = GetItems(_root);
		//		return result;
		//	}
		//	finally
		//	{
		//		_treeLock.ExitReadLock();
		//	}
		//}

		//public Job? GetItem(ObjectId id)
		//{
		//	_ = TryFindItem(id, _root, out var result);
		//	return result;
		//}

		//public Job? GetParentItem(JobTreeNode node)
		//{
		//	if (node.ParentId == null)
		//	{
		//		return null;
		//	}
		//	else
		//	{
		//		_ = TryFindItem(node.ParentId.Value, _root, out var result);
		//		return result;
		//	}
		//}

		//public List<Job>? GetItemAndDescendants(ObjectId id)
		//{
		//	_treeLock.EnterReadLock();

		//	try
		//	{
		//		List<Job>? result;

		//		if (TryFindPathById(id, _root, out var path))
		//		{
		//			result = new List<Job> { path.Node.Item };
		//			result.AddRange(GetItems(path));
		//		}
		//		else
		//		{
		//			result = null;
		//		}

		//		return result;
		//	}
		//	finally
		//	{
		//		_treeLock.ExitReadLock();
		//	}
		//}

		//#endregion

		#region Private Add Methods

		private JobPathType AddInternal(Job job, JobBranchType currentBranch)
		{
			JobPathType newPath;
			
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				newPath = AddCanvasSizeUpdateJob(job, currentBranch);
			}
			else
			{
				if (TryFindParentPath(job, currentBranch, out var parentPath))
				{
					newPath = AddAtParentPath(job, parentPath);
				}
				else
				{
					throw new InvalidOperationException($"Cannot find ... FIX ME FIX ME.");
				}
			}

			return newPath;
		}

		private JobPathType AddCanvasSizeUpdateJob(Job job, JobBranchType currentBranch)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's TransformType is {job.TransformType}.");
			}

			if (job.ParentJobId == null)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's parentJobId is null.");
			}

			if (TryFindParentPath(job, currentBranch, out var parentPath))
			{
				var parentNode = parentPath.NodeSafe;

				var canvasSizeUpdateNode = parentNode.AddCanvasSizeUpdateJob(job);
				var newPath = parentPath.Combine(canvasSizeUpdateNode);

				return newPath;
			}
			else
			{
				throw new InvalidOperationException($"Cannot find ... FIX ME FIX ME.");
			}

		}

		private JobPathType AddAtParentPath(Job job, JobPathType parentPath)
		{
			JobPathType newPath;

			// This is the JobTreeItem for the new Job's real parent.
			var parentNode = parentPath.NodeSafe;

			// Add the new Job to the list of it's parent's "real" children.
			// The index is the position of the new job among its siblings which are sorted by the CreatedDate, ascending.
			var index = parentNode.AddRealChild(job);

			// Get the job already in the tree for which the job being added will directly follow.
			JobPathType preceedingPath;

			if (index == 0)
			{
				// Find the sibling of the parent, that comes just before the parent.
				if (parentPath.TryGetParentPath(out var grandparentPath))
				{
					var grandparentId = grandparentPath.Item!.Id;
					var grandparentBranch = grandparentPath.GetParentBranch();

					if (TryFindPathById(grandparentId, grandparentBranch, out var realGrandparentPath))
					{
						preceedingPath = realGrandparentPath;
					}
					else
					{
						throw new InvalidOperationException("Cant find a child of a known parent.");
					}
				}
				else
				{
					preceedingPath = parentPath;
				}
			}
			else
			{
				var preceedingJob1 = parentNode.RealChildJobs.Values[index - 1];
				preceedingPath = GetPath(preceedingJob1, parentPath.GetParentBranch());
			}

			var preceedingJob = preceedingPath.Item;

			// Does the preceeding sibling job (in date order) move the map to a different Zoom level.
			var addingJobAsAnAlt = preceedingJob!.TransformType is TransformType.ZoomIn or TransformType.ZoomOut;

			if (addingJobAsAnAlt)
			{
				// Add the new node as a Parked ALT.
				newPath = AddAsParkedAlt(job, preceedingPath!);
			}
			else
			{
				// Add the new node in-line after the preceeding ALT Job
				newPath = AddAfter(job, preceedingPath, parentPath);
			}

			return newPath;
		}

		private JobPathType AddAsParkedAlt(Job job, JobPathType preceedingPath)
		{
			JobPathType activeAltPath;

			if (preceedingPath.NodeSafe.IsActiveAlternate)
			{
				// The preceeding node is the Active ALT.
				// Add the new job as a sibling to the branch on which my real parent's JobTreeItem was found (in sort order by date created.)
				activeAltPath = preceedingPath;
			}
			else if (preceedingPath.NodeSafe.IsParkedAlternate)
			{
				// The parent of the preceeding node is the Active ALT
				var parkedParentPath = preceedingPath.GetParentPath()!;
				activeAltPath = parkedParentPath;
			}
			else
			{
				// The preceeding node has not yet been made an Alternate.
				Debug.WriteLine($"Found a Job that is a new Alternate. Marking existing node: {preceedingPath.LastTerm} as the Active ALT.");
				preceedingPath.NodeSafe.IsActiveAlternate = true;
				activeAltPath = preceedingPath;
			}

			Debug.WriteLine($"Adding job: {job.Id}, as a Parked ALT to Active ALT: {activeAltPath.NodeSafe.Item.Id}.");

			var parkedAltPath = AddJob(job, activeAltPath);

			// TODO: See if we can avoid making the just added job be on the 'Main' branch.
			var newPath = MakeBranchActive(parkedAltPath);

			return newPath;
		}

		private JobPathType AddAfter(Job job, JobPathType preceedingPath, JobPathType parentPath)
		{
			JobPathType newPath;

			if (preceedingPath.NodeSafe.IsParkedAlternate)
			{
				if (job.ParentJobId == preceedingPath.NodeSafe.Id) // preceedingPath.LastTerm == parentPath.LastTerm)
				{
					// Add the job as a child of the Parked Alt
					newPath = AddJob(job, preceedingPath);
				}
				else
				{
					newPath = AddInLine(job, preceedingPath.GetParentPath()!);
				}
			}
			else
			{
				// Add the job as a sibling of its parent 
				newPath = AddInLine(job, preceedingPath);
			}

			return newPath;
		}

		private JobPathType AddInLine(Job job, JobPathType parentPath)
		{
			Debug.WriteLine($"Adding job: {job.Id}, in-line after: {parentPath.NodeSafe.Item.Id}.");

			var grandparentBranch = GetGrandParentBranch(parentPath);

			Debug.Assert(grandparentBranch.IsEmpty || !grandparentBranch.LastTerm?.IsActiveAlternate == true, "AddJobInLine is adding a job to an Active Alt node.");
			var result = AddJob(job, grandparentBranch);

			return result;
		}

		private JobBranchType GetGrandParentBranch(JobPathType parentPath)
		{
			var grandparentPath = parentPath.GetParentBranch();

			if (parentPath.NodeSafe.IsActiveAlternate)
			{
				// The real grandparent branch is the Active Alternate's Parent
				Debug.WriteLine("The grandparentBranch is being set to the great-grandparent of the parentPath -- the parentPath points to an Active Alternate.");
				return grandparentPath.GetParentBranch();
			}
			else
			{
				return grandparentPath;
			}
		}

		private JobPathType AddJob(Job job, JobBranchType parentBranch)
		{
			var parentNode = parentBranch.GetNodeOrRoot();
			var newNode = parentNode.AddItem(job);

			if (parentNode.IsActiveAlternate)
			{
				newNode.IsParkedAlternate = true;
			}

			var result = parentBranch.Combine(newNode);

			return result;
		}

		#endregion

		#region Private Branch Methods

		/// <summary>
		///	1.  Get the jobTreeItem
		///	2.  Find its Parent(The active alternate that will be parked)
		///	3.  Remove the jobTreeItem from its Parent
		///	4.  Get a list of our children and remove them.
		///	
		///			-- SwitchAltBranches --
		///	5.  Get a list of all items from the main branch starting with the item just after the Active Alt, aka Parent
		///	6.  The current Alt becomes our child -- [The Alternate currently in the main trunk]
		///	7.  Move all children of this first child[The other alternates] and make them our children
		///	8.  The items that were identified in step 5 are then added as children once current, now parked Alt
		///			-- SwitchAltBranches
		///	
		///	9.  Move us from being a child of the(once Active) now Parked Alt to be a child of the Grandparent
		///	10. Move our children to the Grandparent(following us.)
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private JobPathType MakeBranchActive(JobPathType path)
		{
			if (path.IsEmpty)
			{
				throw new ArgumentException("Path cannot be empty.");
			}

			var parkedAltNode = path.NodeSafe;

			if (parkedAltNode.Item.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("MakeActiveBranch does not support CanvasSizeUpdates.");
			}

			var activeAltNode = parkedAltNode.ParentNode!;

			if (activeAltNode is null)
			{
				throw new InvalidOperationException("Call to MakeBranchActive found no Active Alt parent of the Parked Alt being made active.");
			}

			// The parked ALT's children contains all of the job following the parked ALT.
			var ourChildern = new List<JobNodeType>(parkedAltNode.Children);

			SwitchAltBranches(parkedAltNode, activeAltNode.Node);

			var parentPath = path.GetParentPath()!;
			var grandparentItem = parentPath.GetParentNodeOrRoot();
			var grandparentBranch = parentPath.GetParentBranch();

			// Move the once Parked, now Active Alt node to the grandparent
			_ = parkedAltNode.Move(grandparentItem);
			var newPath = grandparentBranch.Combine(parkedAltNode);

			// Move each job following the once Parked, now Active Alt node to the grandparent
			for (var i = 0; i < ourChildern.Count; i++)
			{
				var child = ourChildern[i];
				_ = child.Move(grandparentItem);
			}

			return newPath;
		}

		/// <summary>
		/// 1. Get a list of the Jobs following the Active Alt
		/// 2. Move the Active Alt from its parent(main trunk) to become a child of the node becoming Active, i.e., the Parked Alt node.
		/// 3. Move each child of the Active Alt to become a child of the node being parked -- excluding the node being parked.
		/// 4. Move each of the nodes following the (once) ActiveAlt to become a child of the node being parked; i.e., the (once) Active Alt node.
		/// </summary>
		/// <param name="parkedAlt"></param>
		/// <param name="activeAlt"></param>
		private void SwitchAltBranches(JobTreeNode parkedAlt, JobTreeNode activeAlt)
		{
			/* 	
				Nodes have children in three cases:
				1. The root node's children contain the list of jobs currently in play. Some of these may be active alternates.
				2. An active alternate node's children contains all of the other alternates currently not in play, aka the "parked alternates.
				3. A parked alternate node's children contains the jobs that follow this alternate that would also be made active.
			
				An active alternate may be "parked" if its part of a trunk that is parked, if that trunk is made active, this will be the active node.
			*/

			if (!parkedAlt.IsParkedAlternate)
			{
				throw new InvalidOperationException("The newNode being Switched to become the Active ALT is not a Parked ALT.");
			}

			if (!activeAlt.IsActiveAlternate)
			{
				throw new InvalidOperationException("The oldNode being Switched out to become a Parked ALT is not the Active ALT.");
			}

			var parentNode = activeAlt.ParentNode;

			if (parentNode is null)
			{
				throw new InvalidOperationException("Call to SwitchAltBranch found the Active ALT to have no parent.");
			}

			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(activeAlt);

			//var strSiblings = string.Join("; ", siblings.Select(x => x.IdAndParentId));
			//var strNewNodeChildren = string.Join("; ", newNode.Children.Select(x => x.IdAndParentId));
			//Debug.WriteLine($"CurrentAlt: {currentAlt.Job.Id}, CurrentAlt ParentNode: {parentNode.Job.Id}, Siblings = {strSiblings}. current Pos: {currentPosition}, NewNode: {newNode.IdAndParentId}, NewNode Children = {strSiblings}.");

			Debug.WriteLine($"Switching Branches. CurrentAlt: {activeAlt.Item.Id}, CurrentAlt ParentNode: {parentNode.Item.Id}. Current Pos: {currentPosition}, NewNode: {parkedAlt.IdAndParentId}.");

			// Get a list of the items after the current position of the parent. We will use this later to add them to the node being parked.
			var jobsFollowingActiveAlt = siblings.Skip(currentPosition + 1).ToList();

			// Move the Active Alt node from the grandparent node to be a child of the node becoming the Active Alt node.
			// The new Active Alt node stores all of the parked ALTs in its list of Children.
			_ = activeAlt.Move(parkedAlt);

			// Move all children of the Active Alt node to be children of the node becoming the Active Alt node. Don't move the node becoming the Active Alt node.
			var otherAlts = new List<JobNodeType>(activeAlt.Children);
			for (var i = 0; i < otherAlts.Count; i++)
			{
				var otherAlt = otherAlts[i];
				if (otherAlt != parkedAlt)
				{
					_ = otherAlt.Move(parkedAlt);
				}
			}
			// The job being added is the active alternate among it's peer alternates..
			activeAlt.IsActiveAlternate = false;
			activeAlt.IsParkedAlternate = true;
			parkedAlt.IsActiveAlternate = true;
			parkedAlt.IsParkedAlternate = false;

			// All of the jobs that followed the Active Alt node are now added to the Parked Alt node.
			var newlyDesignatedParkedAlt = activeAlt;

			for (var i = 0; i < jobsFollowingActiveAlt.Count; i++)
			{
				var successor = jobsFollowingActiveAlt[i];
				_ = successor.Move(newlyDesignatedParkedAlt);
			}
		}

		private JobPathType SelectMostRecentAlternate(JobPathType currentAltPath)
		{
			var parkedAlts = currentAltPath.NodeSafe.Children;
			var mostRecentParkedAlt = parkedAlts.Aggregate((i1, i2) => (i1.Item.CompareTo(i2.Item) > 0) ? i1 : i2);

			//var result = currentAltPath.Combine((JobTreeNode)mostRecentParkedAlt);
			var result = currentAltPath.Combine(mostRecentParkedAlt.Node);

			return result;
		}

		#endregion

		#region Private Load and Export Job Methods

		//private ITreeBranch<JobTreeItem, Job> CreateTree(List<Job> jobs, bool checkHomeJob, out JbType currentPath)
		//{
		//	Job homeJob;

		//	if (checkHomeJob)
		//	{
		//		// Make it an error for their not be one and only one Job of type Home.
		//		homeJob = GetHomeJob(jobs);
		//	}
		//	else
		//	{
		//		// Use the first job, unconditionally
		//		homeJob = jobs.Take(1).First();
		//	}

		//	if (homeJob.ParentJobId != null)
		//	{
		//		Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has a non-null ParentJobId. Setting the ParentJobId to null.");
		//		homeJob.ParentJobId = null;
		//	}

		//	if (homeJob.TransformType != TransformType.Home)
		//	{
		//		Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has TransformType of {homeJob.TransformType}. Expecting the TransformType to be {nameof(TransformType.Home)}.");
		//	}



		//	var tree = new JobTreeItem(homeJob, null, false, false);
		//	var homeItem = tree.AddItem(homeJob);
		//	tree.RealChildJobs.Add(homeJob.Id, homeJob);
		//	currentPath = new JbType(tree, homeItem);
		//	var root = currentPath.GetRoot();

		//	return root;



		//	// CreateRoot returns a JbType pointing to the homeJob.
		//	//var root = JobTreeItem.CreateRoot(homeJob, out currentPath);

		//	//if (numberOfJobsWithNullParentId > 1)
		//	//{
		//	//	Debug.WriteLine($"Loading {jobs.Count()} jobs.");
		//	//	_xroot = BuildTree(jobs, out _xcurrentPath);
		//	//}
		//	//else
		//	//{
		//	//	Debug.WriteLine($"Loading {jobs.Count()} jobs from an older project.");
		//	//	_xroot = BuildTreeForOldPro(jobs, out _xcurrentPath);
		//	//}


		//	//// Have BuildTree start with the homeJob, and not the root, so that it will not add the Home Job a second time.
		//	//currentPath = BuildTree(jobs, currentPath);

		//	return root;
		//}

		private void ReportInput(IList<Job> jobs)
		{
			Debug.WriteLine("INPUT Report");
			Debug.WriteLine("Id\t\t\t\tParentId\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

			if (homeJob != null)
			{
				var strParentJobId = homeJob.ParentJobId.HasValue ? homeJob.ParentJobId.ToString() : "null";
				Debug.WriteLine($"{homeJob.Id}\t{strParentJobId}\t{homeJob.DateCreated}\t{homeJob.TransformType}\t{homeJob.Id.Timestamp}");
			}
			else
			{
				Debug.WriteLine("No Home Node Found.");
			}

			var wlist = jobs.Where(x => x != homeJob).OrderBy(x => x.Id.ToString()).ToList();

			foreach (var j in wlist)
			{
				var strParentJobId = j.ParentJobId.HasValue ? j.ParentJobId.ToString() : "null";
				Debug.WriteLine($"{j.Id}\t{strParentJobId}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		private void ReportOutput(JobBranchType root, JobPathType? currentPath)
		{
			Debug.WriteLine($"OUTPUT Report for currentPath: {currentPath}");
			Debug.WriteLine("Id\t\t\t\t\t\t\tParentId\t\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var jwps = GetJobsWithParentage(root);

			foreach (var jwp in jwps)
			{
				var j = jwp.Item1;
				var p = jwp.Item2;
				Debug.WriteLine($"{j.Id}\t{p?.Id.ToString() ?? "null\t\t\t\t\t"}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		private JobPathType? PopulateTree(IList<Job> jobs, JobBranchType currentBranch)
		{
			var visited = 1;
			LoadChildItems(jobs, currentBranch, ref visited);

			if (visited != jobs.Count)
			{
				Debug.WriteLine($"WARNING: Only {visited} jobs out of {jobs.Count} were included during build.");
			}

			// Use the very top of the tree. The value of current branch given to this method may be pointing to the HomeNode instead of to the root.
			var tree = currentBranch.GetRoot();

			_ = MoveCurrentTo(jobs[0], tree, out var path);

			return path;
		}

		private static Job GetHomeJob(IList<Job> jobs, bool checkHomeJob)
		{
			if (!jobs.Any())
			{
				throw new ArgumentException("The list of jobs cannot be empty when constructing a JobTree.", nameof(jobs));
			}

			var numberOfJobsWithNoParent = jobs.Count(x => !x.ParentJobId.HasValue && x.TransformType != TransformType.CanvasSizeUpdate);

			if (numberOfJobsWithNoParent > 1)
			{
				Debug.WriteLine($"WARNING: Found {numberOfJobsWithNoParent} jobs with a null ParentJobId. Expecting exactly one.");
			}

			Job? homeJob;

			if (!checkHomeJob)
			{
				// Use the first job, unconditionally
				homeJob = jobs.Take(1).First();
			}
			else
			{
				// Make it an error for their not be one and only one Job of type Home.
				homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

				if (homeJob == null)
				{
					throw new InvalidOperationException("There is no Job with TransformType = Home.");
				}
			}

			if (homeJob.ParentJobId != null)
			{
				Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has a non-null ParentJobId. Setting the ParentJobId to null.");
				homeJob.ParentJobId = null;
			}

			if (homeJob.TransformType != TransformType.Home)
			{
				Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has TransformType of {homeJob.TransformType}. Expecting the TransformType to be {nameof(TransformType.Home)}.");
			}

			return homeJob;
		}

		private void LoadChildItems(IList<Job> jobs, JobBranchType currentBranch, ref int visited)
		{
			var currentPath = currentBranch.GetCurrentPath();

			var childJobs = GetChildren(currentPath, jobs);
			foreach (var job in childJobs)
			{
				visited++;

				if (job.TransformType == TransformType.CanvasSizeUpdate)
				{
					Debug.Assert(!jobs.Any(x => x.ParentJobId == job.Id), "Found a CanvasSizeUpdateJob that has children.");
					_ = AddCanvasSizeUpdateJob(job, currentBranch);
				}
				else
				{
					//var path = AddInternal(job, currentBranch);
					var path = AddInternal(job, currentBranch);

					ValidateAddInternal(job, currentBranch);

					LoadChildItems(jobs, path, ref visited);
				}
			}
		}

		[Conditional("DEBUG")]
		private void ValidateAddInternal(Job job, JobBranchType currentBranch)
		{
			if (!TryFindPath(job, currentBranch.GetRoot(), out _))
			{
				throw new InvalidOperationException("Cannot find job just loaded.");
			}
		}

		private IList<Job> GetChildren(JobPathType? currentPath, IList<Job> jobs)
		{
			var parentJobId = currentPath == null ? (ObjectId?)null : currentPath.Item!.Id;
			parentJobId = parentJobId == ObjectId.Empty ? null : parentJobId;
			var result = jobs.Where(x => x.ParentJobId == parentJobId).OrderBy(x => x.Id.Timestamp).ToList();

			return result;
		}

		//private List<Job> PrepareRemaingJobs(IList<Job> jobs, Job homeJob)
		//{
		//	//foreach (var job in jobs)
		//	//{
		//	//	if (job.ParentJobId == null)
		//	//	{
		//	//		job.ParentJobId = homeJob.Id;
		//	//	}

		//	//	//if (job.TransformType == TransformType.CanvasSizeUpdate)
		//	//	//{
		//	//	//	job.TransformType = TransformType.ZoomOut;
		//	//	//}
		//	//}

		//	var remainingJobs = jobs.Where(x => x != homeJob).OrderBy(x => x.Id.ToString()).ToList();

		//	return remainingJobs;
		//}

		private List<Tuple<Job, Job?>> GetJobsWithParentage(JobBranchType currentBranch)
		{
			var result = new List<Tuple<Job, Job?>>();

			foreach (var child in currentBranch.GetNodeOrRoot().Children)
			{
				result.Add(new Tuple<Job, Job?>(child.Item, currentBranch.Item));
				if (child.Node .AlternateDispSizes != null)
				{
					result.AddRange
						(
							child.Node.AlternateDispSizes.Select(x => new Tuple<Job, Job?>(x.Item, child.Item))
						);
				}

				var jobList = GetJobsWithParentage(currentBranch.Combine(child.Node));
				result.AddRange(jobList);
			}

			return result;
		}


		private bool TryFindCanvasSizeUpdatePath(Job job, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		{
			var parentJobId = job.ParentJobId;

			if (parentJobId == null)
			{
				throw new ArgumentException("When finding a CanvasSizeUpdate, the job must have a parent.", nameof(job));
			}

			if (TryFindPathById(parentJobId.Value, currentBranch, out var parentPath))
			{
				var foundNode = parentPath.NodeSafe.AlternateDispSizes?.FirstOrDefault(x => x.Item.Id == job.Id);
				path = foundNode == null ? null : parentPath.Combine(foundNode);
				return path != null;
			}
			else
			{
				path = null;
				return false;
			}
		}

		#endregion

		//#region Base Private Load and Export Item Methods

		////protected ITreeBranch<U,V> CreateTree(List<V> items, out ITreePath<U,V>? currentPath)
		////{
		////	V homeItem = items.Take(1).First();
		////	var root = new TreeBranch<U, V>(homeItem);
		////	currentPath = root.GetCurrentPath();
		////	return root;
		////}

		//private IList<Job> GetItems(JobBranchType currentBranch)
		//{
		//	// TODO: Consider implementing an IEnumerator<ITreeItem<U,V> for the Tree class.
		//	var result = new List<Job>();

		//	//foreach (var child in currentBranch.GetNodeOrRoot().Children)
		//	//{
		//	//	result.Add(child.Item);

		//	//	var nodeList = GetItems(currentBranch.Combine(child));
		//	//	result.AddRange(nodeList);
		//	//}

		//	return result;
		//}

		//private IList<JobNodeType> GetNodes(JobBranchType currentBranch)
		//{
		//	// TODO: Consider implementing an IEnumerator<ITreeItem<U,V> for the Tree class.
		//	var result = new List<JobNodeType>();

		//	//foreach (var child in currentBranch.GetNodeOrRoot().Children)
		//	//{
		//	//	result.Add(child);

		//	//	var nodeList = GetNodes(currentBranch.Combine(child));
		//	//	result.AddRange(nodeList);
		//	//}

		//	return result;
		//}

		//private List<Tuple<JobTreeNode, JobTreeNode?>> GetNodesWithParentage(JobBranchType currentBranch)
		//{
		//	var result = new List<Tuple<JobTreeNode, JobTreeNode?>> ();

		//	//foreach (var child in currentBranch.GetNodeOrRoot().Children)
		//	//{
		//	//	result.Add(new Tuple<ITreeItem<V>, ITreeItem<V>?>(child, (U?) child.ParentNode));

		//	//	var nodeList = GetNodesWithParentage(currentBranch.Combine(child));
		//	//	result.AddRange(nodeList);
		//	//}

		//	return result;
		//}

		//#endregion

		//#region Base Private Collection Methods, With Support for CanvasSizeUpdates

		//protected bool TryFindItem(ObjectId id, JobBranchType currentBranch, [MaybeNullWhen(false)] out Job item)
		//{
		//	item = GetPathById(id, currentBranch)?.Item;
		//	return item != null;
		//}

		//protected JobPathType? GetPathById(ObjectId id, JobBranchType currentBranch)
		//{
		//	return TryFindPathById(id, currentBranch, out var path) ? path : null;
		//}

		//protected JobPathType GetPath(Job item, JobBranchType currentBranch)
		//{
		//	return TryFindPath(item, currentBranch, out var path)
		//		? path
		//		: throw new InvalidOperationException($"Cannot find Item: {item} in the Tree.");
		//}

		//protected bool TryFindPath(Job item, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		//{
		//	path = currentBranch.GetCurrentPath();
		//	return (item.Equals(path?.LastTerm)) || TryFindPathInternal(item, currentBranch, out path);
		//}

		//protected bool TryFindPathById(ObjectId id, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		//{
		//	path = currentBranch.GetCurrentPath();
		//	return (id == path?.LastTerm?.Id) || TryFindPathByIdInternal(id, currentBranch, out path);
		//}

		//protected bool TryFindPathByIdInternal(ObjectId id, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		//{
		//	//var tc = currentBranch.Children;
		//	if (NodeContainsItem(currentBranch, x => x.Id == id, out path))
		//	{
		//		return true;
		//	}

		//	var node = currentBranch.Node;

		//	foreach (JobNodeType child in node.Children)
		//	{
		//		//var cPath = currentBranch.Combine((JobTreeNode)child);
		//		var cPath = currentBranch.Combine(child.Node);

		//		//ITreeBranch<JobTreeItem,Job> cb = cPath;
		//		//var testBranchChildren = cb.Children;
		//		//var testPathChildren = cPath.Children;
		//		//var testBranchItem = cb.GetItemOrRoot();
		//		//var testPathItem = cPath.Item;

		//		//if (TryFindPathByIdInternal(id, currentBranch.Combine((JobTreeNode)child), out var localPath))
		//		if (TryFindPathByIdInternal(id, currentBranch.Combine(child.Node), out var localPath))
		//		{
		//			path = currentBranch.Combine(localPath);
		//			return true;
		//		}
		//	}

		//	path = null;
		//	return false;
		//}

		//protected bool TryFindPathInternal(Job item, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		//{
		//	//var tc = currentBranch.Children;
		//	if (NodeContainsItem(currentBranch, x => x.Item.Equals(item), out path))
		//	{
		//		return true;
		//	}

		//	var node = currentBranch.Node;

		//	foreach (JobNodeType child in node.Children)
		//	{
		//		var cPath = currentBranch.Combine(child.Node);

		//		//ITreeBranch<JobTreeItem,Job> cb = cPath;
		//		//var testBranchChildren = cb.Children;
		//		//var testPathChildren = cPath.Children;
		//		//var testBranchItem = cb.GetItemOrRoot();
		//		//var testPathItem = cPath.Item;

		//		//if (TryFindPathInternal(item, currentBranch.Combine((JobTreeNode)child), out var localPath))
		//		if (TryFindPathInternal(item, currentBranch.Combine(child.Node), out var localPath))
		//		{
		//			path = currentBranch.Combine(localPath);
		//			return true;
		//		}
		//	}

		//	path = null;
		//	return false;
		//}

		//protected bool NodeContainsItem(JobBranchType branch, Func<JobNodeType, bool> predicate, [MaybeNullWhen(false)] out JobPathType path)
		//{
		//	var foundNode = branch.GetNodeOrRoot().Children.FirstOrDefault(predicate);
		//	//var foundNode = branch.Children.FirstOrDefault(predicate);
		//	path = foundNode == null ? null : branch.Combine(foundNode.Node);
		//	return path != null;
		//}

		//#endregion

		#region Private Navigate Methods

		private void UpdateIsSelected(JobNodeType? jobTreeItem, bool isSelected, bool useRealRelationships, JobBranchType startPos)
		{
			if (useRealRelationships)
			{
				UpdateIsSelectedReal(jobTreeItem, isSelected, startPos);
			}
			else
			{
				UpdateIsSelectedLogical(jobTreeItem, isSelected, startPos);
			}
		}

		private void UpdateIsSelectedLogical(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.Node.IsSelected = isSelected;

				if (!TryFindPath(jobTreeItem.Item, startPos, out var path))
				{
					return;
				}

				//var ancestors = jobTreeItem.GetAncestors();
				//var path2 = _xroot.Combine(ancestors);
				//var strPath1 = string.Join("; ", path.Terms.Select(x => x.JobId.ToString()));
				//var strPath2 = string.Join("; ", path2.Terms.Select(x => x.JobId.ToString()));
				//Debug.WriteLine($"Path: {strPath1}\nPath2: {strPath2}.");

				var strPath1 = string.Join("\n\t", path.Terms.Select(x => $"Id:{x.Item.Id}, ParentId:{x.Item.ParentJobId}, Alt:{x.IsActiveAlternate}, Prk:{x.IsParkedAlternate}"));
				Debug.WriteLine($"Path: {strPath1}.");

				var backPath = GetPreviousItemPath(path, GetPredicate(skipPanJobs: true));

				if (backPath == null)
				{
					return;
				}

				// The backPath points to the first job previous to the give job that has a TransformType of Zoom-In or Zoom-Out or Home.

				// Set the parent node's IsParentOfSelected
				backPath.NodeSafe.IsParentOfSelected = isSelected;

				//// Set each sibling node's IsSiblingSelected
				//foreach (var siblingItem in parentNode.Children)
				//{
				//	siblingItem.IsSiblingOfSelected = isSelected;
				//}

				if (jobTreeItem.Node.RealChildJobs.Any())
				{
					// Use the prior job's parent path to start the search for each child.
					var parentBranch = backPath.GetParentBranch();

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
					{
						if (TryFindPath(realChildJob, parentBranch, out var childPath))
						{
							childPath.NodeSafe.IsChildOfSelected = isSelected;
						}
					}
				}
				else
				{
					var forwardPath = GetNextItemPath(path, GetPredicate(skipPanJobs: true));

					if (forwardPath != null)
					{
						forwardPath.NodeSafe.IsChildOfSelected = true;
					}
				}

			}
		}

		private void UpdateIsSelectedReal(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.Node.IsSelected = isSelected;

				if (TryFindParentPath(jobTreeItem.Item, startPos, out var realParentPath))
				{
					var realParentNode = realParentPath.NodeSafe;

					// Set the parent node's IsParentOfSelected
					realParentNode.IsParentOfSelected = isSelected;

					// Use the logical grandparent path (or root) to start the search for each sibling
					var grandparentBranch = realParentPath.GetParentBranch();

					// Set each sibling node's IsSiblingSelected
					foreach (var realSiblingJob in realParentNode.RealChildJobs.Values)
					{
						if (TryFindPath(realSiblingJob, grandparentBranch, out var siblingPath))
						{
							siblingPath.NodeSafe.IsSiblingOfSelected = isSelected;
						}
					}

					// Use the real parent path to start the search for each child.
					var logicalParentBranch = realParentPath;

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
					{
						if (TryFindPath(realChildJob, logicalParentBranch, out var childPath))
						{
							childPath.NodeSafe.IsChildOfSelected = isSelected;
						}
					}

				}
			}
		}

		#endregion

		#region Base Private Navigate Methods

		protected JobPathType? GetNextItemPath(JobPathType path, Func<JobNodeType, bool>? predicate = null)
		{
			var currentItem = path.NodeSafe;

			JobPathType? result;

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (TryGetNextNode(siblings, currentPosition, out var nextNode, predicate))
			{
				//The new item will be a sibling of the current item
				result = path.Combine(nextNode.Node);
			}
			else
			{
				result = null;
			}

			return result;
		}

		private bool TryGetNextNode(IList<JobNodeType> nodes, int currentPosition, [MaybeNullWhen(false)] out JobNodeType nextNode, Func<JobNodeType, bool>? predicate = null)
		{
			if (predicate != null)
			{
				nextNode = nodes.Skip(currentPosition + 1).FirstOrDefault(predicate);
			}
			else
			{
				nextNode = nodes.Skip(currentPosition + 1).FirstOrDefault();
			}

			return nextNode != null;
		}

		private bool CanMoveForward(JobPathType? path)
		{
			var currentItem = path?.LastTerm;

			if (path == null || currentItem == null)
			{
				return false;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			return !(currentPosition == siblings.Count - 1);
		}

		protected JobPathType? GetPreviousItemPath(JobPathType path, Func<JobNodeType, bool>? predicate = null)
		{
			var currentItem = path.LastTerm;

			if (currentItem == null)
			{
				return null;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousNode = GetPreviousNode(siblings, currentPosition, predicate);

			while (previousNode == null && path.Count > 1)
			{
				path = path.GetParentPath()!;
				currentItem = path.NodeSafe;

				var grandparentNode = path.GetParentNodeOrRoot();
				var ancestors = grandparentNode.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousNode = GetPreviousNode(ancestors, currentPosition + 1, predicate);
			}

			if (previousNode != null)
			{
				var result = path.Combine(previousNode.Node);

				return result;
			}
			else
			{
				return null;
			}
		}

		private JobNodeType? GetPreviousNode(IList<JobNodeType> nodes, int currentPosition, Func<JobNodeType, bool>? predicate = null)
		{
			JobNodeType? result;

			if (predicate != null)
			{
				result = nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault(predicate);
			}
			else
			{
				result = nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault();
			}

			return result;
		}

		private bool CanMoveBack(JobPathType? path)
		{
			var currentItem = path?.LastTerm;

			if (path == null || currentItem == null)
			{
				return false;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (currentPosition > 0)
			{
				// We can move to the previous item at the current level.
				return true;
			}
			else
			{
				// If we can go up, return true.
				return path.Count > 1;
			}
		}

		private Func<JobNodeType, bool>? GetPredicate(bool skipPanJobs)
		{
			Func<JobNodeType, bool>? result = skipPanJobs
				? x => x.Item.TransformType is TransformType.ZoomIn or TransformType.ZoomOut
				: null;
			return result;
		}

		//protected bool MoveCurrentTo(Job item, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		//{
		//	if (TryFindPath(item, currentBranch, out path))
		//	{
		//		ExpandAndSetCurrent(path);
		//		return true;
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

		//protected void ExpandAndSetCurrent(JobPathType path)
		//{
		//	if (path.IsEmpty)
		//	{
		//		return;
		//	}

		//	foreach (var p in path.Terms.SkipLast(1))
		//	{
		//		p.IsExpanded = true;
		//	}

		//	var lastTerm = path.Node;
		//	lastTerm.IsCurrent = true;
		//}

		#endregion

		//#region Lock Helpers

		//private Job DoWithReadLock(Func<Job> function)
		//{
		//	_treeLock.EnterReadLock();

		//	try
		//	{
		//		return function();
		//	}
		//	finally
		//	{
		//		_treeLock.ExitReadLock();
		//	}
		//}

		//private void DoWithWriteLock(Action action)
		//{
		//	_treeLock.EnterWriteLock();

		//	try
		//	{
		//		action();
		//	}
		//	finally
		//	{
		//		_treeLock.ExitWriteLock();
		//	}
		//}

		//#endregion

		//#region IDisposable Support

		//public void Dispose()
		//{
		//	GC.SuppressFinalize(this);
		//	((IDisposable)_treeLock).Dispose();
		//}

		//#endregion
	}
}
