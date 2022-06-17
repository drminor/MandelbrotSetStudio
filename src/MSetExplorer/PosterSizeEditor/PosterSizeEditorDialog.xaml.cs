using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for PosterSizeEditorWindow.xaml
	/// </summary>
	public partial class PosterSizeEditorDialog : Window
	{
		private bool _showBorder;
		private Canvas _canvas;
		private ScaleTransform _scaleTransform;
		private Border? _border;

		private PosterSizeEditorViewModel _vm;

		#region Constructor

		public PosterSizeEditorDialog()
		{
			_canvas = new Canvas();
			_scaleTransform = new ScaleTransform(0.5, 0.5);

			_showBorder = true;

			_vm = (PosterSizeEditorViewModel)DataContext;
			Loaded += PosterSizeEditorDialog_Loaded;
			InitializeComponent();
		}

		private void PosterSizeEditorDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the PosterSizeEditor Dialog is being loaded.");
				return;
			}
			else
			{
				_vm = (PosterSizeEditorViewModel)DataContext;
				_canvas = canvas1;
				_canvas.SizeChanged += CanvasSize_Changed;

				var image = new Image { Source = _vm.PreviewImage };
				_canvas.RenderTransform = _scaleTransform;
				_ = canvas1.Children.Add(image);

				// A border is helpful for troubleshooting.
				_border = _showBorder ? BuildBorder(_canvas) : null;

				var sizeDbl = ScreenTypeHelper.ConvertToSizeDbl(_canvas.RenderSize);

				UpdateTheVmWithOurSize(sizeDbl);
				UpdateTheBorderSize(sizeDbl.Round());

				Debug.WriteLine("The PosterSizeEditor Dialog is now loaded");
			}
		}

		private Border BuildBorder(Canvas canvas)
		{
			var result = new Border
			{
				Width = canvas.Width + 4,
				Height = canvas.Width + 4,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				BorderThickness = new Thickness(1),
				BorderBrush = Brushes.BlueViolet,
				Visibility = Visibility.Visible
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Canvas.LeftProperty, -2d);
			result.SetValue(Canvas.TopProperty, -2d);
			result.SetValue(Panel.ZIndexProperty, 100);

			return result;
		}

		#endregion

		#region Event Handlers

		private void CanvasSize_Changed(object sender, SizeChangedEventArgs e)
		{
			UpdateTheVmWithOurSize(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize));
			UpdateTheBorderSize(ScreenTypeHelper.ConvertToSizeInt(e.NewSize));
		}

		#endregion

		#region Public Properties

		#endregion

		#region Private Methods

		private void UpdateTheVmWithOurSize(SizeDbl size)
		{
			_scaleTransform.ScaleX = size.Width / _vm.PreviewImage.Width;
			_scaleTransform.ScaleY = size.Height / _vm.PreviewImage.Height;

			if (_border != null)
			{
				size = size.Inflate(8);
			}

			_vm.ContainerSize = size;
		}

		private void UpdateTheBorderSize(SizeInt size)
		{
			if (_border != null)
			{
				_border.Width = 4 + size.Width / _scaleTransform.ScaleX;
				_border.Height = 4 + size.Height / _scaleTransform.ScaleY;
			}
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion



	}
}
