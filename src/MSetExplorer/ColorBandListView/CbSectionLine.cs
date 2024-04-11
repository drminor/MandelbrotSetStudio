using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbSectionLine
	{
		#region Private Const Fields

		private const double STROKE_THICKNESS = 2.0;
		private const double SELECTION_LINE_ARROW_WIDTH = 7.5;
		private const double MIN_SEL_DISTANCE = 0.49;

		private static readonly Brush DIAG_LIGHT_GREEN = new SolidColorBrush(Colors.LightSeaGreen);


		private static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush DARKISH_GRAY_BRUSH = new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9));

		private static readonly Brush MIDDLIN_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xcc, 0xe8, 0xff));
		private static readonly Brush MEDIUM_BLUE_BRUSH = new SolidColorBrush(Colors.MediumBlue);

		private static readonly Brush IS_SELECTED_BACKGROUND = MIDDLIN_BLUE_BRUSH;
		private static readonly Brush IS_SELECTED_INACTIVE_BACKGROUND = DIAG_LIGHT_GREEN; // DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_HOVERED_STROKE = MEDIUM_BLUE_BRUSH;

		private static readonly Brush DEFAULT_STROKE = DARKISH_GRAY_BRUSH;
		private static readonly Brush DEFAULT_BACKGROUND = TRANSPARENT_BRUSH; // new SolidColorBrush(Colors.AntiqueWhite);

		#endregion

		#region Private Fields

		private readonly Canvas _canvas;
		private readonly ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private readonly SectionLineMovedCallback _sectionLineMovedCallback;

		private SizeDbl _contentScale;
		private bool _parentIsFocused;

		private Rect _sectionLineArea;
		private Rect _topArrowArea;

		//private double _x1Position;
		private double _x2Position;

		private double _selectionLinePosition;
		private double _opacity;

		private readonly Line _dragLine;
		private readonly Polygon _topArrow;
		private double _topArrowHalfWidth;

		private DragState _dragState;

		private double _originalSectionLinePosition;

		private double? _leftWidth;
		private double? _rightWidth;
		private bool _updatingPrevious;

		private bool _isSelected;
		private bool _isUnderMouse;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbSectionLine(int colorBandIndex, Rect topArrowArea, Rect sectionLineArea, ColorBandLayoutViewModel colorBandLayoutViewModel, SectionLineMovedCallback sectionLineMovedCallback)
		{
			_isSelected = false;
			_isUnderMouse = false;
			_dragState = DragState.None;

			_sectionLineMovedCallback = sectionLineMovedCallback;
			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;
			
			_canvas = colorBandLayoutViewModel.Canvas;
			_contentScale = colorBandLayoutViewModel.ContentScale;

			_topArrowArea = topArrowArea;
			_sectionLineArea = sectionLineArea;

			//_x1Position = sectionLineArea.Left;
			_x2Position = sectionLineArea.Right;

			_selectionLinePosition = _x2Position * ContentScale.Width;
			_originalSectionLinePosition = _selectionLinePosition;
			_opacity = 1.0;

			_leftWidth = null;
			_rightWidth = null;
			_updatingPrevious = false;

			_dragLine = BuildDragLine(_sectionLineArea, _isUnderMouse, ParentIsFocused, ContentScale);
			_canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 30);

			_topArrowHalfWidth = SELECTION_LINE_ARROW_WIDTH;
			_topArrow = BuildTopArrow(_topArrowArea, _isSelected, _isUnderMouse, ParentIsFocused, ContentScale);

			_topArrow.MouseUp += Handle_TopArrowMouseUp;

			_canvas.Children.Add(_topArrow);
			_topArrow.SetValue(Panel.ZIndexProperty, 30);
		}

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;

					SectionLinePositionX = SectionLineRectangleArea.Right * ContentScale.Width;
					ResizeTopArrow(TopArrowRectangleArea, ContentScale);
					//ResizeSectionLine(SectionLineRectangleArea, ContentScale);
				}
			}
		}

		public Rect SectionLineRectangleArea
		{
			get => _sectionLineArea;
			set
			{
				if (value != _sectionLineArea)
				{
					_sectionLineArea = value;
					//_x1Position = _sectionLineArea.Left;
					_x2Position = _sectionLineArea.Right;

					_dragLine.Y1 = _sectionLineArea.Top;
					_dragLine.Y2 = _sectionLineArea.Bottom;

					SectionLinePositionX = SectionLineRectangleArea.Right * ContentScale.Width;
				}
			}
		}

		public Rect TopArrowRectangleArea
		{
			get => _topArrowArea;
			set
			{
				if (value != _topArrowArea)
				{
					_topArrowArea = value;
					ResizeTopArrow(TopArrowRectangleArea, ContentScale);
				}
			}
		}

		//public double X1Position
		//{
		//	get => _x1Position;

		//	set
		//	{
		//		if (ScreenTypeHelper.IsDoubleChanged(value, _x1Position))
		//		{
		//			_x1Position = value;
		//			SectionLineRectangleArea = new Rect(value, _sectionLineArea.Y, _sectionLineArea.Width, _sectionLineArea.Height);
		//			TopArrowRectangleArea = new Rect(value, _topArrowArea.Y, _topArrowArea.Width, _topArrowArea.Height);
		//		}
		//	}
		//}

		public double X2Position
		{
			get => _x2Position;

			set
			{
				if (ScreenTypeHelper.IsDoubleChanged(value, _x2Position))
				{
					var width = value - _sectionLineArea.Left;
					_x2Position = value;
					SectionLineRectangleArea = new Rect(_sectionLineArea.X, _sectionLineArea.Y, width, _sectionLineArea.Height);
					TopArrowRectangleArea = new Rect(_sectionLineArea.X, _topArrowArea.Y, width, _topArrowArea.Height);
				}
			}
		}

		public double SectionLinePositionX
		{
			get => _selectionLinePosition;
			set
			{
				if (value != _selectionLinePosition)
				{
					var x2PTest = value / _contentScale.Width;

					if (ScreenTypeHelper.IsDoubleChanged(x2PTest, X2Position, 2))
					{
						Debug.WriteLine($"SectionLinePositionX vs X2Position MISMATCH: X2PTest: {x2PTest} / X2Position: {X2Position}. SectionLinePositionX: {SectionLinePositionX}, X2Position: {X2Position * _contentScale.Width}.");
					}

					Debug.WriteLineIf(_useDetailedDebug, $"CbSectionLine. SectionLinePositionX is being updated from: {_selectionLinePosition} to {value}.");
					_selectionLinePosition = value;
					_dragLine.X1 = value;
					_dragLine.X2 = value;

					_topArrow.Points = BuildTopAreaPoints(_selectionLinePosition, TopArrowRectangleArea.Y, TopArrowRectangleArea.Height);
				}
			}
		}

		public double Opacity
		{
			get => _opacity;
			set
			{
				if (value != _opacity)
				{
					_opacity = value;

					_dragLine.Opacity = value;
					_topArrow.Opacity = value;
				}
			}
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					_topArrow.Fill = GetTopArrowFill(_isSelected, _colorBandLayoutViewModel.ParentIsFocused);
				}
			}
		}

		public bool IsUnderMouse
		{
			get => _isUnderMouse;
			set
			{
				if (value != _isUnderMouse)
				{
					_isUnderMouse = value;

					_dragLine.Stroke = GetDragLineStroke(_isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);
					_topArrow.Stroke = GetTopArrowStroke(IsUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);
				}
			}
		}

		public bool ParentIsFocused
		{
			get => _parentIsFocused;

			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;
					_topArrow.Fill = GetTopArrowFill(IsSelected, ParentIsFocused);
					_topArrow.Stroke = GetTopArrowStroke(IsUnderMouse, ParentIsFocused);
					_dragLine.Stroke = GetDragLineStroke(IsUnderMouse, ParentIsFocused); ;
				}
			}
		}

		public Visibility TopArrowVisibility
		{
			get => _topArrow.Visibility;
			set => _topArrow.Visibility = value;
		}

		#endregion

		#region Public Methods

		public void ResizeSectionLine(Rect sectionLineArea, SizeDbl contentScale)
		{
			_dragLine.X1 = sectionLineArea.Right * contentScale.Width;
			_dragLine.X2 = sectionLineArea.Right * contentScale.Width;
			_dragLine.Y1 = sectionLineArea.Top;
			_dragLine.Y2 = sectionLineArea.Bottom;

		}

		public void ResizeTopArrow(Rect topArrowArea, SizeDbl contentScale)
		{
			_topArrow.Points = BuildTopAreaPoints(topArrowArea.Right * contentScale.Width, topArrowArea.Top, topArrowArea.Height);
		}

		public void StartDrag(double leftWidth, double rightWidth, bool updatingPrevious)
		{
			if (DragState != DragState.None)
			{
				throw new InvalidOperationException($"Cannot start Drag, the DragState = {DragState}.");
			}

			_leftWidth = leftWidth;
			_rightWidth = rightWidth;
			_updatingPrevious = updatingPrevious;
			_originalSectionLinePosition = SectionLinePositionX;

			DragState = DragState.Begun;
		}

		public void CancelDrag()
		{
			if (DragState != DragState.None)
			{
				CancelDragInternal();
			}
		}

		public void TearDown()
		{
			try
			{
				if (DragState != DragState.None)
				{
					DragState = DragState.None;
				}

				_colorBandLayoutViewModel.PropertyChanged -= ColorBandLayoutViewModel_PropertyChanged;
				_topArrow.MouseUp -= Handle_TopArrowMouseUp;

				_canvas.Children.Remove(_dragLine);
				_canvas.Children.Remove(_topArrow);
			}
			catch
			{
				Debug.WriteLine("CbSectionLine encountered an exception in TearDown.");
			}
		}

		#endregion

		#region Private Properties

		public DragState DragState
		{
			get => _dragState;
			private set
			{
				if (_dragState != value)
				{
					switch (value)
					{
						case DragState.None:

							_canvas.MouseMove -= HandleMouseMove;
							_canvas.PreviewMouseLeftButtonUp -= HandlePreviewMouseLeftButtonUp;
							ReleaseMouse();

							_leftWidth = null;
							_rightWidth = null;
							break;

						case DragState.InProcess:

							break;

						case DragState.Begun:
							CaptureMouse();
							_canvas.MouseMove += HandleMouseMove;
							_canvas.PreviewMouseLeftButtonUp += HandlePreviewMouseLeftButtonUp;
							break;

						default:
							break;
					}

					_dragState = value;
				}
			}
		}

		private bool CaptureMouse()
		{
			var result = Mouse.Capture(_canvas);
			if (result)
			{
				//Debug.WriteLine($"Beginning to Drag the SectionLine for ColorBandIndex: {ColorBandIndex}, LeftWidth: {_leftWidth}, RightWidth: {_rightWidth}.");
				_canvas.LostMouseCapture += Canvas_LostMouseCapture;
			}
			else
			{
				Debug.WriteLine($"Could not capture the mouse for ColorBandIndex: {ColorBandIndex}.");
			}

			return result;
		}

		private void ReleaseMouse()
		{
			_canvas.LostMouseCapture -= Canvas_LostMouseCapture;
			_canvas.ReleaseMouseCapture();
		}

		#endregion

		#region Event Handlers

		private void ColorBandLayoutViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandLayoutViewModel.ContentScale))
			{
				ContentScale = _colorBandLayoutViewModel.ContentScale;
				_originalSectionLinePosition = SectionLinePositionX;
			}
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ParentIsFocused))
			{
				ParentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;
			}
		}

		private void HandleMouseMove(object sender, MouseEventArgs e)
		{
			CbSectionLineDragOperation op;
			bool newPositionIsOk;

			var pos = e.GetPosition(relativeTo: _canvas);
			e.Handled = true;

			var amount = pos.X - _originalSectionLinePosition;

			if (DragState == DragState.Begun)
			{
				op = CbSectionLineDragOperation.Started;
				if (Math.Abs(amount) > MIN_SEL_DISTANCE)
				{
					newPositionIsOk = IsNewPositionOk(amount);
					if (newPositionIsOk)
					{
						DragState = DragState.InProcess;
					}
				}
				else
				{
					return;
				}
			}
			else
			{
				op = CbSectionLineDragOperation.Move;
				newPositionIsOk = IsNewPositionOk(amount);
			}

			if (newPositionIsOk)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbSectionLine. The position is ok upon MouseMove. The XPos is {pos.X}. The original position is {_originalSectionLinePosition}.");
				SectionLinePositionX = pos.X;

				_sectionLineMovedCallback(new CbSectionLineMovedEventArgs(ColorBandIndex, pos.X, _updatingPrevious, op));
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbSectionLine. UpdateColorBandWidth returned false. The XPos is {pos.X}. The original position is {_originalSectionLinePosition}.");
			}
		}

		private void HandlePreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (DragState == DragState.None)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbSectionLine. HandleMouseLeftButtonUp. Not Handling the DragState is {DragState}.");
				return;
			}

			var ht = VisualTreeHelper.HitTest(_canvas, Mouse.GetPosition(_canvas));
			var mouseWasOverCanvas = ht != null;

			if (!mouseWasOverCanvas)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbSectionLine is getting a MouseLeftButtonUp event. The mouse is not over the canvas, cancelling.");
				CancelDragInternal();
				return;
			}

			if (Keyboard.IsKeyDown(Key.Escape))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbSectionLine is getting a MouseLeftButtonUp event. The Escape Key is Pressed, cancelling.");
				CancelDragInternal();

				e.Handled = true;
				return;
			}

			var dragCompleted = CompleteDrag();

			if (dragCompleted)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbSectionLine is getting a MouseLeftButtonUp event. The Drag is Complete.");
				e.Handled = true;
			}
		}

		private void Handle_TopArrowMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				NotifySelectionChanged();
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_colorBandLayoutViewModel.RequestContextMenuShown(ColorBandIndex, ColorBandSetEditMode.Cutoffs);
					e.Handled = true;
				}
			}
		}

		private void Canvas_LostMouseCapture(object sender, MouseEventArgs e)
		{
			Debug.WriteLine($"WARNING: CbSectionLine Lost the MouseCapture. Source: {e.Source}. Sender: {sender}. Original Source: {e.MouseDevice}");
			CancelDrag();
		}

		#endregion

		#region Private Methods

		private void NotifySelectionChanged()
		{
			_colorBandLayoutViewModel.IsSelectedChangedCallback(ColorBandIndex, ColorBandSetEditMode.Cutoffs);
		}

		private bool CompleteDrag()
		{
			var distance = Math.Abs(SectionLinePositionX - _originalSectionLinePosition);

			if (distance > MIN_SEL_DISTANCE)
			{
				_sectionLineMovedCallback(new CbSectionLineMovedEventArgs(ColorBandIndex, SectionLinePositionX, _updatingPrevious, CbSectionLineDragOperation.Complete));

				DragState = DragState.None;
				return true;
			}
			else
			{
				CancelDragInternal();
				return false;
			}
		}

		private void CancelDragInternal()
		{
			if (SectionLinePositionX != _originalSectionLinePosition)
			{
				SectionLinePositionX = _originalSectionLinePosition;
			}

			if (DragState == DragState.InProcess)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbSectionLine. CancelDragInternal. Drag was InProcess, raising SectionLineMoved with Operation = Cancel.");
				_sectionLineMovedCallback(new CbSectionLineMovedEventArgs(ColorBandIndex, _originalSectionLinePosition, _updatingPrevious, CbSectionLineDragOperation.Cancel));
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbSectionLine. CancelDragInternal. Drag was only Begun, raising SectionLineMoved with Operation = NotStarted.");
				_sectionLineMovedCallback(new CbSectionLineMovedEventArgs(ColorBandIndex, _originalSectionLinePosition, _updatingPrevious, CbSectionLineDragOperation.NotStarted));
			}

			DragState = DragState.None;
		}

		private bool IsNewPositionOk(double amount)
		{
			if ( !(_leftWidth.HasValue && _rightWidth.HasValue)  )
			{
				throw new InvalidOperationException("The LeftWidth or RightWidth is null.");
			}

			bool result;

			if (amount < 0)
			{
				amount = amount * -1;
				result = _leftWidth > amount + (1 * ContentScale.Width);
			}
			else
			{
				result = _rightWidth > amount + (1 * ContentScale.Width);
			}

			return result;
		}

		#endregion

		#region Private Methods - Layout

		private Line BuildDragLine(Rect sectionLineArea, bool isUnderMounse, bool parentIsFocused, SizeDbl contentScale)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent,
				Stroke = GetDragLineStroke(isUnderMounse, parentIsFocused),
				StrokeThickness = 1.0,
				Y1 = sectionLineArea.Y,
				Y2 = sectionLineArea.Bottom,
				X1 = sectionLineArea.Right * contentScale.Width,
				X2 = sectionLineArea.Right * contentScale.Width
			};

			return result;
		}

		private Polygon BuildTopArrow(Rect topArrowArea, bool isSelected, bool isUnderMouse, bool parentIsFocused, SizeDbl contentScale)
		{
			var result = new Polygon()
			{
				Fill = GetTopArrowFill(isSelected, parentIsFocused),
				Stroke = GetTopArrowStroke(isUnderMouse, parentIsFocused),
				StrokeThickness = STROKE_THICKNESS,
				Points = BuildTopAreaPoints(topArrowArea.Right * contentScale.Width, topArrowArea.Y, topArrowArea.Height)
			};

			return result;
		}

		private PointCollection BuildTopAreaPoints(double sectionLinePosition, double y0, double height)
		{
			var points = new PointCollection()
			{
				new Point(sectionLinePosition, y0 + height),				// Bottom
				new Point(sectionLinePosition - _topArrowHalfWidth, y0),	// Top, left
				new Point(sectionLinePosition + _topArrowHalfWidth, y0),	// Top, right
			};

			return points;
		}

		#endregion

		#region Private Methods - IsCurrent / IsSelected State

		private Brush GetDragLineStroke(bool isUnderMouse, bool parentIsFocused)
		{
			var result = DEFAULT_STROKE; // parentIsFocused && isUnderMouse ? IS_HOVERED_STROKE : DEFAULT_STROKE;
			return result;
		}

		private Brush GetTopArrowStroke(bool isUnderMouse, bool parentIsFocused)
		{
			var result = parentIsFocused && isUnderMouse ? IS_HOVERED_STROKE : DEFAULT_STROKE;
			return result;
		}

		private Brush GetTopArrowFill(bool isSelected, bool parentIsFocused)
		{
			var result = isSelected 
				? parentIsFocused
					? IS_SELECTED_BACKGROUND
					: IS_SELECTED_INACTIVE_BACKGROUND
				: DEFAULT_BACKGROUND;

			return result;
		}

		#endregion

		#region Unused

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

		#endregion
	}
}
