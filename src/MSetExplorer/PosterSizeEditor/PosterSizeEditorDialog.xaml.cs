using MSS.Types;
using System;
using System.Diagnostics;
//using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
		private Rectangle _newImageRectangle;
		private Rectangle _clipRectangle;
		private Border? _border;

		private PosterSizeEditorViewModel _vm;

		#region Constructor

		public PosterSizeEditorDialog()
		{
			_canvas = new Canvas();
			_image = new Image();
			_newImageRectangle = new Rectangle();
			_clipRectangle = new Rectangle();
			_showBorder = false;

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
				_image.SetValue(Panel.ZIndexProperty, 10);

				var sizeDbl = ScreenTypeHelper.ConvertToSizeDbl(_canvas.RenderSize);
				UpdateTheVmWithOurSize(sizeDbl);

				_newImageRectangle = BuildNewImageRectangle(_canvas, _vm.LayoutInfo.NewImageArea);
				_clipRectangle = BuildClipRectangle(_canvas, new RectangleDbl(1, 1, 2, 2));

				// A border is helpful for troubleshooting.
				_border = _showBorder ? BuildBorder(_canvas) : null;
				UpdateTheBorderSize(sizeDbl.Round());

				_vm.PreserveAspectRatio = true;

				Debug.WriteLine("The PosterSizeEditor Dialog is now loaded");
			}
		}

		private Rectangle BuildNewImageRectangle(Canvas canvas, RectangleDbl newImageSizeArea)
		{
			var result = new Rectangle
			{
				Width = newImageSizeArea.Width,
				Height = newImageSizeArea.Height,
				Fill = Brushes.Gray
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Canvas.LeftProperty, newImageSizeArea.X1);
			result.SetValue(Canvas.BottomProperty, newImageSizeArea.Y1);
			result.SetValue(Panel.ZIndexProperty, 5);

			return result;
		}

		private Rectangle BuildClipRectangle(Canvas canvas, RectangleDbl clip)
		{
			var result = new Rectangle
			{
				Width = clip.Width,
				Height = clip.Height,
				Fill = Brushes.Transparent,
				Stroke = Brushes.Red,
				StrokeThickness = 0.5
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Canvas.LeftProperty, clip.X1);
			result.SetValue(Canvas.BottomProperty, clip.Y1);
			result.SetValue(Panel.ZIndexProperty, 15);

			return result;
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
				SetOriginalImageOffset(_vm.LayoutInfo.OriginalImageArea.Position);
				ClipOriginalImage(_vm.LayoutInfo.PreviewImageClipRegionYInverted);
				DrawClipRectangle(_vm.LayoutInfo.PreviewImageClipRegion);

				DrawNewImageSizeRectangle(_vm.LayoutInfo.NewImageArea);
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

		private void SetOriginalImageOffset(PointDbl value)
		{
			Debug.WriteLine($"The original image is being placed at {value}.");
			_image.SetValue(Canvas.LeftProperty, value.X);
			_image.SetValue(Canvas.BottomProperty, value.Y);
		}

		private void ClipOriginalImage(RectangleDbl clipRegion)
		{

			Debug.WriteLine($"Clip region is {clipRegion}.");
			var rect = ScreenTypeHelper.ConvertToRect(clipRegion);
			_image.Clip = new RectangleGeometry(rect);
		}

		private void DrawNewImageSizeRectangle(RectangleDbl newImageArea)
		{
			Debug.WriteLine($"The new image is being placed at {newImageArea}.");
			_newImageRectangle.Width = Math.Max(newImageArea.Width, 0);
			_newImageRectangle.Height = Math.Max(newImageArea.Height, 0);
			_newImageRectangle.SetValue(Canvas.LeftProperty, newImageArea.X1);
			_newImageRectangle.SetValue(Canvas.BottomProperty, newImageArea.Y1);
		}

		private void DrawClipRectangle(RectangleDbl clipRegion)
		{
			//Debug.WriteLine($"The new image is being placed at {clipRegion}.");
			_clipRectangle.Width = Math.Max(clipRegion.Width, 0);
			_clipRectangle.Height = Math.Max(clipRegion.Height, 0);
			_clipRectangle.SetValue(Canvas.LeftProperty, clipRegion.X1);
			_clipRectangle.SetValue(Canvas.BottomProperty, clipRegion.Y1);
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
