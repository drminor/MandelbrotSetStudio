using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MSetExplorer
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool InDesignMode => DesignerProperties.GetIsInDesignMode(new DependencyObject());

	}
}
