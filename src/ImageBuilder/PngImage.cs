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

        //public void WriteLine(int[] pixelData)
        //{
        //    var iLine = new ImageLine(imi);
        //    for(var ptr = 0; ptr < pixelData.Length; ptr++)
        //    {
        //        ImageLineHelper.SetPixelFromARGB8(iLine, ptr, pixelData[ptr]);
        //    }

        //    png.WriteRow(iLine, curRow++);
        //}

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
