using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MExplorer
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		//private void myButton_Click(object sender, RoutedEventArgs e)
		//{
		//	myButton.Content = "Clicked";
		//}

		void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
		{
			//args.DrawingSession.DrawEllipse(155, 115, 80, 30, Colors.Black, 3);
			//args.DrawingSession.DrawText("Hello, world!", 100, 100, Colors.Yellow);

			CanvasBitmap canvasBitmap = GetCanvasBitmap(args.DrawingSession);
			Rect dest = new(0, 0, 100, 100);

			args.DrawingSession.DrawImage(canvasBitmap, dest);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}


		private CanvasBitmap GetCanvasBitmap(CanvasDrawingSession drawingSession)
		{
			byte[] buf = GetImageBytes();
			CanvasBitmap result = CanvasBitmap.CreateFromBytes(drawingSession, buf, 100, 100, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);

			return result;
		}

		private byte[] GetImageBytes()
		{
			byte[] buf = new byte[32 * 100 * 100];

			for(int i = 0; i < 100; i++)
			{
				for(int j = 0; j < 100; j++)
				{
					int index = 4 * (i * 100 + j);
					buf[index] = 200;
					buf[index + 1] = 10; // (byte)((byte)i + j);
					buf[index + 2] = 45;
					buf[index + 3] = 1;
				}
			}

			return buf;
		}
	}
}
