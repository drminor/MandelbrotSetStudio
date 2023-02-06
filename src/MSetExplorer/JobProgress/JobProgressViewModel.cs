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
			Debug.WriteLine("The JobProgressViewModel is being loaded.");
			_synchronizationContext = SynchronizationContext.Current;
			_mapLoaderManager = mapLoaderManager;
			_currentJobProgressInfo = new JobProgressInfo(0, "temp", DateTime.UtcNow, 0);
			MapSectionProcessInfos = new ObservableCollection<MapSectionProcessInfo>();

			_mapLoaderManager.RequestAdded += MapLoaderManager_RequestAdded;
			_mapLoaderManager.SectionLoaded += MapLoaderManager_SectionLoaded;	
		}

		private void MapLoaderManager_RequestAdded(object? sender, JobProgressInfo e)
		{
			_synchronizationContext?.Post((o) => HandleRequstAdded(e), null);
		}

		private void HandleRequstAdded(JobProgressInfo jobProgressInfo)
		{
			CurrentJobProgressInfo = jobProgressInfo;
			CurrentJobProgressInfo.DateCreatedUtc = DateTime.UtcNow;

			OnPropertyChanged(nameof(TotalSections));
		}

		private void MapLoaderManager_SectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			//Debug.WriteLine($"Got a RequestCompleted event. JobNumber: {e.JobNumber}, Number Completed: {e.RequestsCompleted}.");
			_synchronizationContext?.Post((o) => HandleMapSectionLoaded(e), null);
		}

		private void HandleMapSectionLoaded(MapSectionProcessInfo mapSectionProcessInfo)
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

			OnPropertyChanged(nameof(RunTime));
			OnPropertyChanged(nameof(EstimatedTimeRemaining));

			OnPropertyChanged(nameof(FetchedCount));
			OnPropertyChanged(nameof(GeneratedCount));

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

		public int TotalSections
		{
			get => CurrentJobProgressInfo.TotalSections;
			set { }
		}

		public TimeSpan RunTime
		{
			get => CurrentJobProgressInfo.RunTime;
			set { }
		}

		public TimeSpan EstimatedTimeRemaining
		{
			get => CurrentJobProgressInfo.EstimatedTimeRemaining;
			set { }
		}

		public double PercentComplete
		{
			get => CurrentJobProgressInfo.PercentComplete;
			set { }
		}

		public int FetchedCount
		{
			get => CurrentJobProgressInfo.FetchedCount;
			set { }
		}

		public int GeneratedCount
		{
			get => CurrentJobProgressInfo.GeneratedCount;
			set { }
		}

		#endregion

	}
}
