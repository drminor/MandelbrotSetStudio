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
		private JobProgressRecord _currentJobProgressRecord;

		public JobProgressViewModel(IMapLoaderManager mapLoaderManager)
		{
			_synchronizationContext = SynchronizationContext.Current;
			_mapLoaderManager = mapLoaderManager;
			_currentJobProgressRecord = new JobProgressRecord(0, "temp", DateTime.Now, 0);
			MapSectionProcessInfos = new ObservableCollection<MapSectionProcessInfo>();

			_mapLoaderManager.RequestAdded += MapLoaderManager_RequestAdded;
			_mapLoaderManager.RequestCompleted += MapLoaderManager_RequestCompleted;	
		}

		private void MapLoaderManager_RequestAdded(object? sender, int e)
		{
			_synchronizationContext?.Post((o) => HandleRequstAdded(e), null);
		}

		private void HandleRequstAdded(int jobNumber)
		{
			CurrentJobProgressRecord.PropertyChanged -= CurrentJobProgressRecord_PropertyChanged;
			CurrentJobProgressRecord = new JobProgressRecord(jobNumber, "temp", DateTime.Now, 100);

			CurrentJobProgressRecord.PropertyChanged += CurrentJobProgressRecord_PropertyChanged;
		}

		private void CurrentJobProgressRecord_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(PercentageComplete));
		}

		private void MapLoaderManager_RequestCompleted(object? sender, MapSectionProcessInfo e)
		{

			Debug.WriteLine($"Got a RequestCompleted event. JobNumber: {e.JobNumber}, Number Completed: {e.RequestsCompleted}.");
			_synchronizationContext?.Post((o) => HandleRequestCompleted(e), null);
		}

		private void HandleRequestCompleted(MapSectionProcessInfo mapSectionProcessInfo)
		{
			MapSectionProcessInfos.Add(mapSectionProcessInfo);
			if (CurrentJobProgressRecord.JobNumber == mapSectionProcessInfo.JobNumber)
			{
				if (mapSectionProcessInfo.FoundInRepo)
				{
					CurrentJobProgressRecord.FetchedCount += 1;
				}
				else
				{
					CurrentJobProgressRecord.GeneratedCount += 1;
				}
			}
		}

		#endregion

		#region Public Properties

		public ObservableCollection<MapSectionProcessInfo> MapSectionProcessInfos { get; }

		public JobProgressRecord CurrentJobProgressRecord
		{
			get => _currentJobProgressRecord; 
			set
			{
				if (value !=_currentJobProgressRecord)
				{
					_currentJobProgressRecord = value;
					OnPropertyChanged();
				}
			}
		}

		public double PercentageComplete
		{
			get => CurrentJobProgressRecord.PercentComplete;
			set { }
		}


		#endregion
	}
}
