using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for JobProgressControl.xaml
	/// </summary>
	public partial class JobProgressControl : UserControl
	{
		private JobProgressViewModel _vm;
		private ICollectionView? _collectionView;

		#region Constructor 

		public JobProgressControl()
		{
			_vm = (JobProgressViewModel)DataContext;

			Loaded += JobProgressControl_Loaded;
			InitializeComponent();
		}

		private void JobProgressControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the obProgress Control is being loaded.");
				return;
			}
			else
			{
				_vm = (JobProgressViewModel)DataContext;
				prgBarCurrentJob.Maximum = 100;
				borderTop.DataContext = DataContext;

				lvJobProgressEntries.ItemsSource = _vm.MapSectionProcessInfos;
				_collectionView = CollectionViewSource.GetDefaultView(_vm.MapSectionProcessInfos);


				_vm.MapSectionProcessInfos.CollectionChanged += MapSectionProcessInfos_CollectionChanged;
				_vm.PropertyChanged += ViewModel_PropertyChanged;

				Debug.WriteLine("The JobProgress Control is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(JobProgressViewModel.PercentComplete))
			{
				prgBarCurrentJob.Value = _vm.PercentComplete;
				MoveSelectedToLast();
			}
		}

		private void MapSectionProcessInfos_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				MoveSelectedToLast();
			}
		}

		#endregion

		private void MoveSelectedToLast()
		{
			if (_collectionView != null)
			{
				_collectionView.MoveCurrentToLast();

				if (_collectionView.CurrentPosition > 20)
				{
					var curItem = _collectionView.CurrentItem;
					if (curItem != null)
					{
						lvJobProgressEntries.ScrollIntoView(curItem);
					}
				}

			}
		}

	}
}
