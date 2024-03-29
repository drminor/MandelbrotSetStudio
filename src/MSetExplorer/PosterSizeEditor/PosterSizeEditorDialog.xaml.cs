﻿using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
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

		private MapAreaInfo2 _initialPosterMapAreaInfo;
		private SizeDbl _initialPosterSize;

		#region Constructor

		public PosterSizeEditorDialog(MapAreaInfo2 posterMapAreaInfo, SizeDbl posterSize)
		{
			_initialPosterMapAreaInfo = posterMapAreaInfo.Clone();
			_initialPosterSize = posterSize;

			_canvas = new Canvas();
			_image = new Image();
			_newImageRectangle = new Rectangle();
			_clipRectangle = new Rectangle();
			_showBorder = false;

			

			_vm = (PosterSizeEditorViewModel)DataContext;
			Loaded += PosterSizeEditorDialog_Loaded;
			Closing += PosterSizeEditorDialog_Closing;
			InitializeComponent();
		}

		private void PosterSizeEditorDialog_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			_vm.CancelPreviewImageGeneration();
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

				var s1 = new Size(_canvas.ActualWidth, _canvas.ActualHeight);

				_image = new Image { Source = _vm.PreviewImage };
				_ = canvas1.Children.Add(_image);

				var s2 = new Size(_canvas.ActualWidth, _canvas.ActualHeight);
				var previewImageSize = new SizeDbl(_vm.PreviewImage.Width, _vm.PreviewImage.Height);
				var controlSize = new SizeDbl(ActualWidth, ActualHeight);
				Debug.WriteLine
					(
						$"PosterSizeEditor: CanvasSize before: {s1}, CanvasSize after adding the PreviewImage: {s2}. Image Size: {previewImageSize}. Container Size: {_canvas.RenderSize}. Control Size: {controlSize}."
					);

				_image.SetValue(Panel.ZIndexProperty, 10);

				_newImageRectangle = BuildNewImageRectangle(_canvas, new RectangleDbl(1, 1, 2, 2));
				_clipRectangle = BuildClipRectangle(_canvas, new RectangleDbl(1, 1, 2, 2));

				// Initialize the ViewModel 
				var containerSize = _canvas.RenderSize;
				_vm.Initialize(_initialPosterMapAreaInfo, containerSize, _initialPosterSize);

				_canvas.SizeChanged += CanvasSize_Changed;

				// A border is helpful for troubleshooting.
				_border = _showBorder ? BuildBorder(_canvas) : null;
				UpdateTheBorderSize(containerSize);

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

		#region Public Properties

		public event EventHandler? ApplyChangesRequested;

		public MapAreaInfo2? PosterMapAreaInfo => _vm.PosterMapAreaInfo;

		public RectangleDbl NewMapArea => _vm.NewMapArea;
		public SizeDbl NewMapSize => _vm.NewMapSize;

		#endregion

		#region Public Methods

		public void UpdateWithNewMapInfo(MapAreaInfo2 mapAreaInfo, SizeDbl posterSize)
		{
			var containerSize = _canvas.RenderSize;
			_vm.UpdateWithNewMapInfo(mapAreaInfo, containerSize, posterSize);
		}

		#endregion

		#region Event Handlers

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PosterSizeEditorViewModel.LayoutInfo))
			{
				var previewImageSize = new SizeDbl(_vm.PreviewImage.Width, _vm.PreviewImage.Height);
				Debug.WriteLine($"Here at PosterSizeEditor - Code Behind - LayoutInfo property changed. ContainerSize: {_vm.ContainerSize}, PreviewImageSize: {previewImageSize}, PreviewImageScaleFactor: {_vm.LayoutInfo.ScaleFactorForPreviewImage}.");


				SetOriginalImageOffset(_vm.LayoutInfo.OriginalImageArea.Position);

				ClipOriginalImage(_vm.LayoutInfo.PreviewImageClipRegionYInverted);
				DrawClipRectangle(_vm.LayoutInfo.PreviewImageClipRegion);

				DrawNewImageSizeRectangle(_vm.LayoutInfo.NewImageArea);
			}
		}

		private void CanvasSize_Changed(object sender, SizeChangedEventArgs e)
		{
			UpdateTheVmWithOurSize(e.NewSize);
			UpdateTheBorderSize(e.NewSize);
		}

		#endregion

		#region Private Methods

		private void UpdateTheVmWithOurSize(Size size)
		{
			var sizeDbl = ScreenTypeHelper.ConvertToSizeDbl(size);
			if (_border != null)
			{
				sizeDbl = sizeDbl.Inflate(8);
			}

			_vm.ContainerSize = sizeDbl;
		}

		private void UpdateTheBorderSize(Size size)
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

			//Debug.WriteLine($"Clip region is {clipRegion}.");
			var rect = ScreenTypeHelper.ConvertToRect(clipRegion);
			_image.Clip = new RectangleGeometry(rect);
		}

		private void DrawNewImageSizeRectangle(RectangleDbl newImageArea)
		{
			//Debug.WriteLine($"The new image is being placed at {newImageArea}.");
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

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void ApplyChangesButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.CancelPreviewImageGeneration();
			ApplyChangesRequested?.Invoke(this, new EventArgs());
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion
	}
}
