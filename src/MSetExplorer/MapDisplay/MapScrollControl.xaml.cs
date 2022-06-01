using System.Diagnostics;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapScrollControl.xaml
	/// </summary>
	public partial class MapScrollControl : UserControl
	{
		private IMapScrollViewModel _vm;

		#region Constructor

		public MapScrollControl()
		{
			_vm = (IMapScrollViewModel)DataContext;

			Loaded += MapScroll_Loaded;
			InitializeComponent();
		}

		private void MapScroll_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the MapScroll UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IMapScrollViewModel)DataContext;

				mapDisplay1.DataContext = _vm.MapDisplayViewModel;

				_vm.PropertyChanged += ViewModel_PropertyChanged;

				HScrollBar.Scroll += HScrollBar_Scroll;
				VScrollBar.Scroll += VScrollBar_Scroll;

				Debug.WriteLine("The MapScroll Control is now loaded.");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapScrollViewModel.CurrentJobAreaAndCalcSettings))
			{
				if (_vm.CurrentJobAreaAndCalcSettings != null)
				{
					VScrollBar.Maximum = _vm.GetVMax();
					HScrollBar.Maximum = _vm.GetHMax();

					VScrollBar.ViewportSize = _vm.GetVerticalViewPortSize();

					HScrollBar.ViewportSize = _vm.GetHorizontalViewPortSize();
				}
			}
		}

		private void VScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
		{
			_vm.VerticalPosition = e.NewValue;
		}

		private void HScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
		{
			_vm.HorizontalPosition = e.NewValue;
		}

		#endregion
	}
}
