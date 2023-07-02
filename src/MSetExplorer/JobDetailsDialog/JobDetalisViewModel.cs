using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class JobDetailsViewModel : INotifyPropertyChanged
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly Func<Job, SizeDbl, bool, long>? _deleteNonEssentialMapSectionsFunction;

		private IJobInfo? _selectedJob;

		#endregion

		#region Constructor

		public JobDetailsViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, Func<Job, SizeDbl, bool, long>? deleteNonEssentialMapSectionsFunction, ObjectId ownerId, OwnerType ownerType, ObjectId? initialJobId)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;

			_deleteNonEssentialMapSectionsFunction = deleteNonEssentialMapSectionsFunction;

			OwnerId = ownerId;
			OwnerType = ownerType;

			var jobInfos = _projectAdapter.GetJobInfosForOwner(ownerId);

			JobInfos = new ObservableCollection<IJobInfo>(jobInfos);

			SelectedJobInfo = JobInfos.FirstOrDefault(x => x.Id == initialJobId);

			var view = CollectionViewSource.GetDefaultView(JobInfos);
			_ = view.MoveCurrentTo(SelectedJobInfo);
		}

		#endregion

		#region Public Properties

		public ObjectId OwnerId { get; init; }

		public OwnerType OwnerType { get; init; }

		public ObservableCollection<IJobInfo> JobInfos { get; init; }

		public IJobInfo? SelectedJobInfo
		{
			get => _selectedJob;

			set
			{
				_selectedJob = value;
				OnPropertyChanged();
			}
		}


		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.PosterExists(name, out _);
			return result;
		}

		public bool DeleteSelected(out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			//var posterInfo = SelectedJobInfo;

			//if (posterInfo == null)
			//{
			//	return false;
			//}

			//bool result;
			//if (ProjectAndMapSectionHelper.DeletePoster(posterInfo.PosterId, _projectAdapter, _mapSectionAdapter, out numberOfMapSectionsDeleted))
			//{
			//	_ = PosterInfos.Remove(posterInfo);
			//	result = true;
			//}
			//else
			//{
			//	result = false;
			//}

			var result = false;
			return result;
		}

		//public long TrimSelected(bool agressive)
		//{
		//	if (SelectedJobInfo == null || _deleteNonEssentialMapSectionsFunction == null)
		//	{
		//		return -1;
		//	}

		//	var jobId = SelectedJobInfo.CurrentJobId;
		//	var job = _projectAdapter.GetJob(jobId);
		//	var posterSize = SelectedJobInfo.Size;

		//	var result = _deleteNonEssentialMapSectionsFunction(job, posterSize, agressive);

		//	return result;
		//}

		//public long TrimHeavySelected()
		//{
		//	return -1;
		//}

		#endregion

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
