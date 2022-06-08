﻿using MSS.Types;
using System;
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
				_vm.MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;

				HScrollBar.Scroll += HScrollBar_Scroll;
				VScrollBar.Scroll += VScrollBar_Scroll;

				Debug.WriteLine("The MapScroll Control is now loaded.");
			}
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.LogicalDisplaySize))
			{
				ConfigureScrollBars(_vm.MapDisplayViewModel.LogicalDisplaySize, _vm.PosterSize);
			}
		}

		private void ConfigureScrollBars(SizeDbl logicalDisplaySize, SizeInt? posterSize)
		{
			if (posterSize.HasValue)
			{
				// NOTE: LogicalDisplaySize = new SizeDbl(CanvasSize).Scale(1 / _displayZoom);

				try
				{
					//var displaySize = _vm.MapDisplayViewModel.CanvasSize;
					//var verticalViewPortSize = displaySize.Height * logicalDisplaySize.Height / posterSize.Value.Height;

					//VScrollBar.Maximum = (int)Math.Round(displaySize.Height - verticalViewPortSize);

					//VScrollBar.ViewportSize = verticalViewPortSize;
					//VScrollBar.LargeChange = verticalViewPortSize;
					//VScrollBar.SmallChange = 0.125 * verticalViewPortSize;

					//var horizontalViewPortSize = displaySize.Width * logicalDisplaySize.Width / posterSize.Value.Width;

					//HScrollBar.Maximum = (int)Math.Round(displaySize.Width - horizontalViewPortSize);
					//HScrollBar.ViewportSize = horizontalViewPortSize;
					//HScrollBar.LargeChange = horizontalViewPortSize;
					//HScrollBar.SmallChange = 0.125 * horizontalViewPortSize;


					VScrollBar.Maximum = posterSize.Value.Height - logicalDisplaySize.Height;
					var verticalViewPortSize = logicalDisplaySize.Height;

					VScrollBar.ViewportSize = verticalViewPortSize;
					VScrollBar.LargeChange = verticalViewPortSize;
					VScrollBar.SmallChange = 0.125 * verticalViewPortSize;

					HScrollBar.Maximum = posterSize.Value.Width - logicalDisplaySize.Width;
					var horizontalViewPortSize = logicalDisplaySize.Width;

					HScrollBar.ViewportSize = horizontalViewPortSize;
					HScrollBar.LargeChange = horizontalViewPortSize;
					HScrollBar.SmallChange = 0.125 * horizontalViewPortSize;
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
			VScrollBar.Maximum = 0;
			VScrollBar.ViewportSize = 1024;
			VScrollBar.LargeChange = 128;
			VScrollBar.SmallChange = 1;

			VScrollBar.Maximum = 0;
			HScrollBar.ViewportSize = 1024;
			HScrollBar.LargeChange = 128;
			HScrollBar.SmallChange = 1;
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(IMapScrollViewModel.VMax))
			//{
			//	VScrollBar.Maximum = _vm.VMax;
			//	//VScrollBar.ViewportSize = _vm.VerticalViewPortSize;
			//	//VScrollBar.LargeChange = _vm.VerticalViewPortSize;
			//	//VScrollBar.SmallChange = 0.125 * VScrollBar.LargeChange;
			//}

			//else if (e.PropertyName == nameof(IMapScrollViewModel.HMax))
			//{
			//	HScrollBar.Maximum = _vm.HMax;
			//	//HScrollBar.ViewportSize = _vm.HorizontalViewPortSize;
			//	//HScrollBar.LargeChange = _vm.HorizontalViewPortSize;
			//	//HScrollBar.SmallChange = 0.125 * HScrollBar.LargeChange;
			//}

			if (e.PropertyName == nameof(IMapScrollViewModel.PosterSize))
			{
				ConfigureScrollBars(_vm.MapDisplayViewModel.LogicalDisplaySize, _vm.PosterSize);
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
