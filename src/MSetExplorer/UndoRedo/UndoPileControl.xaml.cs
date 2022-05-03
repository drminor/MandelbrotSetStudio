using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for UndoPileControl.xaml
	/// </summary>
	public partial class UndoPileControl : UserControl
	{
		private IUndoRedoViewModel _vm;

		public UndoPileControl()
		{
			_vm = (IUndoRedoViewModel)DataContext;
			Loaded += UndoPileControl_Loaded;
			InitializeComponent();
		}

		private void UndoPileControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the UndoPileControl is being loaded.");
				Debug.WriteLine("The DataContext is null as the UndoPileControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IUndoRedoViewModel)DataContext;
				_vm.PropertyChanged += ViewModel_PropertyChanged;

				//Debug.WriteLine("The UndoPileControl is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IUndoRedoViewModel.CanGoBack) || e.PropertyName == nameof(IUndoRedoViewModel.CanGoForward))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		// Undo CanExecute
		public void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.CanGoBack ?? false;
		}

		// Undo
		public void UndoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.GoBack();
		}

		// Redo CanExecute
		public void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.CanGoForward ?? false;
		}

		// Redo
		public void RedoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.GoForward();
		}



	}
}
