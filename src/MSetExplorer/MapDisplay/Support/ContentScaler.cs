using MSS.Types;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class ContentScaler : IContentScaler
	{
		private readonly ContentPresenter _contentPresenter;

		private SizeDbl _contentViewPortSize;

		public ContentScaler(ContentPresenter contentPresenter)
		{
			_contentPresenter = contentPresenter;

			TranslateTransform = new TranslateTransform();
			ScaleTransform = new ScaleTransform();

			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(TranslateTransform);
			transformGroup.Children.Add(ScaleTransform);

			_contentPresenter.RenderTransform = transformGroup;
		}

		// Although we are implementing the interface, we are not actually doing anything.
		// The Content is sized via the standard calls to Arrange on the content
		// as the PanAndZoom control is executing its ArrangeOverride method.
		public SizeDbl ContentViewportSize
		{
			get => _contentViewPortSize;
			set => _contentViewPortSize = value;
		}

		public TranslateTransform TranslateTransform { get; init; }

		public ScaleTransform ScaleTransform { get; init; }
	}
}
