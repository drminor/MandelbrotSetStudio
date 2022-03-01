using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private SizeInt _canvasSize;
		private VectorInt _canvasControlOffset;
		private readonly DrawingGroup _drawingGroup;
		private IScreenSectionCollection _screenSectionCollection;

		public MapDisplayViewModel(SizeInt blockSize)
		{
			_drawingGroup = new DrawingGroup();
			ImageSource = new DrawingImage(_drawingGroup);

			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();
			CanvasSize = new SizeInt(1024, 1024);

			var canvasSizeInBlocks = GetSizeInBlocks(CanvasSize, BlockSize);
			_screenSectionCollection = new ScreenSectionCollection(canvasSizeInBlocks, BlockSize, _drawingGroup);

			_ = new MapSectionCollectionBinder(_screenSectionCollection, MapSections);

			CanvasControlOffset = new VectorInt();
		}

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public SizeInt BlockSize { get; }

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				_canvasSize = value;
				Clip(new PointInt(CanvasControlOffset));
				OnPropertyChanged();
			}
		}

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set
			{
				_canvasControlOffset = value;
				Clip(new PointInt(value));
				OnPropertyChanged(); }
		}

		public ObservableCollection<MapSection> MapSections { get; }

		public IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		public void ShiftMapSections(VectorInt amount)
		{

		}

		#region Private Methods

		private SizeInt GetSizeInBlocks(SizeInt canvasSize, SizeInt blockSize)
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, blockSize);
			var result = canvasSizeInBlocks.Inflate(2);

			// Always overide the above calculation and allocate 400 sections.
			if (result.Width > 0)
			{
				result = new SizeInt(12, 12);
			}

			return result;
		}

		private void Clip(PointInt bottomLeft)
		{
			if (!(_screenSectionCollection is null))
			{
				var drawingGroupSize = _screenSectionCollection.CanvasSizeInBlocks.Scale(BlockSize);
				Rect rect = new Rect(new Point(bottomLeft.X, (drawingGroupSize.Height - CanvasSize.Height) - bottomLeft.Y), new Point(CanvasSize.Width + bottomLeft.X, drawingGroupSize.Height - bottomLeft.Y));

				//Debug.WriteLine($"The clip rect is {rect}.");
				_drawingGroup.ClipGeometry = new RectangleGeometry(rect);
			}
		}

		//private PointInt GetPointInt(Point p)
		//{
		//	return new PointDbl(p.X, p.Y).Round();
		//}

		private Point GetPoint(PointInt p)
		{
			return new Point(p.X, p.Y);
		}

		private Size GetSize(SizeInt s)
		{
			return new Size(s.Width, s.Height);
		}

		private Rect GetRect(PointInt p, SizeInt s)
		{
			return new Rect(GetPoint(p), GetSize(s));
		}

		#endregion
	}
}
