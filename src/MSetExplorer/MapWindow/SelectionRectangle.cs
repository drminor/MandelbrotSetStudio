using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer.MapWindow
{
	internal class SelectionRectangle
	{
		private readonly Canvas _canvas;
		private readonly SizeInt _defaultSize;
		private readonly Rectangle _selectedArea;

		private bool _isActive;

		#region Constructor

		public SelectionRectangle(Canvas canvas, SizeInt defaultSize)
		{
			_canvas = canvas;
			_defaultSize = defaultSize;

			_selectedArea = new Rectangle()
			{
				Width = _defaultSize.Width,
				Height = _defaultSize.Height,
				Fill = Brushes.Transparent,
				Stroke = BuildDrawingBrush(),
				StrokeThickness = 1,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_isActive = false;

			_ = _canvas.Children.Add(_selectedArea);
			_selectedArea.SetValue(Panel.ZIndexProperty, 10);

			Move(new Point(0, 0));

			_selectedArea.KeyUp += SelectedArea_KeyUp;
			canvas.MouseWheel += Canvas_MouseWheel;
			canvas.MouseMove += Canvas_MouseMove;

			canvas.MouseEnter += Canvas_MouseEnter;
			canvas.MouseLeave += Canvas_MouseLeave;

			// Just for Diagnostics
			_selectedArea.MouseWheel += SelectedArea_MouseWheel;
			_selectedArea.MouseLeftButtonDown += SelectedArea_MouseLeftButtonDown;
		}

		#endregion

		#region Public Properties


		public bool IsActive
		{
			get => _isActive;
			set
			{
				if (_isActive != value)
				{
					if (value)
					{
						_selectedArea.Visibility = Visibility.Visible;
					}
					else
					{
						_selectedArea.Visibility = Visibility.Hidden;
						_selectedArea.Width = _defaultSize.Width;
						_selectedArea.Height = _defaultSize.Height;
					}

					_isActive = value;
				}
			}
		}

		#endregion

		#region Public Methods

		public void Activate(Point position)
		{
			IsActive = true;
			Move(position);
			if (!_selectedArea.Focus())
			{
				Debug.WriteLine("Activate did not move the focus to the SelectedRectangle, free form.");
			}
		}

		public bool Contains(Point position)
		{
			var rp = new Point((double)_selectedArea.GetValue(Canvas.LeftProperty), (double)_selectedArea.GetValue(Canvas.BottomProperty));
			var rs = new Size(_selectedArea.Width, _selectedArea.Height);
			var r = new Rect(rp, rs);

			var result = r.Contains(position);

			//var strResult = result ? "is contained" : "is not contained";
			//Debug.WriteLine($"Checking {p} to see if it contained by {r} and it {strResult}.");

			return result;
		}

		#endregion

		#region Event Handlers

		private void Canvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			//Debug.WriteLine("The canvas received a MouseWheel event.");

			var cPos = GetPosition();
			var cSize = GetSize();

			Point newPos;
			Size newSize;

			if (e.Delta > 0)
			{
				newPos = new Point(cPos.X - 2, cPos.Y - 2);
				newSize = new Size(cSize.Width + 4, cSize.Height + 4);
			}
			else if (e.Delta < 0 && cSize.Width > 8 && cSize.Height > 8)
			{
				newPos = new Point(cPos.X + 2, cPos.Y + 2);
				newSize = new Size(cSize.Width - 4, cSize.Height - 4);
			}
			else
			{
				Debug.WriteLine("MouseWheel, but no change.");
				return;
			}

			Move(newPos, newSize);

			e.Handled = true;
		}

		private void SelectedArea_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (!IsActive)
			{
				Debug.WriteLine($"The {e.Key} was pressed, but we are not active, returning.");
				return;
			}

			Debug.WriteLine($"The {e.Key} was pressed.");

			if (e.Key == System.Windows.Input.Key.Escape)
			{
				IsActive = false;
			}
		}

		private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (_isActive)
			{
				// Get position of mouse relative to the main canvas and invert the y coordinate.
				var position = e.GetPosition(relativeTo: _canvas);
				position = new Point(position.X, _canvas.ActualHeight - position.Y);

				Move(position);
			}
		}

		private void Canvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (_isActive)
			{
				_selectedArea.Visibility = Visibility.Hidden;
			}
		}

		private void Canvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (_isActive)
			{
				_selectedArea.Visibility = Visibility.Visible;

				if (!_selectedArea.Focus())
				{
					Debug.WriteLine("Canvas Enter did not move the focus to the SelectedRectangle.");
				}
			}
		}

		// Just for Diagnostics

		private void SelectedArea_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var position = e.GetPosition(relativeTo: _canvas);

			Debug.WriteLine($"The SelectionRectangle is getting a Mouse Left Button Down at {position}.");
		}

		private void SelectedArea_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			Debug.WriteLine("The SelectionRectangle received a MouseWheel event.");
		}

		#endregion

		#region Private Methods

		private void Move(Point position)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, free form.");

			var x = position.X - _selectedArea.Width / 2;
			if (x < 0)
			{
				x = 0;
			}

			if (x + _selectedArea.Width > _canvas.ActualWidth)
			{
				x = _canvas.ActualWidth - _selectedArea.Width;
			}

			var cLeft = (double)_selectedArea.GetValue(Canvas.LeftProperty);
			if (double.IsNaN(cLeft) || Math.Abs(x - cLeft) > 0.01)
			{
				_selectedArea.SetValue(Canvas.LeftProperty, x);
			}

			var y = position.Y - (_selectedArea.Height / 2);
			if (y < 0)
			{
				y = 0;
			}

			if (y + _selectedArea.Height > _canvas.ActualHeight)
			{
				y = _canvas.ActualHeight - _selectedArea.Height;
			}

			var cBot = (double)_selectedArea.GetValue(Canvas.BottomProperty);

			if (double.IsNaN(cBot) || Math.Abs(y - cBot) > 0.01)
			{
				_selectedArea.SetValue(Canvas.BottomProperty, y);
			}
		}

		private void Move(Point position, Size size)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, with size: {size}");

			if (position.X < 0
				|| position.Y < 0
				|| position.X + size.Width > _canvas.ActualWidth
				|| position.Y + size.Height > _canvas.ActualHeight)
			{
				return;
			}

			var cPos = GetPosition();
			if (position.X != cPos.X)
			{
				_selectedArea.SetValue(Canvas.LeftProperty, (double)position.X);
			}

			if (position.Y != cPos.Y)
			{
				_selectedArea.SetValue(Canvas.BottomProperty, (double)position.Y);
			}

			var cSize = GetSize();
			if (Math.Abs(size.Width - cSize.Width) > 0.01)
			{
				_selectedArea.Width = size.Width;
			}

			if (Math.Abs(size.Height - cSize.Height) > 0.01)
			{
				_selectedArea.Height = size.Height;
			}
		}

		private Point GetPosition()
		{
			return new Point((double)_selectedArea.GetValue(Canvas.LeftProperty), (double)_selectedArea.GetValue(Canvas.BottomProperty));
		}

		private Size GetSize()
		{
			return new Size(_selectedArea.Width, _selectedArea.Height);
		}

		#endregion

		#region Drawing Support

		private DrawingBrush BuildDrawingBrush()
		{
			var aDrawingGroup = new DrawingGroup();

			var inc = 16;
			var x = 0;
			var y = 0;

			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White));

			x = 0;
			y += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black));

			x = 0;
			y += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White));

			x = 0;
			y += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.White, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(new Rect(x, y, inc, inc), Brushes.Black, Brushes.Black));


			var result = new DrawingBrush(aDrawingGroup)
			{
				TileMode = TileMode.Tile,
				//Stretch = Stretch.None,
				ViewportUnits = BrushMappingMode.Absolute,
				Viewport = new Rect(0, 0, inc, inc)
				
			};

			return result;
		}

		private GeometryDrawing BuildDot(Rect rect, SolidColorBrush fill, SolidColorBrush outline)
		{
			var result = new GeometryDrawing(
				fill,
				new Pen(outline, 1),
				new RectangleGeometry(rect)
			);

			return result;
		}

		#endregion
	}
}
