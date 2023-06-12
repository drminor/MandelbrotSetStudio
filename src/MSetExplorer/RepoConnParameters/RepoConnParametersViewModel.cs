
using MSS.Types;
using ProjectRepo;
using System;
using System.ServiceProcess;

namespace MSetExplorer
{
	public class RepoConnParametersViewModel : ViewModelBase
	{
		private string? _serverName;
		private int _port;
		private string? _databaseName;

		private string? _serviceStatus;
		private bool _canConnect;

		public RepoConnParametersViewModel(string? serverName, int port, string? databaseName)
		{
			_serverName = serverName ?? Environment.MachineName;
			_port = port;
			_databaseName = databaseName ?? RMapConstants.DEFAULT_DATA_BASE_NAME;

			_serviceStatus = null;
			_canConnect = false;
		}

		#region Public Properties

		public string ConnectionStatus
		{
			get => _canConnect ? "Connected" : "Unable to connect";
		}

		public string? ServiceStatus
		{
			get => _serviceStatus;
			
			private set
			{
				_serviceStatus = value;
				OnPropertyChanged();
			}
		}

		public string? ServerName
		{
			get => _serverName;
			set
			{
				_serverName = value;
				OnPropertyChanged();
			}
		}

		public int Port
		{
			get => _port;
			set
			{
				_port = value;
				OnPropertyChanged();
			}
		}

		public string? DatabaseName
		{
			get => _databaseName;
			set
			{
				_databaseName = value;
				OnPropertyChanged();
			}
		}

		#endregion

		public ServiceControllerStatus? RefreshServiceStatus()
		{
			var status = ServiceHelper.CheckService(RMapConstants.SERVICE_NAME);

			ServiceStatus = status == null ? "Not Found" : status.Value.ToString();

			return status;
		}

		public bool CheckConnectivity(TimeSpan? connectTime = null)
		{
			bool result;

			if (ServerName != null && DatabaseName != null)
			{
				var dbProvider = new DbProvider(ServerName, Port, DatabaseName);
				result = dbProvider.TestConnection(DatabaseName, connectTime);
			}
			else
			{
				result = false;
			}

			_canConnect = result;
			OnPropertyChanged(nameof(ConnectionStatus));

			return _canConnect;
		}

		//private bool DatabaseExists(DbProvider dbProvider)
		//{
		//	try
		//	{
		//		var projectReaderWriter = new ProjectReaderWriter(dbProvider);
		//		var x = projectReaderWriter.Collection.Indexes;
		//		_ = x.List();
		//		return true;
		//	}
		//	catch
		//	{
		//		return false;
		//	}
		//}

	}
}
