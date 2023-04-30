using System;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	public class ZoomSlider : IDisposable
	{
		private readonly ScrollBar _scrollbar;
		private readonly IContentScaleInfo _zoomedControl;

		public ZoomSlider(ScrollBar scrollBar, IContentScaleInfo zoomedControl)
		{
			_scrollbar = scrollBar;
			_zoomedControl = zoomedControl;

			_scrollbar.ValueChanged += _scrollbar_ValueChanged;
		}

		private void _scrollbar_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
		{
			if (_zoomedControl.CanZoom)
			{
				_zoomedControl.SetContentScale(_scrollbar.Value);
			}
		}

		public void ContentScaleWasUpdated(double scale)
		{
			// TODO: Validate that Min < scale and scale < Max
			_scrollbar.Value = scale;
		}

		public void InvalidateScaleContentInfo()
		{
			if (_zoomedControl.CanZoom)
			{
				// TODO: Set the scrollBar Max, Min and value in the correct order.
				_scrollbar.Maximum = _zoomedControl.MaxContentScale;
				_scrollbar.Minimum = _zoomedControl.MinContentScale;
				_scrollbar.Value = _zoomedControl.ContentScale;

				_scrollbar.SmallChange = 2;
				_scrollbar.LargeChange = 4;

			}
		}

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
