using System;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	public class ZoomSlider : IDisposable
	{
		#region Private Fields

		private const double BREAK_DOWN_FACTOR = 0.5;

		private readonly ScrollBar _scrollbar;
		private readonly IContentScaleInfo _zoomedControl;

		private bool _disableScrollValueSync = false;

		#endregion

		#region Constructor

		public ZoomSlider(ScrollBar scrollBar, IContentScaleInfo zoomedControl)
		{
			_scrollbar = scrollBar;
			_zoomedControl = zoomedControl;
			InvalidateScaleContentInfo();

			_scrollbar.ValueChanged += _scrollbar_ValueChanged;
		}

		#endregion

		#region Event Handlers

		private void _scrollbar_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
		{
			if (!_disableScrollValueSync)
			{
				if (_zoomedControl.CanZoom)
				{
					(BaseValue, RelativeValue) = GetBaseAndRelative(_scrollbar.Value);

					_zoomedControl.SetScale(RelativeValue);
				}
			}
		}

		#endregion

		#region Public Properties

		public double BaseValue { get; set; }
		public double RelativeValue { get; set; }

		#endregion

		#region Public Methods

		public void ContentScaleWasUpdated(double contentScale)
		{
			if (double.IsNaN(_scrollbar.Value) || RelativeValue != contentScale)
			{
				RelativeValue = contentScale;
				var combinedValue = GetCombinedValue(BaseValue, RelativeValue);
				Debug.Assert(combinedValue >= _scrollbar.Minimum && combinedValue <= _scrollbar.Maximum, $"ContentScaleWasUpdated was called with value: {contentScale}, producing combinedValue: {combinedValue}, but it is not withing the range: {_scrollbar.Minimum} and {_scrollbar.Maximum}.");

				_scrollbar.Value = combinedValue;
			}
		}

		public void InvalidateScaleContentInfo()
		{
			if (_disableScrollValueSync)
			{
				return;
			}

			if (_zoomedControl.CanZoom)
			{
				_disableScrollValueSync = true;

				try
				{
					//_scrollbar.Value = 1;
					_scrollbar.Maximum = _zoomedControl.MaxScale;
					_scrollbar.Minimum = _zoomedControl.MinScale;

					_scrollbar.SmallChange = _scrollbar.Minimum;
					_scrollbar.LargeChange = _scrollbar.Minimum * 2;

					RelativeValue = _zoomedControl.Scale;
					var combinedValue = GetCombinedValue(BaseValue, RelativeValue);
					Debug.Assert(combinedValue >= _scrollbar.Minimum && combinedValue <= _scrollbar.Maximum, $"ContentScaleWasUpdated was called with value: {RelativeValue}, producing combinedValue: {combinedValue}, but it is not withing the range: {_scrollbar.Minimum} and {_scrollbar.Maximum}.");

					_scrollbar.Value = combinedValue;
				}
				finally
				{
					_disableScrollValueSync = false;
				}
			}
		}

		#endregion

		#region Private Methods

		private (double baseValue, double relativeValue) GetBaseAndRelative(double value)
		{

			//var t = value / BREAK_DOWN_FACTOR;

			//var b = (double)(int)t;
			//b = b *= BREAK_DOWN_FACTOR;

			//var r = t - b;

			//r = r *= BREAK_DOWN_FACTOR;

			//return (b, r);


			double b;
			double r;

			if (value > 0.5)
			{
				b = 0;
				r = value;
			}
			else if (value == 0.5)
			{
				b = 1;
				r = 1;
			}
			else if (value == 0.4375)
			{
				b = 1;
				r = 0.875;
			}
			else if (value == 0.375)
			{
				b = 1;
				r = 0.75;
			}
			else if (value == 0.3125)
			{
				b = 1;
				r = 0.625;
			}
			else
			{
				b = 2;
				r = 1;
			}

			return (b, r);
		}

		private double GetCombinedValue(double b, double r)
		{
			var combinedValue = Math.Pow(BREAK_DOWN_FACTOR, b) * r;
			return combinedValue;
		}


		/*
		
				combined	base	relative
		1		1			0		1			1 * 1
		15/16	0.9375		0		0.9375		1 * 0.9375
		14/16	0.875		0		0.875		1 * 0.875
		8/16	0.5			1		1			0.5 * 1.0				= 0.5			1/2
		7/16	0.4375		1		0.875		0.5 * 0.875				= 0.5 x ((2 * 7) / 16) 14/16 = 0.875
		6/16	0.375		1		0.75		0.5 * 3/4				= 1.5/4 = 3/8 = 6/16
		5/16	0.3125		1		0.625		0.5 * 5/8
		4/16	0.25		2		1			0.5 * 0.5 * 1			= 0.25 * 1 = 0.25			
		3/16	0.1875		2		0.5			0.5 * 0.5 * 12/16		= 0.25 x 12/16 = (0.25 * 12) / 16 = 3/16
		2/16	0.125		3		1			0.5 * 0.5 * 0.5 x 1
		1/16				4		1			0.5 x 0.5 * 0.5 * 0.5	8/16 ^4 = 8^4 / 16^4 = 4096 / 65536 = 1/16 = 0.0625

		
		*/

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_scrollbar.ValueChanged -= _scrollbar_ValueChanged;
				}

				disposedValue = true;
			}
		}
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
