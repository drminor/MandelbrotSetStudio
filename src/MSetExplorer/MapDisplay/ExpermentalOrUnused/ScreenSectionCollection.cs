﻿using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer.MapDisplay.ExpermentalOrUnused
{
    internal class ScreenSectionCollection : IScreenSectionCollection
    {
        private readonly SizeInt _maxSizeInBlocks;

        private readonly SizeInt _blockSize;
        private readonly int _maxYPtr;

        private readonly DrawingGroup _drawingGroup;

        private readonly GeometryDrawing _foundationRectangle;
        private readonly ScreenSection[,] _screenSections;

        private SizeInt _canvasSizeInBlocks;
        private VectorInt _startIndex;

        #region Constructor

        public ScreenSectionCollection(DrawingGroup drawingGroup, SizeInt blockSize, SizeInt maxSizeInBlocks)
        {
            _drawingGroup = drawingGroup;
            _blockSize = blockSize;
            _maxSizeInBlocks = maxSizeInBlocks;

            _startIndex = new VectorInt();

            _maxYPtr = maxSizeInBlocks.Height - 1;

            _foundationRectangle = BuildFoundationRectangle(maxSizeInBlocks, _blockSize);
            _drawingGroup.Children.Add(_foundationRectangle);

            _screenSections = new ScreenSection[maxSizeInBlocks.Height, maxSizeInBlocks.Width];

            CanvasSizeInBlocks = new SizeInt(8);
        }

        #endregion

        #region Public Properties

        public VectorInt SectionIndex => _startIndex;

        public int CurrentDrawingGroupCnt => _drawingGroup.Children.Count;

        private SizeInt _allocatedBlocks;

        public SizeInt CanvasSizeInBlocks
        {
            get => _canvasSizeInBlocks;
            set
            {
                if (value.Width < 0 || value.Height < 0)
                {
                    return;
                }

                if (_canvasSizeInBlocks != value)
                {
                    _canvasSizeInBlocks = value;
                    _allocatedBlocks = RebuildScreenSections(_canvasSizeInBlocks);
                }
            }
        }

        #endregion

        #region Public Methods

        public IList<ScreenSection> GetAllScreenSections()
        {
            var result = new List<ScreenSection>();

            foreach (var blockPosition in ScreenTypeHelper.Points(_screenSections))
            {
                var screenSection = GetScreenSection(blockPosition);
                result.Add(screenSection);
            }

            return result;
        }

        public void HideScreenSections(bool rebuild = false)
        {
            if (rebuild)
            {
                _allocatedBlocks = RebuildScreenSections(_canvasSizeInBlocks);
            }
            else
            {
                foreach (var blockPosition in ScreenTypeHelper.Points(_screenSections))
                {
                    var screenSection = GetScreenSection(blockPosition);
                    if (screenSection != null)
                    {
                        screenSection.Active = false;
                    }
                }

                var missedCnt = 0;
                foreach (var imageDrawing in _drawingGroup.Children.OfType<ImageDrawing>())
                {
                    _drawingGroup.Children.Remove(imageDrawing);
                    missedCnt++;
                }
                // Log: While Hiding Screen Sections,
                Debug.WriteLine($"While Hiding Screen Sections, found {missedCnt} orphan ImageDrawings.");
                // Log: Add Spacer
                Debug.WriteLine($"Clearing Display\n\n");
                //_drawingGroup.Children.Remove
            }
        }

        private SizeInt RebuildScreenSections(SizeInt canvasSizeInBlocks)
        {
            var numberOfBlocksToAllocate = canvasSizeInBlocks.Inflate(2);

            Debug.WriteLine($"Allocating ScreenSections. Old size: {_allocatedBlocks}, new size: {numberOfBlocksToAllocate}.");
            BuildScreenSections(_screenSections, numberOfBlocksToAllocate, _blockSize, _drawingGroup);

            return numberOfBlocksToAllocate;
        }

        public void Draw(PointInt position, byte[] pixels, bool offline)
        {
            var screenSection = GetScreenSection(position);

            if (screenSection is null || pixels is null)
            {
                //Debug.WriteLine($"Not drawing section: {position} with screen index: {screenIndex}.");
                return;
            }
            else
            {
                //Debug.WriteLine($"Drawing section: {position} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}. ip = {invertedPosition}");
                if (offline)
                {
                    screenSection.DrawOffline(pixels);
                }
                else
                {
                    var invertedPosition = GetInvertedBlockPos(position);
                    screenSection.Draw(invertedPosition, pixels);
                }
            }
        }

        public void Redraw(PointInt position)
        {
            var screenSection = GetScreenSection(position);

            if (screenSection != null)
            {
                //Debug.WriteLine($"Redrawing section: {position} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");
                var invertedPosition = GetInvertedBlockPos(position);
                screenSection.ReDraw(invertedPosition);
            }
            else
            {
                //Debug.WriteLine($"Not redrwaing section: {position} with screen index: {screenIndex}.");
            }
        }

        public void Finish()
        {
            _drawingGroup.Children.Remove(_foundationRectangle);
            _drawingGroup.Children.Add(_foundationRectangle);
        }

        public bool Hide(MapSection mapSection)
        {
            var screenSection = GetScreenSection(mapSection.ScreenPosition);

            if (screenSection != null)
            {
                var result = screenSection.Hide();
                if (!result)
                {
                    Debug.WriteLine("Cannot hide an inactive mapSection.");
                }
                return result;
            }
            else
            {
                Debug.WriteLine("Cannot hide a null mapSection.");
                return false;
            }
        }

        private PointInt GetInvertedBlockPos(PointInt blockPosition)
        {
            var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

            return result;
        }

        public void Shift(VectorInt amount)
        {
            if (amount.X == 0 && amount.Y == 0)
            {
                return;
            }

            //HideScreenSections();

            var oldStartIndex = _startIndex;
            _startIndex = IndexAdd(_startIndex, amount.Invert());

            Debug.WriteLine($"The ScreenSectionCollection was shifted by {amount}. StartIndex old: {oldStartIndex}, new: {_startIndex}.");
        }

        #endregion

        #region Private Methods

        private ScreenSection GetScreenSection(PointInt blockPosition)
        {
            // Use the start index to find the "current" cell in the 2-D Ring Buffer
            var screenIndex = IndexAdd(_startIndex, new VectorInt(blockPosition));
            var result = _screenSections[screenIndex.Y, screenIndex.X];

            return result;
        }

        private VectorInt IndexAdd(VectorInt index, VectorInt amount)
        {
            index = index.Add(amount).Mod(_allocatedBlocks);

            var nx = index.X;
            var ny = index.Y;

            if (nx < 0)
            {
                nx += _allocatedBlocks.Width;
            }

            if (ny < 0)
            {
                ny += CanvasSizeInBlocks.Height;
            }

            var result = new VectorInt(nx, ny);

            return result;
        }

        private void BuildScreenSections(ScreenSection[,] currentSections, SizeInt blocksToAllocate, SizeInt blockSize, DrawingGroup drawingGroup)
        {
            if (blocksToAllocate.Width > _maxSizeInBlocks.Width || blocksToAllocate.Height > _maxSizeInBlocks.Height)
            {
                throw new ArgumentException($"The CanvasSizeInWholeBlocks cannot exceed the maximum supported value of {_maxSizeInBlocks}.");
            }

            var maxX = int.MinValue;
            var minY = int.MaxValue;

            foreach (var blockPosition in ScreenTypeHelper.Points(blocksToAllocate))
            {
                var invertedPosition = GetInvertedBlockPos(blockPosition);
                if (currentSections[blockPosition.Y, blockPosition.X] == null)
                {
                    var screenSection = new ScreenSection(invertedPosition, blockSize, drawingGroup);
                    currentSections[blockPosition.Y, blockPosition.X] = screenSection;
                }

                if (invertedPosition.X > maxX)
                {
                    maxX = invertedPosition.X;
                }

                if (invertedPosition.Y < minY)
                {
                    minY = invertedPosition.Y;
                }
            }

            var x1 = 0;
            var x2 = (maxX + 1) * blockSize.Width;
            var y1 = minY * blockSize.Height;
            var y2 = _maxSizeInBlocks.Height * blockSize.Height;
            var bounds = new RectangleInt(x1, x2, y1, y2);

            // Log: The new ScreenSectionCollection Bounds
            //Debug.WriteLine($"The new ScreenSectionCollection Bounds is {bounds}.");

            _foundationRectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(bounds));
        }

        private GeometryDrawing BuildFoundationRectangle(SizeInt sizeInBlocks, SizeInt blockSize)
        {
            var rectangle = new RectangleGeometry()
            {
                Rect = ScreenTypeHelper.CreateRect(new PointInt(), sizeInBlocks.Scale(blockSize))
            };

            var result = new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Chartreuse, 3), rectangle);

            return result;
        }

        #endregion

        public class ScreenSection
        {
            private PointInt _blockPosition;
            private readonly SizeInt _blockSize;
            private readonly Image _image;
            private readonly DrawingGroup _drawingGroup;

            private ImageDrawing _imageDrawing;
            private bool _active;

            private byte[]? _pendingPixels;

            #region Constructor

            public ScreenSection(PointInt blockPosition, SizeInt blockSize, DrawingGroup drawingGroup)
            {
                _blockPosition = blockPosition;
                _blockSize = blockSize;
                _drawingGroup = drawingGroup;
                _image = CreateImage(_blockSize.Width, _blockSize.Height);
                _imageDrawing = CreateImageDrawing(_blockPosition, _image.Source);

                _pendingPixels = null;
            }

            #endregion

            #region Public Properties

            public bool Active
            {
                get => _active;
                set
                {
                    if (value != _active)
                    {
                        if (value)
                        {
                            AddToGroup("setting Active to true");
                        }
                        else
                        {
                            RemoveFromGroup("setting Active to false");
                        }

                        _active = value;
                    }
                }
            }

            public PointInt BlockPosition
            {
                get => _blockPosition;
                private set
                {
                    if (value != _blockPosition)
                    {
                        if (Active)
                        {
                            Active = false;
                            //RemoveFromGroup("setting the BlockPosition");
                        }

                        _blockPosition = value;
                    }
                }
            }

            public PointInt ScreenPosition => ScreenTypeHelper.ConvertToPointInt(_imageDrawing.Rect.Location);

            #endregion

            #region Public Methods

            public bool Hide()
            {
                if (Active)
                {
                    Active = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Delete()
            {
                _ = Hide();
                _imageDrawing = new ImageDrawing();
            }

            public void Draw(PointInt position, byte[] pixels)
            {
                if (BlockPosition != position)
                {
                    Debug.WriteLine($"Creating new ImageDrawing for {position} while drawing. The previous value is {BlockPosition}");

                    BlockPosition = position;
                    //Active = false;
                    _imageDrawing = CreateImageDrawing(BlockPosition, _image.Source);
                }
                else
                {
                    //Debug.WriteLine($"Updating existing ImageDrawing for {position} while drawing for {position}.");
                }

                var bitmap = (WriteableBitmap)_imageDrawing.ImageSource;
                WritePixels(pixels, bitmap);
                Active = true;
            }

            public void ReDraw(PointInt position)
            {
                if (_pendingPixels != null)
                {
                    BringOnline(position, _pendingPixels);
                    _pendingPixels = null;
                }
                else
                {
                    if (BlockPosition != position)
                    {
                        // TODO: Add logic to atempt to locate the "old" bitmap.
                        //throw new InvalidOperationException("Cannot redraw a screen section at a different position.");
                        Debug.WriteLine($"WARNING: Cannot redraw a screen section at a different position.");
                        return;
                    }
                    else
                    {
                        //Debug.WriteLine($"Updating existing ImageDrawing for {position} while re-drawing.");
                    }
                }

                Active = true;
            }

            private void BringOnline(PointInt position, byte[] pixels)
            {
                if (BlockPosition != position)
                {
                    Debug.WriteLine($"Creating new ImageDrawing for {position} while re-drawing. The previous value is {BlockPosition}");

                    BlockPosition = position;
                    //Active = false;
                    _imageDrawing = CreateImageDrawing(BlockPosition, _image.Source);
                }
                else
                {
                    //Debug.WriteLine($"Updating existing ImageDrawing for {position} while re-drawing.");
                }

                var bitmap = (WriteableBitmap)_imageDrawing.ImageSource;
                WritePixels(pixels, bitmap);
            }

            public void DrawOffline(byte[] pixels)
            {
                //if (Active)
                //{
                //	throw new InvalidOperationException("Attempting to DrawOffline to an Active ScreenSection.");
                //}

                //if (BlockPosition != position)
                //{
                //	//Debug.WriteLine($"Creating new ImageDrawing for {position} while drawing off-line. The previous value is {BlockPosition}");

                //	BlockPosition = position;
                //	_imageDrawing = CreateImageDrawing(BlockPosition);
                //}
                //else
                //{
                //	//Debug.WriteLine($"Updating existing ImageDrawing for {position} while drawing off-line.");
                //	_imageDrawing = CreateImageDrawing(BlockPosition);
                //}

                //var bitmap = (WriteableBitmap)_imageDrawing.ImageSource;
                //WritePixels(pixels, bitmap);

                _pendingPixels = pixels;
            }

            #endregion

            #region Private Methods

            // TODO: See if WritePixels can be replaced with something that updates the Bitmap's backbuffer.
            // It may be possible to have a object pool of bitmaps and then
            // 1) Get object, 2) fill object, 3) create an Image around the object. We may be able to avoid allocating new Byte[] in this fashion.
            private void WritePixels(byte[] pixels, WriteableBitmap bitmap)
            {
                var w = (int)Math.Round(bitmap.Width);
                var h = (int)Math.Round(bitmap.Height);

                Debug.Assert(w == 128 && h == 128 && pixels.Length == 4 * 128 * 128, "Pixels are not complete.");

                var rect = new Int32Rect(0, 0, w, h);
                var stride = 4 * w;
                bitmap.WritePixels(rect, pixels, stride, 0);
            }

            private ImageDrawing CreateImageDrawing(PointInt blockPosition, ImageSource imageSource)
            {
                var position = blockPosition.Scale(_blockSize);
                var rect = ScreenTypeHelper.CreateRect(position, _blockSize);
                var result = new ImageDrawing(imageSource, rect);

                return result;
            }

            //private ImageDrawing GetDiagnosticVersion(ImageDrawing original)
            //{
            //	var result = original.CloneCurrentValue();
            //	var bitmap = (WriteableBitmap)result.ImageSource;
            //	var ar = new byte[_blockSize.NumberOfCells * 4];
            //	bitmap.CopyPixels(ar, 4 * _blockSize.Width, 0);

            //	for (var rowPtr = 0; rowPtr < _blockSize.Height; rowPtr++)
            //	{
            //		var curResultPtr = rowPtr * _blockSize.Width * 4;

            //		for (var colPtr = 0; colPtr < _blockSize.Width; colPtr++)
            //		{
            //			ar[curResultPtr + 3] = 100; // Alpha is set to 100, instead of 255
            //			curResultPtr += 4;
            //		}
            //	}

            //	WritePixels(ar, bitmap);

            //	return result;
            //}

            private Image CreateImage(int w, int h)
            {
                var result = new Image
                {
                    Width = w,
                    Height = h,
                    Stretch = Stretch.None,
                    Margin = new Thickness(0),
                    Source = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null)
                };

                return result;
            }

            private void AddToGroup(string opDesc)
            {
                if (_drawingGroup.Children.Contains(_imageDrawing))
                {
                    Debug.WriteLine($"While {opDesc}, found that the section with BlockPosition: {_blockPosition} is already added.");
                }
                else
                {
                    _drawingGroup.Children.Add(_imageDrawing);
                }
            }

            private void RemoveFromGroup(string opDesc)
            {
                var result = _drawingGroup.Children.Remove(_imageDrawing);

                if (!result)
                {
                    Debug.WriteLine($"While {opDesc}, cannot remove section with BlockPosition: {_blockPosition}.");
                }
            }

            #endregion
        }

    }
}
