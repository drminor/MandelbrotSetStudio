using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer.MapDisplay.ExpermentalOrUnused
{
    public class MySimplePanel : Panel
    {
        // Make the panel as big as the biggest element
        protected override Size MeasureOverride(Size availableSize)
        {
            var maxSize = new Size();

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(availableSize);
                maxSize.Height = Math.Max(child.DesiredSize.Height, maxSize.Height);
                maxSize.Width = Math.Max(child.DesiredSize.Width, maxSize.Width);
            }

            return availableSize;
        }

        // Arrange the child elements to their final position
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //content = this.Template.FindName("PART_Content", this) as FrameworkElement;
            //if (content != null)
            //{
            //	//
            //	// Setup the transform on the content so that we can scale it by 'ContentScale'.
            //	//
            //	this.contentScaleTransform = new ScaleTransform(this.ContentScale, this.ContentScale);

            //	//
            //	// Setup the transform on the content so that we can translate it by 'ContentOffsetX' and 'ContentOffsetY'.
            //	//
            //	this.contentOffsetTransform = new TranslateTransform();
            //	UpdateTranslationX();
            //	UpdateTranslationY();

            //	//
            //	// Setup a transform group to contain the translation and scale transforms, and then
            //	// assign this to the content's 'RenderTransform'.
            //	//
            //	TransformGroup transformGroup = new TransformGroup();
            //	transformGroup.Children.Add(this.contentOffsetTransform);
            //	transformGroup.Children.Add(this.contentScaleTransform);
            //	content.RenderTransform = transformGroup;
            //}
        }
    }
}
