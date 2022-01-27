using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace MSetExplorer.ScreenHelpers
{
    internal class Win32
    {
        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref WPOINT point);

        [StructLayout(LayoutKind.Sequential)]
        public struct WPOINT
        {
            public int x;
            public int y;

            public WPOINT(int X, int Y)
            {
                x = X;
                y = Y;
            }
        }

        public static Point PositionCursor(IntPtr hWnd, Point pos)
		{
            var p = GetScreenPoint(hWnd, pos);
            _ = SetCursorPos(p.x, p.y);
            var result = new Point(p.x, p.y);
            return result;
        }

        public static Point TranslateToScreen(IntPtr hWnd, Point pos)
        {
            var p = GetScreenPoint(hWnd, pos);
            var result = new Point(p.x, p.y);
            return result;
        }

        public static WPOINT GetScreenPoint(IntPtr hWnd, Point pos)
		{
            var p = new WPOINT((int)pos.X, (int)pos.Y);
            _ = ClientToScreen(hWnd, ref p);
            return p;
        }

    }
}
