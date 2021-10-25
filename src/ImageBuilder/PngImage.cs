using System;
using System.IO;
using PngImageLib;

namespace ImageBuilder
{
    public sealed class PngImage : IDisposable
    {
        private readonly Stream OutputStream;
        private readonly ImageInfo imi;
        private readonly PngWriter png;
        private int curRow;

        public readonly string Path;
        public readonly ImageLine ImageLine;

        public PngImage(string path, int width, int height)
        {
            Path = path;
            OutputStream = File.Open(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

            imi = new ImageInfo(width, height, 8, false); // 8 bits per channel, no alpha 
            png = new PngWriter(OutputStream, imi, path);
            ImageLine = new ImageLine(imi);
            curRow = 0;
        }

        public void WriteLine(int[] pixelData)
        {
            ImageLine iLine = new ImageLine(imi);
            for(int ptr = 0; ptr < pixelData.Length; ptr++)
            {
                ImageLineHelper.SetPixelFromARGB8(iLine, ptr, pixelData[ptr]);
            }

            png.WriteRow(iLine, curRow++);
        }

        public void WriteLine(ImageLine iline)
        {
            png.WriteRow(iline, curRow++);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
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
