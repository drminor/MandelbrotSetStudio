using PngImageLib;
using System;
using System.IO;

namespace Ar.Com.Hjg.Pngcs
{
    class BufferedStreamFeeder
    {
        private Stream _stream;
        private byte[] buf;
        private int pendinglen; // bytes read and stored in buf that have not yet still been fed to IBytesConsumer
        private int offset;
        private bool eof = false;
        private bool closeStream = true;
        private bool failIfNoFeed = false;
        private const int DEFAULTSIZE = 8192;

       	public BufferedStreamFeeder(Stream ist) : this(ist,DEFAULTSIZE) {
	    }

    	public BufferedStreamFeeder(Stream ist, int bufsize) {
	    	this._stream = ist;
	    	buf = new byte[bufsize];
	    }

        /// <summary>
        /// Stream from which bytes are read
        /// </summary>
        public Stream GetStream()
        {
            return _stream;
        }
        /// <summary>
        /// Feeds bytes to the consumer 
        ///  Returns bytes actually consumed
        ///  This should return 0 only if the stream is EOF or the consumer is done
        /// </summary>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public int Feed(IBytesConsumer consumer)
        {
            return Feed(consumer, -1);
        }

        public int Feed(IBytesConsumer consumer, int maxbytes)
        {
            int n = 0;
            if (pendinglen == 0)
            {
                RefillBuffer();
            }
            int tofeed = maxbytes > 0 && maxbytes < pendinglen ? maxbytes : pendinglen;
            if (tofeed > 0)
            {
                n = consumer.Consume(buf, offset, tofeed);
                if (n > 0)
                {
                    offset += n;
                    pendinglen -= n;
                }
            }
            if (n < 1 && failIfNoFeed)
                throw new PngjInputException("failed feed bytes");
            return n;
        }

        public bool FeedFixed(IBytesConsumer consumer, int nbytes)
        {
            int remain = nbytes;
            while (remain > 0)
            {
                int n = Feed(consumer, remain);
                if (n < 1)
                    return false;
                remain -= n;
            }
            return true;
        }

        protected void RefillBuffer()
        {
            if (pendinglen > 0 || eof)
                return; // only if not pending data
            try
            {
                // try to read
                offset = 0;
                pendinglen = _stream.Read(buf,0,buf.Length);
                if (pendinglen < 0)
                {
                    close();
                    return;
                }
                else
                    return;
            }
            catch (IOException e)
            {
                throw new PngjInputException(e);
            }
        }

        public bool HasMoreToFeed()
        {
            if (eof)
                return pendinglen > 0;
            else
                RefillBuffer();
            return pendinglen > 0;
        }

        public void SetCloseStream(bool closeStream)
        {
            this.closeStream = closeStream;
        }

        public void close()
        {
            eof = true;
            buf = null;
            pendinglen = 0;
            offset = 0;
            try
            {
                if (_stream != null && closeStream)
                    _stream.Close();
            }
            catch (Exception e)
            {
                PngHelperInternal.Log("Exception closing stream", e);
            }
            _stream = null;
        }

       	public void SetInputStream(Stream ist) { // to reuse this object
		    this._stream = ist;
		    eof = false;
	    }

        public bool IsEof()
        {
            return eof;
        }

        public void SetFailIfNoFeed(bool failIfNoFeed)
        {
            this.failIfNoFeed = failIfNoFeed;
        }
    }
}
