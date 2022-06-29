using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CbshScrollControl.xaml
	/// </summary>
	public partial class CbshScrollControl : UserControl
	{
		private CbshScrollViewModel _vm;

		#region Constructor

		public CbshScrollControl()
		{
			_vm = (CbshScrollViewModel)DataContext;

			Loaded += CbshScroll_Loaded;
			InitializeComponent();
		}

		private void CbshScroll_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the MapScroll UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (CbshScrollViewModel)DataContext;

				cbshDisplayControl1.DataContext = _vm.CbshDisplayViewModel;

				_vm.PropertyChanged += CbshScrollViewModel_PropertyChanged; 
				_vm.CbshDisplayViewModel.PropertyChanged += CbshDisplayViewModel_PropertyChanged;

				HScrollBar.Scroll += HScrollBar_Scroll;
				//VScrollBar.Scroll += VScrollBar_Scroll;

				Debug.WriteLine("The MapScroll Control is now loaded.");
			}
		}

		private void _vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Event Handlers

		private void CbshDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.LogicalDisplaySize))
			{
				ConfigureScrollBars(_vm.CbshDisplayViewModel.LogicalDisplaySize, _vm.HistogramSize);
			}
		}

		private void CbshScrollViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapScrollViewModel.PosterSize))
			{
				ConfigureScrollBars(_vm.CbshDisplayViewModel.LogicalDisplaySize, _vm.HistogramSize);
			}

			//if (e.PropertyName == nameof(IMapScrollViewModel.VerticalPosition))
			//{
			//	VScrollBar.Value = _vm.VerticalPosition;
			//}

			if (e.PropertyName == nameof(IMapScrollViewModel.HorizontalPosition))
			{
				HScrollBar.Value = _vm.HorizontalPosition;
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

		#region Private Methods

		private void ConfigureScrollBars(SizeInt logicalDisplaySize, SizeInt? posterSize)
		{
			if (posterSize.HasValue)
			{
				try
				{
					//VScrollBar.Maximum = posterSize.Value.Height - logicalDisplaySize.Height;
					var verticalViewPortSize = logicalDisplaySize.Height;

					//VScrollBar.ViewportSize = verticalViewPortSize;
					//VScrollBar.LargeChange = verticalViewPortSize;
					//VScrollBar.SmallChange = 0.125 * verticalViewPortSize;

					HScrollBar.Maximum = posterSize.Value.Width - logicalDisplaySize.Width;
					var horizontalViewPortSize = logicalDisplaySize.Width;

					HScrollBar.ViewportSize = horizontalViewPortSize;
					HScrollBar.LargeChange = horizontalViewPortSize;
					HScrollBar.SmallChange = 0.125 * horizontalViewPortSize;

					Debug.WriteLine($"LogicalDisplaySize: {logicalDisplaySize}, V-ViewPortSize: {verticalViewPortSize}, H-ViewPortSize: {horizontalViewPortSize}.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Warning: Encountered exception: {e.Message} while configuring the scroll bars. Resetting to default values.");
					ConfigureScrollBarsWithDefautVals();
				}
			}
			else
			{
				ConfigureScrollBarsWithDefautVals();
			}
		}

		private void ConfigureScrollBarsWithDefautVals()
		{
			//VScrollBar.Maximum = 0;
			//VScrollBar.ViewportSize = 1024;
			//VScrollBar.LargeChange = 128;
			//VScrollBar.SmallChange = 1;

			HScrollBar.Maximum = 0;
			HScrollBar.ViewportSize = 1024;
			HScrollBar.LargeChange = 128;
			HScrollBar.SmallChange = 1;
		}

		#endregion
	}
}
