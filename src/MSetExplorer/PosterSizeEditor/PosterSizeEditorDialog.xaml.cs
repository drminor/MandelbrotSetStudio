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
		private readonly bool _showBorder;

		private Canvas _canvas;
		private Image _image;
		private Border? _border;

		private PosterSizeEditorViewModel _vm;

		#region Constructor

		public PosterSizeEditorDialog()
		{
			_canvas = new Canvas();
			_image = new Image();
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
				_vm.PropertyChanged += ViewModel_PropertyChanged;
				_canvas = canvas1;
				_canvas.SizeChanged += CanvasSize_Changed;

				_image = new Image { Source = _vm.PreviewImage };
				_ = canvas1.Children.Add(_image);

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

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PosterSizeEditorViewModel.LayoutInfo))
			{
				SetImageOffset(_vm.LayoutInfo.OriginalImageArea.Position);
				UpdateOffsetsXEnabled();
				UpdateOffsetsYEnabled();
			}

			if (e.PropertyName == nameof(PosterSizeEditorViewModel.PreserveWidth))
			{
				UpdateOffsetsXEnabled();
			}

			if (e.PropertyName == nameof(PosterSizeEditorViewModel.PreserveHeight))
			{
				UpdateOffsetsYEnabled();
			}
		}

		private void CanvasSize_Changed(object sender, SizeChangedEventArgs e)
		{
			UpdateTheVmWithOurSize(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize));
			UpdateTheBorderSize(ScreenTypeHelper.ConvertToSizeInt(e.NewSize));
		}

		#endregion

		#region Private Methods

		private void UpdateTheVmWithOurSize(SizeDbl size)
		{
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
				_border.Width = 4 + size.Width;
				_border.Height = 4 + size.Height;
			}
		}

		private void UpdateOffsetsXEnabled()
		{
			var offsetXAreEnabled = (!_vm.PreserveWidth) || (_vm.PreserveWidth && _vm.Width - _vm.OriginalWidth > 0);
			txtBeforeX.IsEnabled = offsetXAreEnabled;
			txtAfterX.IsEnabled = offsetXAreEnabled;
		}

		private void UpdateOffsetsYEnabled()
		{
			var offsetYAreEnabled = (!_vm.PreserveHeight) || (_vm.PreserveHeight && _vm.Height - _vm.OriginalHeight > 0);
			txtBeforeY.IsEnabled = offsetYAreEnabled;
			txtAfterY.IsEnabled = offsetYAreEnabled;
		}

		private void SetImageOffset(PointDbl value)
		{
			_image.SetValue(Canvas.LeftProperty, value.X);
			_image.SetValue(Canvas.BottomProperty, value.Y);
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
