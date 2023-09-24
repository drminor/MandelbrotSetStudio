using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for RepoConnParametersDialog.xaml
	/// </summary>
	public partial class RepoConnParametersDialog : Window
	{
		private RepoConnParametersViewModel _vm;

		#region Constructor

		public RepoConnParametersDialog()
		{
			_vm = (RepoConnParametersViewModel)DataContext;
			Loaded += RepoConnParametersDialog_Loaded;
			InitializeComponent();
		}

		#endregion

		#region Event Handlers

		private void RepoConnParametersDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the RepoConnParameters Dialog is being loaded.");
				return;
			}
			else
			{
				_vm = (RepoConnParametersViewModel)DataContext;
				txtServerName.LostFocus += TxtServerName_LostFocus;
				_ = txtServerName.Focus();

				RefreshServiceStatus();
				CheckConnectivity();

				Debug.WriteLine("The RepoConnParameters Dialog is now loaded");
			}
		}

		private void TxtServerName_LostFocus(object sender, RoutedEventArgs e)
		{
			RefreshServiceStatus();
			CheckConnectivity();
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object _1, RoutedEventArgs _2)
		{
			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object _1, RoutedEventArgs _2)
		{
			DialogResult = false;
			Close();
		}

		private void BrowseServerButton_Click(object _1, RoutedEventArgs _2)
		{
			MessageBox.Show("Using a repository hosted on a remote server is planned, but not yet implemented.", "Search Network", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
		}

		private void CheckServiceButton_Click(object _1, RoutedEventArgs _2)
		{
			RefreshServiceStatus();

			//MessageBox.Show($"Service is {_vm.ServiceStatus}.", "Checking Service Status", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
		}

		private void CheckConnectionButton_Click(object _1, RoutedEventArgs _2)
		{
			//var connected = CheckConnectivity();

			//var msg = connected ? "Connection established." : "Could not connect.";

			//MessageBox.Show($"{msg}", "Checking Connectivity", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);

			_ = CheckConnectivity();
		}
		
		#endregion

		#region Private Methods

		private void RefreshServiceStatus()
		{
			var status = _vm.RefreshServiceStatus();

			btnCheckConnection.IsEnabled = status == ServiceControllerStatus.Running;
			btnSave.IsEnabled = status == ServiceControllerStatus.Running && _vm.ConnectionStatus.Contains("Connected");

			if (status == null)
			{
				borderServiceNotInstalled.Visibility = Visibility.Visible;
				borderServiceFound.Visibility = Visibility.Collapsed;
			}
			else 
			{
				borderServiceNotInstalled.Visibility = Visibility.Collapsed;
				borderServiceFound.Visibility = Visibility.Visible;

				if (status == ServiceControllerStatus.Running)
				{
					txtBlockServiceStatus.Text = "The MongoDB Windows Service is running.";
					txtServerName.IsEnabled = true;
					btnBrowseServer.IsEnabled = true;
					txtPort.IsEnabled = true;
					txtDatabaseName.IsEnabled = true;
				}
				else
				{
					txtBlockServiceStatus.Text = $"The MongoDB Windows Service is {status.Value}.";

					txtServerName.IsEnabled = false;
					btnBrowseServer.IsEnabled = false;
					txtPort.IsEnabled = false;
					txtDatabaseName.IsEnabled = false;
				}
			}
		}

		private bool CheckConnectivity()
		{
			bool result;

			if (_vm.ServiceStatus == ServiceControllerStatus.Running.ToString())
			{
				result = _vm.CheckConnectivity(TimeSpan.FromSeconds(10));
			}
			else
			{
				result = false;
			}

			btnSave.IsEnabled = result;

			return result;
		}

		#endregion
	}
}
