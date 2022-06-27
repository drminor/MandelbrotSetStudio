using MSS.Common;
using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace MSetExplorer
{
	public class JobProgressViewModel : ViewModelBase
	{
		#region Constructor

		private readonly SynchronizationContext? _synchronizationContext;
		private readonly IMapLoaderManager _mapLoaderManager;
		private JobProgressInfo _currentJobProgressInfo;

		public JobProgressViewModel(IMapLoaderManager mapLoaderManager)
		{
			_synchronizationContext = SynchronizationContext.Current;
			_mapLoaderManager = mapLoaderManager;
			_currentJobProgressInfo = new JobProgressInfo(0, "temp", DateTime.Now, 0);
			MapSectionProcessInfos = new ObservableCollection<MapSectionProcessInfo>();

			_mapLoaderManager.RequestAdded += MapLoaderManager_RequestAdded;
			_mapLoaderManager.RequestCompleted += MapLoaderManager_RequestCompleted;	
		}

		private void MapLoaderManager_RequestAdded(object? sender, JobProgressInfo e)
		{
			_synchronizationContext?.Post((o) => HandleRequstAdded(e), null);
		}

		private void HandleRequstAdded(JobProgressInfo jobProgressInfo)
		{
			CurrentJobProgressInfo = jobProgressInfo;
		}

		private void MapLoaderManager_RequestCompleted(object? sender, MapSectionProcessInfo e)
		{
			Debug.WriteLine($"Got a RequestCompleted event. JobNumber: {e.JobNumber}, Number Completed: {e.RequestsCompleted}.");
			_synchronizationContext?.Post((o) => HandleRequestCompleted(e), null);
		}

		private void HandleRequestCompleted(MapSectionProcessInfo mapSectionProcessInfo)
		{
			if (mapSectionProcessInfo.RequestsCompleted == -1)
			{

			}
			else
			{
				MapSectionProcessInfos.Add(mapSectionProcessInfo);

				if (CurrentJobProgressInfo.JobNumber == mapSectionProcessInfo.JobNumber)
				{
					if (mapSectionProcessInfo.FoundInRepo)
					{
						CurrentJobProgressInfo.FetchedCount += 1;
					}
					else
					{
						CurrentJobProgressInfo.GeneratedCount += 1;
					}
				}
			}

			OnPropertyChanged(nameof(PercentComplete));
		}

		#endregion

		#region Public Properties

		public ObservableCollection<MapSectionProcessInfo> MapSectionProcessInfos { get; }

		public JobProgressInfo CurrentJobProgressInfo
		{
			get => _currentJobProgressInfo; 
			set
			{
				if (value !=_currentJobProgressInfo)
				{
					_currentJobProgressInfo = value;
					OnPropertyChanged();
				}
			}
		}

		public double PercentComplete
		{
			get => CurrentJobProgressInfo.PercentComplete;
			set { }
		}

		#endregion

		//public void Done(int jobNumber)
		//{
		//	if (CurrentJobProgressInfo.JobNumber == jobNumber)
		//	{
		//		CurrentJobProgressInfo.PercentComplete = 100;
		//		OnPropertyChanged(nameof(PercentComplete));
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"Done was called but not used. Our JobNumber = {CurrentJobProgressInfo.JobNumber}, JobNumber provided: {jobNumber}.");
		//	}
		//}
	}
}
