using System;
using System.Diagnostics;
using System.IO;
using MSS.Common;
using System.Runtime.CompilerServices;
using PngImageLib;

namespace ImageBuilder
{
    public sealed class PngImage : IDisposable
    {
		private const double VALUE_FACTOR = 10000;

		private readonly Stream OutputStream;
        private readonly ImageInfo imi;
        private readonly PngWriter png;
        private int curRow;
        private bool weOwnTheStream;

        public string Path { get; }
        public ImageLine ImageLine { get; }

        public PngImage(string path, int width, int height) : this(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read), path, width, height)
        {
            weOwnTheStream = true;
        }

        public PngImage(Stream outputStream, string path, int width, int height)
		{
            OutputStream = outputStream;
            Path = path;
            imi = new ImageInfo(width, height, 8, false); // 8 bits per channel, no alpha 
            png = new PngWriter(OutputStream, imi, path);
            ImageLine = new ImageLine(imi);
            curRow = 0;
            weOwnTheStream = false;
        }

        public void WriteLine(ImageLine iline)
        {
            png.WriteRow(iline, curRow++);
        }

        public void End()
		{
            png.End();
		}

        public void Abort()
        {
            png.Abort();
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void FillPngImageLineSegment(ImageLine iLine, int pixPtr, ushort[]? counts, ushort[]? escapeVelocities, int lineLength, int samplesToSkip, ColorMap colorMap)
		{
			if (counts == null || escapeVelocities == null)
			{
				FillPngImageLineSegmentWithWhite(iLine, pixPtr, lineLength);
				return;
			}

			var cComps = new byte[4];
			var dest = new Span<byte>(cComps);

			var previousCountVal = counts[0];

			for (var xPtr = 0; xPtr < lineLength; xPtr++)
			{
				var countVal = counts[xPtr + samplesToSkip];

				if (countVal != previousCountVal)
				{
					//NumberOfCountValSwitches++;
					previousCountVal = countVal;
				}

				var escapeVelocity = colorMap.UseEscapeVelocities ? escapeVelocities[xPtr + samplesToSkip] / VALUE_FACTOR : 0;

				if (escapeVelocity > 1.0)
				{
					Debug.WriteLine($"The Escape Velocity is greater that 1.0");
				}

				colorMap.PlaceColor(countVal, escapeVelocity, dest);

				ImageLineHelper.SetPixel(iLine, pixPtr++, cComps[2], cComps[1], cComps[0]);
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FillPngImageLineSegmentWithWhite(ImageLine iLine, int pixPtr, int len)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

		#region IDisposable Support

		private bool disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && weOwnTheStream)
                {
                    png.End();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
