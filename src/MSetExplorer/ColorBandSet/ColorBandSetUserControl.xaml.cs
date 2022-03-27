using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBand.xaml
	/// </summary>
	public partial class ColorBandSetUserControl : UserControl
	{
		private ColorBandSetViewModel _vm;

		#region Constructor 

		public ColorBandSetUserControl()
		{
			InitializeComponent();
			Loaded += ColorBandSetUserControl_Loaded;
		}

		private void ColorBandSetUserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandColorUserControl is being loaded.");
				return;
			}
			else
			{
				//lvColorBandsHdr.Width = lvColorBands.ActualWidth - 25;

				_vm = (ColorBandSetViewModel)DataContext;

				

				//clrBandDetail.DataContext = _vm;

				//_vm.ItemWidth = lvColorBands.ActualWidth - 5;

				//lvColorBands.ItemsSource = _vm.ColorBands;

				//lvColorBands.SelectionChanged += LvColorBands_SelectionChanged;

				//lvColorBands

				//Debug.WriteLine("The ColorBandSetUserControl is now loaded");
			}
		}

		private void LvColorBands_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//if (lvColorBands.SelectionMode != SelectionMode.Single)
			//{
			//	if (lvColorBands.SelectedItems.Count > 1)
			//	{
			//		//lvColorBands.SelectedItems.RemoveAt(0);
			//	}
			//}

			//_vm.SelectedColorBand = lvColorBands.SelectedItem as ColorBand;
		}

		#endregion

		#region Button Handlers

		private void CommitEditOnLostFocus(object sender, DependencyPropertyChangedEventArgs e)
		{
			//// if the root element of the edit mode template loses focus, commit the edit
			//if ((bool)e.NewValue == false)
			//{
			//	CommitCharacterChanges(null, null);
			//}
		}

		private void ColorBandDoubleClick(object sender, RoutedEventArgs e)
		{
			EditColorBand();
		}

		private void EditButton_Click(object sender, RoutedEventArgs e)
		{
			EditColorBand();
		}

		private void EditColorBand()
		{
			//var editableCollectionView = lvColorBands.Items as IEditableCollectionView;

			var editableCollectionView = _vm.ColorBandsView as IEditableCollectionView;

			if (!editableCollectionView.IsEditingItem && lvColorBands.Items.CurrentItem is ColorBandJr selItem)
			{
				editableCollectionView.EditItem(selItem);

				// invoke focus update at loaded priority so that template swap has time to complete
				_ = Dispatcher.Invoke(DispatcherPriority.Loaded, (ThreadStart)delegate ()
				{
					if (lvColorBands.ItemContainerGenerator.ContainerFromItem(selItem) is UIElement container)
					{
						_ = container.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
						Debug.WriteLine("The focus was moved.");
					}
				});

				var colorBandEditorDialog = new ColorBandEditorDialog
				{
					DataContext = selItem,
				};

				var res = colorBandEditorDialog.ShowDialog();

				if (res == true)
				{
					editableCollectionView.CommitEdit();
				}
				else
				{
					editableCollectionView.CancelEdit();
				}

				//lvColorBands.Items.Refresh();
				var didMove = lvColorBands.Items.MoveCurrentTo(selItem);

				if (!lvColorBands.Focus())
				{
					Debug.WriteLine("Could not return focus to the ListBox.");
				}
			}
		}

		private void InsertButton_Click(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine($"There are {lvColorBands.SelectedItems.Count} selected items. The current pos is {lvColorBands.SelectedIndex}.");

			lvColorBands.SelectedItems.Clear();

			//foreach(var x in lvColorBands.Items)
			//{
			//	if(x is ListViewItem lvi)
			//	{
			//		lvi.IsSelected = false;
			//	}
			//}
			_vm.InsertItem();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.DeleteSelectedItem();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.ApplyChanges();
		}

		//private void Test1Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test1();
		//}

		//private void Test2Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test2();
		//}

		//private void Test3Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test3();
		//}

		//private void Test4Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test4();
		//}

		#endregion
	}
}
