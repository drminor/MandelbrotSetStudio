using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class SelectionRectangle
	{
		#region Private Fields

		//private const int DEFAULT_SELECTION_SIZE_INDEX = 4; // Translates to 1/2^4 or 1/16 or 6.25% of the display size. 64 pixels for a display size of 1024
		private const int DEFAULT_SELECTION_SIZE_INDEX = 3; // Translates to 1/2^3 or 1/8 or 12.5% of the display size. 128 pixels for a display size of 1024

		private const int PITCH_TARGET = 16;
		private const int DRAG_TRIGGER_DIST = 3;
		private const int MINIMUM_SELECTION_EXTENT = 8;

		private readonly Canvas _canvas;

		private SizeDbl _displaySize;

		private int _pitch;
		private SizeDbl _adjustedDisplaySize;
		private SizeDbl _defaultSelectionSize;

		private readonly Rectangle _selectedArea;
		private readonly Line _dragLine;

		private bool _selecting;
		private DragState _dragState;

		private Point _dragAnchor;
		private bool _haveMouseDown;

		private int _selectedSizeIndex;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public SelectionRectangle(Canvas canvas, SizeDbl displaySize)
		{
			_dragState = DragState.None;

			_canvas = canvas;
			_displaySize = displaySize;

			_pitch = RMapHelper.CalculatePitch(_displaySize, PITCH_TARGET);

			_selectedSizeIndex = DEFAULT_SELECTION_SIZE_INDEX;
			var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);

			_adjustedDisplaySize = RMapHelper.GetDisplaySizeRounded16(displaySize);
			_defaultSelectionSize = GetSelectionSize(_adjustedDisplaySize, percentageOfDisplaySize);
			_selectedArea = BuildSelectionRectangle(_canvas, _defaultSelectionSize);

			SelectedPosition = new PointDbl();

			_dragLine = BuildDragLine(_canvas);

			_selectedArea.KeyUp += SelectedArea_KeyUp;
			_dragLine.KeyUp += DragLine_KeyUp;

			canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
			canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;

			canvas.MouseWheel += Canvas_MouseWheel;
			canvas.MouseMove += Canvas_MouseMove;

			canvas.MouseEnter += Canvas_MouseEnter;
			canvas.MouseLeave += Canvas_MouseLeave;

			canvas.Focusable = true;
		}

		private Rectangle BuildSelectionRectangle(Canvas canvas, SizeDbl selectionSize)
		{
			var result = new Rectangle()
			{
				Width = selectionSize.Width,
				Height = selectionSize.Height,
				Fill = Brushes.Transparent,
				Stroke = DrawingHelper.BuildSelectionDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Panel.ZIndexProperty, 10);

			return result;
		}

		private Line BuildDragLine(Canvas canvas)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent,
				Stroke = DrawingHelper.BuildSelectionDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Panel.ZIndexProperty, 20);

			return result;
		}

		#endregion

		#region Events

		internal event EventHandler<AreaSelectedEventArgs>? AreaSelected;
		internal event EventHandler<ImageDraggedEventArgs>? ImageDragged;

		#endregion

		#region Public Properties

		public bool IsEnabled { get; set; }

		// Same as ContentViewportSize - i.e., logical size or canvas size, not the size of the container
		public SizeDbl DisplaySize
		{
			get => _displaySize;

			set
			{
				var previousValue = _displaySize;
				_displaySize = value;

				if (!_displaySize.IsNAN() & !_displaySize.IsNearZero())
				{
					_pitch = RMapHelper.CalculatePitch(_displaySize, PITCH_TARGET);
					_adjustedDisplaySize = RMapHelper.GetDisplaySizeRounded16(_displaySize);

					//_selectedSizeIndex = DEFAULT_SELECTION_SIZE_INDEX;
					//var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);
					//_defaultSelectionSize = GetSelectionSize(_adjustedDisplaySize, percentageOfDisplaySize);
				}
				else
				{
					_pitch = PITCH_TARGET;
					_adjustedDisplaySize = SizeDbl.NaN;
				}

				_selectedSizeIndex = DEFAULT_SELECTION_SIZE_INDEX;
				var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);
				_defaultSelectionSize = GetSelectionSize(_adjustedDisplaySize, percentageOfDisplaySize);

				Debug.WriteLineIf(_useDetailedDebug, $"SelectionRectangle is having its DisplaySize updated from: {previousValue} to {value}. The new SelectionSize is {_defaultSelectionSize}.");

				_selectedArea.Width = _defaultSelectionSize.Width;
				_selectedArea.Height = _defaultSelectionSize.Height;
			}
		}

		public RectangleDbl Area
		{
			get
			{
				var p = SelectedPosition;
				var s = SelectedSize;
				var result = new RectangleDbl(p, s);

				return result;
			}
		}

		public VectorInt ZoomPoint
		{
			get
			{
				var selectionCenter = Area.GetCenter();

				var startP = new PointDbl(_canvas.ActualWidth / 2, _canvas.ActualHeight / 2);
				var endP = new PointDbl(selectionCenter.X, selectionCenter.Y);
				var vectorDbl = endP.Diff(startP);
				var result = vectorDbl.Round();

				return result;

			}
		}

		#endregion

		#region Private Properties

		private bool Selecting
		{
			get => _selecting;

			set
			{
				if (_selecting != value)
				{
					if (value)
					{
						_selectedArea.Visibility = Visibility.Visible;
						_selectedArea.Focus();
					}
					else
					{
						_selectedArea.Visibility = Visibility.Hidden;
						_selectedArea.Width = _defaultSelectionSize.Width;
						_selectedArea.Height = _defaultSelectionSize.Height;

						AreaSelected?.Invoke(this, AreaSelectedEventArgs.CreateCancelPreviewInstance(TransformType.ZoomIn));
					}

					_selecting = value;
				}
			}
		}

		private DragState DragState
		{
			get => _dragState;
			set
			{
				if (_dragState != value)
				{
					if (value == DragState.None)
					{
						_dragLine.Visibility = Visibility.Hidden;
					}
					else if (value == DragState.Begun)
					{
						_dragLine.Visibility = Visibility.Visible;
						_dragLine.Focus();
					}
					else
					{
						_dragLine.Visibility = Visibility.Visible;

						// TODO: Check This -- is this needed?>
						_dragLine.Focus();
					}

					_dragState = value;
				}
			}
		}

		private PointDbl SelectedPosition
		{
			get
			{
				var x = SelectedAreaX;
				var y = SelectedAreaYInverted;

				return new PointDbl(double.IsNaN(x) ? 0 : x, double.IsNaN(y) ? 0 : y);
			}

			set
			{
				_selectedArea.SetValue(Canvas.LeftProperty, value.X);
				_selectedArea.SetValue(Canvas.BottomProperty, value.Y);
			}
		}

		private double SelectedAreaX => (double)_selectedArea.GetValue(Canvas.LeftProperty);
		private double SelectedAreaYInverted => (double)_selectedArea.GetValue(Canvas.BottomProperty);

		private SizeDbl SelectedSize
		{
			get
			{
				var result = new SizeDbl(_selectedArea.Width, _selectedArea.Height);
				//Debug.WriteLine($"The selected size is {result}.");
				return result;
			}

			set
			{
				//Debug.WriteLine($"The selected size is being updated to {value}.");
				_selectedArea.Width = value.Width;
				_selectedArea.Height = value.Height;
			}
		}

		private Point DragLineTerminus
		{
			get => new(_dragLine.X2, _dragLine.Y2);
			set
			{
				_dragLine.X2 = value.X;
				_dragLine.Y2 = value.Y;
			}
		}

		#endregion

		#region Public Methods

		public void TearDown()
		{
			try
			{
				_canvas.MouseLeftButtonUp -= Canvas_MouseLeftButtonUp;
				_canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;

				_canvas.MouseWheel -= Canvas_MouseWheel;
				_canvas.MouseMove -= Canvas_MouseMove;

				_canvas.MouseEnter -= Canvas_MouseEnter;
				_canvas.MouseLeave -= Canvas_MouseLeave;
			}
			catch
			{
				Debug.WriteLine("SelectionRectangle encountered an exception in TearDown.");
			}
		}

		#endregion

		#region Event Handlers

		private void SelectedArea_KeyUp(object sender, KeyEventArgs e)
		{
			//Debug.WriteLine($"The {e.Key} was pressed on the Selected Area.");

			if (!Selecting)
			{
				//Debug.WriteLine($"The {e.Key} was pressed, but we are not active, returning.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				Selecting = false;
			}
		}

		private void DragLine_KeyUp(object sender, KeyEventArgs e)
		{
			if (DragState == DragState.None)
			{
				//Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- not in drag.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				//Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- cancelling drag.");
				DragState = DragState.None;

				ImageDragged?.Invoke(this, ImageDraggedEventArgs.CreateCancelPreviewInstance(TransformType.Pan));
			}
		}

		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!Selecting)
			{
				return;
			}

			if (e.Delta < 0)
			{
				// Reverse roll, zooms out.
				if (TryGetNewSelectionSize(ref _selectedSizeIndex, zoomIn: false, out var newSelectionSize))
				{
					PointDbl posYInverted;

					if (_selectedSizeIndex == 0)
					{
						posYInverted = new PointDbl(_canvas.ActualWidth, _canvas.ActualHeight).Scale(0.5);
					}
					else
					{
						var controlPos = e.GetPosition(relativeTo: _canvas);
						posYInverted = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
					}

					var pos = new PointDbl(posYInverted.X - newSelectionSize.Value.Width / 2, posYInverted.Y - newSelectionSize.Value.Height / 2);
					var newSelection = new RectangleDbl(pos, newSelectionSize.Value);

					_ = MoveAndSize(newSelection);
					e.Handled = true;
				}
			}
			else if (e.Delta > 0)
			{
				// Forward roll, zooms in.
				if (TryGetNewSelectionSize(ref _selectedSizeIndex, zoomIn: true, out var newSelectionSize))
				{
					var controlPos = e.GetPosition(relativeTo: _canvas);
					var posYInverted = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
					var pos = new PointDbl(posYInverted.X - newSelectionSize.Value.Width / 2, posYInverted.Y - newSelectionSize.Value.Height / 2);
					var newSelection = new RectangleDbl(pos, newSelectionSize.Value);

					_ = MoveAndSize(newSelection);
					e.Handled = true;
				}
			}
			else
			{
				//Debug.WriteLine("MouseWheel, but no change.");
			}
		}

		private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_haveMouseDown = true;

			if (DragState == DragState.None)
			{
				_dragAnchor = e.GetPosition(relativeTo: _canvas);
			}
		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (Selecting)
			{
				HandleSelectionMove(e);
			}
			else
			{
				HandleDragMove(e);
			}
		}

		private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!IsEnabled)
			{
				//Debug.WriteLine($"Section Rectangle is getting a MouseLeftButtonUp event -- we are disabled.");
				return;
			}

			//Debug.WriteLine($"Section Rectangle is getting a MouseLeftButtonUp event. IsFocused = {_canvas.IsFocused}. Have a mouse down event = {_haveMouseDown}, DragState = {DragState}, Selecting = {Selecting}");

			if (DragState == DragState.None)
			{
				if (_haveMouseDown)
				{
					_haveMouseDown = false;
					HandleSelectionRect(e);
				}
			}
			else
			{
				HandleDragLine(e);
			}
		}

		private void Canvas_MouseLeave(object sender, MouseEventArgs e)
		{
			//_haveMouseDown = false;
			if (Selecting)
			{
				_selectedArea.Visibility = Visibility.Hidden;
			}

			else if (DragState == DragState.Begun)
			{
				DragState = DragState.None;
				_dragLine.Visibility = Visibility.Hidden;
			}

			else if (DragState == DragState.InProcess)
			{
				_dragLine.Visibility = Visibility.Hidden;
			}
		}

		private void Canvas_MouseEnter(object sender, MouseEventArgs e)
		{
			if (Selecting)
			{
				_selectedArea.Visibility = Visibility.Visible;

				if (!_selectedArea.Focus())
				{
					Debug.WriteLine("WARNING: Canvas Enter did not move the focus to the SelectedRectangle.");
				}
			}
			
			else if (DragState != DragState.None)
			{
				_dragLine.Visibility = Visibility.Visible;

				if (!_dragLine.Focus())
				{
					Debug.WriteLine("WARNING: Canvas Enter did not move the focus to the DragLine.");
				}
			}
		}

		#endregion

		#region Private Methods

		private void HandleSelectionMove(MouseEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			// Invert the y coordinate.
			var posYInverted = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			Move(posYInverted);
		}

		private void HandleSelectionRect(MouseButtonEventArgs e)
		{
			// The controlPos is the center of the selection Rectangle
			var controlPos = e.GetPosition(relativeTo: _canvas);

			// The canvas has coordinates where the y value increases from top to bottom.
			var posYInverted = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);

			if (!Selecting)
			{
				//Debug.WriteLine($"Activating Select at {posYInverted}.");
				Activate(posYInverted);
			}
			else
			{
				if (_selectedArea.IsMouseOver) 
				{
					//Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = True.");

					var area = StopSelecting();
					RaiseAreaSelected(area, isPreview: false);
				}
				else
				{
					Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = False.");
				}
			}
		}

		private void Activate(PointDbl position)
		{
			Selecting = true;
			Move(position);

			if (!_selectedArea.Focus())
			{
				Debug.WriteLine("WARNING: Activate did not move the focus to the SelectedRectangle");
			}
		}

		private RectangleDbl StopSelecting()
		{
			var snapShotOfArea = Area;
			Selecting = false;

			return snapShotOfArea;
		}


		/// <summary>
		/// Reposition the Selection Rectangle, keeping it's current size.
		/// </summary>
		/// <param name="posYInverted">The current mouse position, aka the center position of the selection rectangle</param>
		private void Move(PointDbl posYInverted)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, free form.");
			//ReportPosition(posYInverted);

			var x = posYInverted.X - (_selectedArea.Width / 2);
			var y = posYInverted.Y - (_selectedArea.Height / 2);

			if (x < 0)
			{
				x = 0;
			}

			if (x + _selectedArea.Width > _canvas.ActualWidth)
			{
				x = _canvas.ActualWidth - _selectedArea.Width;
			}

			if (y < 0)
			{
				y = 0;
			}

			if (y + _selectedArea.Height > _canvas.ActualHeight)
			{
				y = _canvas.ActualHeight - _selectedArea.Height;
			}

			// x and y are the lower, left-hand point of the selection rectangle

			if (ScreenTypeHelper.IsDoubleChanged(x, SelectedAreaX, 0.01) || ScreenTypeHelper.IsDoubleChanged(y, SelectedAreaYInverted, 0.01))
			//if (double.IsNaN(cLeft) || Math.Abs(x - cLeft) > 0.01 || double.IsNaN(cBot) || Math.Abs(y - cBot) > 0.01)
			{
				SelectedPosition = new PointDbl(x, y);

				RaiseAreaSelected(Area, isPreview: true);
			}
		}

		/// <summary>
		/// Reposition the Selection Rectangle and update its size.
		/// </summary>
		/// <param name="selection">The screen coordinates of the new selection</param>
		/// <returns></returns>
		private bool MoveAndSize(RectangleDbl selection)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, with size: {size}");

			var posYInverted = selection.Position; // Lower, left-hand corner
			var size = selection.Size;

			if (posYInverted.X < 0
				|| posYInverted.Y < 0
				|| posYInverted.X + size.Width > _canvas.ActualWidth
				|| posYInverted.Y + size.Height > _canvas.ActualHeight)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"SelectionRectangle is not moving. The new selection is : {selection}.");
				return false;
			}

			var selectedPositionWasUpdated = false;

			if (ScreenTypeHelper.IsDoubleChanged(posYInverted.X, SelectedAreaX) || ScreenTypeHelper.IsDoubleChanged(posYInverted.Y, SelectedAreaYInverted))
			///if (double.IsNaN(cPos.X) || Math.Abs(posYInverted.X - cPos.X) > 0.01 || double.IsNaN(cPos.Y) || Math.Abs(posYInverted.Y - cPos.Y) > 0.01)
			{
				SelectedPosition = posYInverted;
				selectedPositionWasUpdated = true;
			}

			var selectedSizeWasUpdated = false;
			//var cSize = SelectedSize;
			//if (Math.Abs(size.Width - cSize.Width) > 0.01 || Math.Abs(size.Height - cSize.Height) > 0.01)
			if (ScreenTypeHelper.IsDoubleChanged(size.Width, _selectedArea.Width, 0.01) || ScreenTypeHelper.IsDoubleChanged(size.Height, _selectedArea.Height))
			{
				var previousSize = SelectedSize;
				SelectedSize = size;
				selectedSizeWasUpdated = true;

				Debug.WriteLineIf(_useDetailedDebug, $"SelectionRectangle is having its SelectionSize updated from: {previousSize} to {size}.");
			}

			if (selectedPositionWasUpdated | selectedSizeWasUpdated)
			{
				RaiseAreaSelected(Area, isPreview: true);
			}

			return selectedPositionWasUpdated;
		}

		private void RaiseAreaSelected(RectangleDbl area, bool isPreview)
		{
			var displaySize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);
			var zoomPoint = GetDistanceFromCanvasCenter(area, displaySize);
			var factor = RMapHelper.GetSmallestScaleFactor(area.Size, displaySize);

			var areaSelectedEventArgs = new AreaSelectedEventArgs(TransformType.ZoomIn, zoomPoint, factor, area, displaySize, _adjustedDisplaySize, isPreview);

			var previewMessage = isPreview ? " PREVIEW " : string.Empty;
			Debug.WriteLineIf(_useDetailedDebug, $"Raising AreaSelected {previewMessage} with position: {zoomPoint} and factor: {factor}");

			AreaSelected?.Invoke(this, areaSelectedEventArgs);
		}

		// Return the distance from the Canvas Center to the new mouse position.
		private VectorInt GetDistanceFromCanvasCenter(RectangleDbl area, SizeDbl displaySize)
		{
			Debug.Assert(area.Width > 0 && area.Height > 0, "Selction Rectangle has a zero or negative value its width or height.");

			var canvasCenter = displaySize.Scale(0.5); // new PointDbl(_canvas.ActualWidth / 2, _canvas.ActualHeight / 2);
			var selectionCenter = area.GetCenter();
			var vectorDbl = selectionCenter.Sub(canvasCenter);

			var result = vectorDbl.Round();

			return result;
		}

		private void HandleDragMove(MouseEventArgs e)
		{
			if (!IsEnabled)
			{
				return;
			}

			if (DragState == DragState.None)
			{
				if (_haveMouseDown && e.LeftButton == MouseButtonState.Pressed)
				{
					var controlPos = e.GetPosition(relativeTo: _canvas);
					var dist = _dragAnchor - controlPos;

					if (Math.Abs(dist.Length) > DRAG_TRIGGER_DIST)
					{
						_dragLine.X1 = _dragAnchor.X;
						_dragLine.Y1 = _dragAnchor.Y;
						_dragLine.X2 = _dragAnchor.X;
						_dragLine.Y2 = _dragAnchor.Y;

						DragState = DragState.Begun;
						_haveMouseDown = false;
						_dragLine.Focus();
					}
				}
			}
			else
			{
				var controlPos = e.GetPosition(relativeTo: _canvas);
				SetDragPosition(controlPos);
			}
		}

		private void HandleDragLine(MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			if (DragState == DragState.Begun)
			{
				DragState = DragState.InProcess;
			}
			else
			{
				DragState = DragState.None;
				var offset = GetDragOffset(DragLineTerminus);
				ImageDragged?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset, isPreview: false));
			}
		}

		// Position the current end of the drag line
		private void SetDragPosition(Point controlPos)
		{
			var dist = controlPos - _dragAnchor;

			// Horizontal
			var x = dist.X;

			x = _dragAnchor.X + x;

			if (x < 0)
			{
				x += _pitch;
			}

			if (x > _canvas.ActualWidth)
			{
				x -= _pitch;
			}

			// Vertical
			var y = dist.Y;

			y = _dragAnchor.Y + y;

			if (y < 0)
			{
				y += _pitch;
			}

			if (y > _canvas.ActualWidth)
			{
				y -= _pitch;
			}

			var dragLineTerminus = DragLineTerminus;
			var newDlt = new Point(x, y);

			if ( (dragLineTerminus - newDlt).LengthSquared > 0.05 )
			{
				DragLineTerminus = newDlt;

				var offset = GetDragOffset(DragLineTerminus);
				ImageDragged?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset, isPreview: true));
			}
		}

		// Return the distance from the DragAnchor to the new mouse position.
		private VectorInt GetDragOffset(Point controlPos)
		{
			var startP = new PointDbl(_dragAnchor.X, _canvas.ActualHeight - _dragAnchor.Y);
			var endP = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			var vectorDbl = endP.Diff(startP);
			var result = vectorDbl.Round();

			return result;
		}

		private bool TryGetNewSelectionSize(ref int selectedSizeIndex, bool zoomIn, [NotNullWhen(true)] out SizeDbl? newSelectionSize)
		{
			if (zoomIn)
			{
				var newSelectionSizeIndex = selectedSizeIndex + 1;

				var percentageOfDisplaySize = Math.Pow(2, -1 * newSelectionSizeIndex);
				var selectionSize = GetSelectionSize(_adjustedDisplaySize, percentageOfDisplaySize);

				if (selectionSize.Width <= MINIMUM_SELECTION_EXTENT || selectionSize.Height <= MINIMUM_SELECTION_EXTENT)
				{
					// Cannot zoom in any futher.
					newSelectionSize = null;
					return false;
				}

				selectedSizeIndex = newSelectionSizeIndex;
				newSelectionSize = selectionSize;
			}
			else
			{
				if (_selectedSizeIndex == 1)
				{
					// Cannot zoom out any further.
					newSelectionSize = null;
					return false;
				}

				_selectedSizeIndex--;

				var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);
				newSelectionSize = GetSelectionSize(_adjustedDisplaySize, percentageOfDisplaySize);
			}

			//var pos = new PointDbl(SelectedAreaX - result.Width / 2, SelectedAreaYInverted - result.Height / 2);
			//newSelectionSize = new RectangleDbl(pos, result);
			return true;
		}

		private SizeDbl GetSelectionSize(SizeDbl adjustedDisplaySize, double percentageOfDisplaySize)
		{
			//SizeDbl result;

			if (adjustedDisplaySize.IsNAN() || adjustedDisplaySize.IsNearZero())
			{
				return new SizeDbl(128);
			}

			if (percentageOfDisplaySize == 1)
			{
				return adjustedDisplaySize;
			}

			//var displaySizeRoundedTo16 = GetDisplaySizeRounded16(displaySize);
			//var result = displaySizeRoundedTo16.Scale(percentageOfDisplaySize);

			var result = adjustedDisplaySize.Scale(percentageOfDisplaySize);


			//var w = displaySize.Width;
			//var h = displaySize.Height;

			//if (w >= h)
			//{
			//	var adjustedHeight = RoundToNearestMulOf16(h);
			//	var selectedHeight = adjustedHeight * percentageOfDisplaySize;
			//	var selectedWidth = selectedHeight * (w / h);
			//	result = new SizeDbl(selectedWidth, selectedHeight);
			//}
			//else
			//{
			//	var adjustedWidth = RoundToNearestMulOf16(w);
			//	var selectedWidth = adjustedWidth * percentageOfDisplaySize;
			//	var selectedHeight = selectedWidth * (h / w);
			//	result = new SizeDbl(selectedWidth, selectedHeight);
			//}

			return result;
		}

		#endregion

		#region Not Used

		//private void SetMousePosition(Point posYInverted)
		//{
		//	var position = new Point(posYInverted.X, _canvas.ActualHeight - posYInverted.Y);
		//	var canvasPos = GetCanvasPosition();
		//	var pos = new Point(position.X + canvasPos.X, position.Y + canvasPos.Y);

		//	var source = (HwndSource)PresentationSource.FromVisual(_canvas);
		//	var hWnd = source.Handle; 
		//	var _ = Win32.PositionCursor(hWnd, pos);

		//	//Debug.WriteLine($"Activating to canvas:{position}, inv:{posYInverted}, screen:{screenPos}");
		//}

		//private Point GetCanvasPosition()
		//{
		//	var generalTransform = _canvas.TransformToAncestor(Application.Current.MainWindow);
		//	var relativePoint = generalTransform.Transform(new Point(0, 0));

		//	return relativePoint;
		//}

		//private void ReportPosition(Point posYInverted)
		//{
		//	var position = new Point(posYInverted.X, _canvas.ActualHeight - posYInverted.Y);
		//	var canvasPos = GetCanvasPosition();
		//	var pos = new Point(position.X + canvasPos.X, position.Y + canvasPos.Y);

		//	HwndSource source = (HwndSource)PresentationSource.FromVisual(_canvas);
		//	IntPtr hWnd = source.Handle;
		//	var screenPos = Win32.TranslateToScreen(hWnd, pos);

		//	Debug.WriteLine($"Mouse moved to canvas:{position}, inv:{posYInverted}, screen:{screenPos}");
		//}

		//private bool IsShiftKey()
		//{
		//	return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
		//}

		//private bool IsCtrlKey()
		//{
		//	return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		//}

		//private bool IsAltKey()
		//{
		//	return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
		//}

		//private void SelectedArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//	var position = e.GetPosition(relativeTo: _canvas);

		//	Debug.WriteLine($"The SelectionRectangle is getting a Mouse Left Button Down at {position}.");
		//}

		//private void SelectedArea_MouseWheel(object sender, MouseWheelEventArgs e)
		//{
		//	Debug.WriteLine("The SelectionRectangle received a MouseWheel event.");
		//}

		#endregion
	}
}
