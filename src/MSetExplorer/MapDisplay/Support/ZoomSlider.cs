using System;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	public class ZoomSlider : IDisposable
	{
		#region Private Fields

		private readonly ScrollBar _scrollbar;
		private readonly IZoomInfo _zoomedControl;

		private bool _disableScrollValueSync = false;   // If true, do update the _zoomedControl's Scale property.

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public ZoomSlider(ScrollBar scrollBar, IZoomInfo zoomedControl)
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
			if (_zoomedControl.CanZoom && !_disableScrollValueSync)
			{
				//Debug.WriteLineIf(_useDetailedDebug, "\n========== The user is setting the scale.");

				Debug.WriteLineIf(_useDetailedDebug, $"\n========== The ZoomSlider is updating the PanAndZoomControl's ScrollBar Value: {_scrollbar.Value}.");
				_zoomedControl.Scale = _scrollbar.Value;
			}
		}

		#endregion

		#region Public Methods

		public void ContentScaleWasUpdated(double contentScale)
		{
			if (double.IsNaN(_scrollbar.Value) || _scrollbar.Value != contentScale)
			{
				Debug.Assert(contentScale >= _scrollbar.Minimum && contentScale <= _scrollbar.Maximum, $"ContentScaleWasUpdated was called with value: {contentScale} which is not within the range: {_scrollbar.Minimum} and {_scrollbar.Maximum}.");

				_disableScrollValueSync = true;
				try
				{
					_scrollbar.Value = contentScale;
				}
				finally
				{
					_disableScrollValueSync = false;
				}
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
					Debug.WriteLineIf(_useDetailedDebug, "The ZoomSlider is handling the InvalidateScaleContentInfo.");
					_scrollbar.Value = _scrollbar.Maximum;

					_scrollbar.Minimum = _zoomedControl.MinScale;
					_scrollbar.Maximum = _zoomedControl.MaxScale;

					//var recip = 1 / _scrollbar.Minimum;
					//var in10Parts = recip / 20;
					//var inverse10Parts = 1 / in10Parts;

					_scrollbar.SmallChange = 0.005; // _zoomedControl.MinScale / 2;
					_scrollbar.LargeChange = 0.20; // _zoomedControl.MinScale * 2; // _scrollbar.Minimum * 8;

					_scrollbar.Value = _zoomedControl.Scale;
				}
				finally
				{
					_disableScrollValueSync = false;
				}
			}
		}

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
