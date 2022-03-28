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
				//_vm.ItemWidth = lvColorBands.ActualWidth - 5;

				//Debug.WriteLine("The ColorBandSetUserControl is now loaded");
			}
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
			var editableCollectionView = _vm.ColorBandsView as IEditableCollectionView;

			if (!editableCollectionView.IsEditingItem && lvColorBands.Items.CurrentItem is ColorBand selItem)
			{
				var index = lvColorBands.Items.IndexOf(selItem);
				var sucesssor = GetSuccessor(index);

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
					Sucessor = GetSuccessor(index)
				};

				var res = colorBandEditorDialog.ShowDialog();

				if (res == true)
				{
					if (selItem.BlendStyleUpdated)
					{
						selItem.ActualEndColor = ColorBandSet.GetActualEndColor(selItem, sucesssor?.StartColor);
					}

					editableCollectionView.CommitEdit();
					UpdateNeighbors(selItem, index);
				}
				else
				{
					editableCollectionView.CancelEdit();
				}

				lvColorBands.Items.Refresh();
				var didMove = lvColorBands.Items.MoveCurrentTo(selItem);

				if (!lvColorBands.Focus())
				{
					Debug.WriteLine("Could not return focus to the ListBox.");
				}
				else
				{
					FocusListBoxItem(index);
				}
			}
		}

		private void UpdateNeighbors(ColorBand selItem, int index)
		{
			if (TryGetPredeccessor(index, out var predecessor))
			{
				if (selItem.StartColorUpdated && predecessor.BlendStyle == ColorBandBlendStyle.Next)
				{
					predecessor.ActualEndColor = selItem.StartColor;
				}
			}

			if (TryGetSuccessor(index, out var sucessor))
			{
				if (selItem.CutOffUpdated)
				{
					sucessor.PreviousCutOff = selItem.CutOff;
				}
			}
		}

		private bool TryGetPredeccessor(int index, out ColorBand colorBand)
		{
			if (index < 1)
			{
				colorBand = null;
				return false;
			}
			else
			{
				colorBand = (ColorBand)lvColorBands.Items[index - 1];
				return true;
			}
		}

		private bool TryGetSuccessor(int index, out ColorBand colorBand)
		{
			if (index > lvColorBands.Items.Count - 2)
			{
				colorBand = null;
				return false;
			}
			else
			{
				colorBand = (ColorBand)lvColorBands.Items[index + 1];
				return true;
			}
		}

		private ColorBand GetSuccessor(int index)
		{
			_ = TryGetSuccessor(index, out var successor);
			return successor;
		}

		private void FocusListBoxItem(int index)
		{
			if (index != -1)
			{
				_ = Dispatcher.Invoke(DispatcherPriority.Loaded, (ThreadStart)delegate ()
				{
					var wasFocused = false;
					if (lvColorBands.ItemContainerGenerator.ContainerFromIndex(index) is IInputElement container)
					{
						wasFocused = container.Focus();
					}
				});
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
