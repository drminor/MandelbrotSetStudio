using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CbshDisplayControl.xaml
	/// </summary>
	public partial class CbshDisplayControl : UserControl
	{
		private bool _clipImageBlocks;

		private CbshDisplayViewModel _vm;

		private Canvas _canvas;
		private Image _colorBandsDisplayImage;
		
		//private VectorInt _offset;
		//private double _offsetZoom;

		#region Constructor

		public CbshDisplayControl()
		{
			_canvas = new Canvas();
			_colorBandsDisplayImage = new Image();
			_clipImageBlocks = false;


			//_offset = new VectorInt(-1, -1);
			//_offsetZoom = 1;

			_vm = (CbshDisplayViewModel)DataContext;

			Loaded += CbshDisplayControl_Loaded;
			//Unloaded += MapDisplayControl_Unloaded;
			InitializeComponent();
		}

		private void CbshDisplayControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the MapDisplay UserControl is being loaded.");
				return;
			}
			else
			{
				_canvas = MainCanvas;
				_vm = (CbshDisplayViewModel)DataContext;

				UpdateTheVmWithOurSize(new SizeDbl(TopBorder.ActualWidth, TopBorder.ActualHeight));

				_vm.PropertyChanged += ViewModel_PropertyChanged;
				TopBorder.SizeChanged += Container_SizeChanged;

				_canvas.ClipToBounds = _clipImageBlocks;
				_colorBandsDisplayImage = new Image { Source = _vm.ImageSource };
				_ = _canvas.Children.Add(_colorBandsDisplayImage);
				_colorBandsDisplayImage.SetValue(Canvas.LeftProperty, (double)0);
				_colorBandsDisplayImage.SetValue(Canvas.TopProperty, (double)0);
				_colorBandsDisplayImage.SetValue(Panel.ZIndexProperty, 5);

				//SetCanvasOffset(new VectorInt(), 1);

				Debug.WriteLine("The CbshDisplay Control is now loaded.");
			}
		}

		#endregion

		#region Event Handlers

		private void Container_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateTheVmWithOurSize(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize));
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				UpdateTheCanvasSize(_vm.CanvasSize);
			}
		}

		#endregion

		#region Private Methods

		private void UpdateTheVmWithOurSize(SizeDbl size)
		{
			_vm.ContainerSize = size;
		}

		private void UpdateTheCanvasSize(SizeInt size)
		{
			_canvas.Width = size.Width;
			_canvas.Height = size.Height;
		}

		#endregion
	}
}
