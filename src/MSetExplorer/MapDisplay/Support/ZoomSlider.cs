using System;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	public class ZoomSlider : IDisposable
	{
		private readonly ScrollBar _scrollbar;
		private readonly IContentScaleInfo _zoomedControl;

		private bool _disableScrollValueSync = false;

		public ZoomSlider(ScrollBar scrollBar, IContentScaleInfo zoomedControl)
		{
			_scrollbar = scrollBar;
			_zoomedControl = zoomedControl;

			_scrollbar.ValueChanged += _scrollbar_ValueChanged;
		}

		private void _scrollbar_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
		{
			if (!_disableScrollValueSync)
			{
				if (_zoomedControl.CanZoom)
				{
					_zoomedControl.SetScale(_scrollbar.Value);
				}
			}
		}

		public void ContentScaleWasUpdated(double scale)
		{
			Debug.Assert(scale > _scrollbar.Minimum && scale < _scrollbar.Maximum, $"ContentScaleWasUpdated was called with value: {scale}, but it is not withing the range: {_scrollbar.Minimum} and {_scrollbar.Maximum}.");
			_scrollbar.Value = scale;
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
					_scrollbar.Value = 0;
					_scrollbar.Maximum = _zoomedControl.MaxScale;
					_scrollbar.Minimum = _zoomedControl.MinScale;

					_scrollbar.SmallChange = 2;
					_scrollbar.LargeChange = 4;
				}
				finally
				{
					_disableScrollValueSync = false;
					_scrollbar.Value = _zoomedControl.Scale;
				}
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
