using MSS.Types;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
			_vm = (ColorBandSetViewModel)DataContext;
			InitializeComponent();
			Loaded += ColorBandSetUserControl_Loaded;
		}

		private void ColorBandSetUserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandSetUserControl is being loaded.");
				Debug.WriteLine("The DataContext is null as the ColorBandSetUserControl is being loaded.");
				return;
			}
			else
			{
				//lvColorBandsHdr.Width = lvColorBands.ActualWidth - 25;
				_vm = (ColorBandSetViewModel)DataContext;
				_vm.PropertyChanged += ViewModel_PropertyChanged;
				//undoRedo1.DataContext = _vm.
				//_vm.ItemWidth = lvColorBands.ActualWidth - 5;

				//Debug.WriteLine("The ColorBandSetUserControl is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.IsDirty))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		#endregion

		#region Button Handlers

		private void ColorBandDoubleClick(object sender, RoutedEventArgs e)
		{
			//EditColorBand();
		}

		private void ShowDetails_Click(object sender, RoutedEventArgs e)
		{
			string msg;

			var specs = _vm.BeyondTargetSpecs;

			if (specs != null)
			{
				msg = $"Percentage: {specs.Percentage}, Count: {specs.Count}, Exact Count: {specs.ExactCount}";
			}
			else
			{
				msg = "No Details Available.";
			}

			_ = MessageBox.Show(msg);
		}

		private void CommitEditOnLostFocus(object sender, DependencyPropertyChangedEventArgs e)
		{
			//// if the root element of the edit mode template loses focus, commit the edit
			//if ((bool)e.NewValue == false)
			//{
			//	CommitCharacterChanges(null, null);
			//}
		}

		#endregion

		#region Command Binding Handlers

		// Insert CanExecute
		private void InsertCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		// Insert
		private void InsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			InsertColorBand();
		}

		// Delete CanExecute
		private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ColorBandsView.CurrentPosition < _vm?.ColorBandsView.Count - 1;
		}

		// Delete
		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.DeleteSelectedItem();
		}


		// Revert CanExecute
		private void RevertCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.IsDirty ?? false;
		}

		// Revert
		private void RevertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.RevertChanges();
		}

		// Apply CanExecute
		private void ApplyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.IsDirty ?? false;
		}

		// Apply
		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.ApplyChanges();
		}

		#endregion

		#region Private Methods

		private void InsertColorBand()
		{
			Debug.WriteLine($"There are {lvColorBands.SelectedItems.Count} selected items. The current pos is {lvColorBands.SelectedIndex}.");

			var view = _vm.ColorBandsView;

			if (!view.IsAddingNew && lvColorBands.Items.CurrentItem is ColorBand selItem)
			{
				if (selItem.CutOff - selItem.StartingCutOff < 1)
				{
					_ = MessageBox.Show("No Room to insert here.");
					return;
				}

				var index = lvColorBands.Items.IndexOf(selItem);
				var prevCutOff = selItem.PreviousCutOff ?? 0;
				var newCutoff = prevCutOff + (selItem.CutOff - prevCutOff) / 2;
				var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.Next, selItem.StartColor, selItem.PreviousCutOff, selItem.StartColor, double.NaN);
				_vm.InsertItem(index, newItem);

				//lvColorBands.Items.Refresh();
				_ = lvColorBands.Items.MoveCurrentToPosition(index);

				FocusListBoxItem(index);
			}
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

		//private void EditColorBand()
		//{
		//	var view = _vm.ColorBandsView;

		//	if (!view.IsEditingItem && lvColorBands.Items.CurrentItem is ColorBand selItem)
		//	{
		//		var index = lvColorBands.Items.IndexOf(selItem);
		//		var sucesssor = GetSuccessor(index);

		//		view.EditItem(selItem);

		//		// invoke focus update at loaded priority so that template swap has time to complete
		//		_ = Dispatcher.Invoke(DispatcherPriority.Loaded, (ThreadStart)delegate ()
		//		{
		//			if (lvColorBands.ItemContainerGenerator.ContainerFromItem(selItem) is UIElement container)
		//			{
		//				_ = container.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		//				Debug.WriteLine("The focus was moved.");
		//			}
		//		});

		//		var colorBandEditorDialog = new ColorBandEditorDialog
		//		{
		//			DataContext = selItem,
		//			Sucessor = GetSuccessor(index)
		//		};

		//		var res = colorBandEditorDialog.ShowDialog();

		//		if (res == true)
		//		{
		//			if (selItem.BlendStyleUpdated)
		//			{
		//				selItem.ActualEndColor = ColorBandSet.GetActualEndColor(selItem, sucesssor?.StartColor);
		//			}

		//			view.CommitEdit();
		//			UpdateNeighbors(selItem, index);
		//			_vm.ItemWasUpdated();
		//		}
		//		else
		//		{
		//			view.CancelEdit();
		//		}

		//		lvColorBands.Items.Refresh();
		//		_ = lvColorBands.Items.MoveCurrentTo(selItem);

		//		if (!lvColorBands.Focus())
		//		{
		//			Debug.WriteLine("Could not return focus to the ListBox.");
		//		}
		//		else
		//		{
		//			FocusListBoxItem(index);
		//		}
		//	}
		//}

		//private void UpdateNeighbors(ColorBand selItem, int index)
		//{
		//	if (TryGetPredeccessor(index, out var predecessor))
		//	{
		//		if (selItem.StartColorUpdated && predecessor != null && predecessor.BlendStyle == ColorBandBlendStyle.Next)
		//		{
		//			predecessor.ActualEndColor = selItem.StartColor;
		//		}
		//	}

		//	if (TryGetSuccessor(index, out var sucessor))
		//	{
		//		if (selItem.CutOffUpdated && sucessor != null)
		//		{
		//			sucessor.PreviousCutOff = selItem.CutOff;
		//		}
		//	}
		//}

		//private bool TryGetPredeccessor(int index, out ColorBand? colorBand)
		//{
		//	if (index < 1)
		//	{
		//		colorBand = null;
		//		return false;
		//	}
		//	else
		//	{
		//		colorBand = (ColorBand)lvColorBands.Items[index - 1];
		//		return true;
		//	}
		//}

		//private bool TryGetSuccessor(int index, out ColorBand? colorBand)
		//{
		//	if (index > lvColorBands.Items.Count - 2)
		//	{
		//		colorBand = null;
		//		return false;
		//	}
		//	else
		//	{
		//		colorBand = (ColorBand)lvColorBands.Items[index + 1];
		//		return true;
		//	}
		//}

		//private ColorBand? GetSuccessor(int index)
		//{
		//	_ = TryGetSuccessor(index, out var successor);
		//	return successor;
		//}

		#endregion
	}
}
