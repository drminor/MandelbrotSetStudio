using MSS.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
			Loaded += ColorBandSetUserControl_Loaded;
			InitializeComponent();
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
			if (e.PropertyName == nameof(ColorBandSetViewModel.IsDirty) || e.PropertyName == nameof(ColorBandSetViewModel.ColorBandSet))
			{
				// TODO: What is the domain or scope of calling InvalidateRequerySuggested on the ColorBandSetUserControl?
				CommandManager.InvalidateRequerySuggested();
			}
		}

		#endregion

		#region Button Handlers

		private void ColorBandDoubleClick(object sender, RoutedEventArgs e)
		{
			//MessageBox.Show("Got a dbl click.");
			EditColorBand();
		}

		private void ShowDetails_Click(object sender, RoutedEventArgs e)
		{
			string msg;

			if (lvColorBands.Items.CurrentItem is ColorBand selItem)
			{
				var index = lvColorBands.Items.IndexOf(selItem);

				//msg = $"Percentage: {selItem.Percentage}, Count: {specs.Count}, Exact Count: {specs.ExactCount}";
				msg = $"Percentage: {selItem.Percentage}";

				if (index == lvColorBands.Items.Count - 1 && _vm.BeyondTargetSpecs != null)
				{
					var specs = _vm.BeyondTargetSpecs;
					msg += $"\nBeyond Last Info: Percentage: {specs.Percentage}, Count: {specs.Count}, Exact Count: {specs.ExactCount}";
				}

				//ReportHistogram(_vm.GetHistogramForColorBand(index));
			}
			else
			{
				msg = "No Current Item.";
			}

			_ = MessageBox.Show(msg);
		}

		private void ReportHistogram(IDictionary<int, int> histogram)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Histogram:");

			foreach(KeyValuePair<int, int> kvp in histogram)
			{
				sb.AppendLine($"\t{kvp.Key} : {kvp.Value}");
			}

			Debug.WriteLine(sb.ToString());
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
			e.CanExecute = _vm?.CurrentColorBand != null;
		}

		// Insert
		private void InsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_vm.TryInsertNewItem(out var index))
			{
				FocusListBoxItem(index);
			}
		}

		// Delete CanExecute
		private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.CurrentColorBand != null; // _vm?.ColorBandsView.CurrentPosition < _vm?.ColorBandsView.Count - 1;
		}

		// Delete
		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.TryDeleteSelectedItem();
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

		// Will create a new dialog box that will show the starting / ending and blend options
		private void EditColorBand()
		{
			var view = _vm.ColorBandsView;

			if (!view.IsEditingItem && lvColorBands.Items.CurrentItem is ColorBand selItem)
			{
				var index = lvColorBands.Items.IndexOf(selItem);

				var predeccessor = GetPredeccessor(index);
				var sucesssor = GetSuccessor(index);

				var preClr = predeccessor?.EndColor ?? new ColorBandColor("#ff0000");
				var folClr = sucesssor?.StartColor ?? new ColorBandColor("#00ff00");

				Debug.WriteLine($"Edit Color Band: The Predeccessor's EndColor is {preClr}. The Successor' StartColor is {folClr}.");

				//view.EditItem(selItem);
			}
		}

		private void UpdateNeighbors(ColorBand selItem, int index)
		{
			var predecessor = GetPredeccessor(index);
			var successor = GetSuccessor(index);

			selItem.UpdateWithNeighbors(predecessor, successor);
		}

		private ColorBand? GetPredeccessor(int index)
		{
			_ = TryGetPredeccessor(index, out var predeccessor);
			return predeccessor;
		}

		private bool TryGetPredeccessor(int index, out ColorBand? colorBand)
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

		private ColorBand? GetSuccessor(int index)
		{
			_ = TryGetSuccessor(index, out var successor);
			return successor;
		}

		private bool TryGetSuccessor(int index, out ColorBand? colorBand)
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

		#endregion
	}
}
