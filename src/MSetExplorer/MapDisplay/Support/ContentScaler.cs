using MSS.Types;
using System;
using System.Windows;
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

			ScaleTransform = new ScaleTransform();

			TranslateTransform = new TranslateTransform();

			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(TranslateTransform);
			transformGroup.Children.Add(ScaleTransform);

			_contentPresenter.RenderTransform = transformGroup;
		}

		public ScaleTransform ScaleTransform { get; init; }
		
		public SizeDbl ContentViewportSize
		{
			get => _contentViewPortSize;
			set => _contentViewPortSize = value;
		}

		public TranslateTransform TranslateTransform { get; init; }

	}
}
