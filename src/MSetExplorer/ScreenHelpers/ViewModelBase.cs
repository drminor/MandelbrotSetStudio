using System;
using System.ComponentModel;
using System.Windows;

namespace MSetExplorer
{
	public class ViewModelBase
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
				handler(this, e);
			}
		}

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
