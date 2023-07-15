using MSS.Types;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Default implementation
	/// </summary>
	internal class ContentScaler : IContentScaler
	{
		private readonly ContentPresenter _contentPresenter;

		private SizeDbl _contentViewPortSize;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		public ContentScaler(ContentPresenter contentPresenter)
		{
			_contentPresenter = contentPresenter;

			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			//_canvas.RenderTransform = _canvasRenderTransform;
		}

		// Although we are implementing the interface, we are not actually doing anything.
		// The Content is sized via the standard calls to Arrange on the content as the PanAndZoom control
		// executes its ArrangeOverride method.
		public SizeDbl ContentViewportSize
		{
			get => _contentViewPortSize;
			set => _contentViewPortSize = value;
		}

		public SizeDbl ContentScale { get; set; }

		public RectangleDbl? ScaledContentArea { get; set; }

	}
}
