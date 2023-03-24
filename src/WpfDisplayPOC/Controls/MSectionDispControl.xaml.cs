using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WpfDisplayPOC
{
    /// <summary>
    /// Interaction logic for MSectionDispControl.xaml
    /// </summary>
    public partial class MSectionDispControl : UserControl
    {
        private bool _isLoaded;

        public MSectionDispControl()
        {
            _isLoaded = false;

            Loaded += MSectionDispControl_Loaded;
            Initialized += MSectionDispControl_Initialized;
            SizeChanged += MSectionDispControl_SizeChanged;

			InitializeComponent();
		}

		public void RenderStars(Field.Star[] stars)
        {
            if (_isLoaded)
            {
                BitmapGrid1.RenderStars(stars);
            }
        }

		private void MSectionDispControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isLoaded)
            {
                BitmapGrid1.ClearBitmap();
                //BitmapGrid1.InvalidateVisual();
            }
		}

		protected override void ParentLayoutInvalidated(UIElement child)
        {
            base.ParentLayoutInvalidated(child);
			BitmapGrid1.InvalidateVisual();
		}

        private void MSectionDispControl_Initialized(object? sender, System.EventArgs e)
        {
			Debug.WriteLine("The MSectionDispControl is initialized.");
		}

		private void MSectionDispControl_Loaded(object sender, RoutedEventArgs e)
        {
            BitmapGrid1.ClearBitmap();
            BitmapGrid1.InvalidateVisual();

			_isLoaded = true;
		}
	}
}
