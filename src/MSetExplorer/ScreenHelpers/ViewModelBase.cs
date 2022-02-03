using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MSetExplorer
{
	public class ViewModelBase
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool InDesignMode => DesignerProperties.GetIsInDesignMode(new DependencyObject());



		//public event EventHandler RequestClose;

		//public RelayCommand CloseWindowCommand { get; private set; }

		//public ViewModelBase()
		//{
		//	CloseWindowCommand = new RelayCommand(CloseAWindow);
		//}

		//private void CloseAWindow(object obj)
		//{
		//	var window = obj as Window;
		//	if (window != null)
		//		window.Close();
		//}

	}
}
