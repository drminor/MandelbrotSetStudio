using PngImageLib;
using PngImageLib.Chunks;
using System;

namespace Ar.Com.Hjg.Pngcs
{
    class ChunkReader
    {
        protected ChunkReaderMode mode;
	    private readonly ChunkRaw chunkRaw;

	    private readonly bool crcCheck; // by default, this is false for SKIP, true elsewhere
	    protected int read = 0;
    	//private int crcn = 0; // how many bytes have been read from crc 

        public ChunkReader(int clen, String id, long offsetInPng, ChunkReaderMode mode)
        {
            if (id.Length != 4 || clen < 0)
                throw new PngjExceptionInternal("Bad chunk paramenters: " + mode);

            this.mode = mode;
            chunkRaw = new ChunkRaw(clen, id, mode == ChunkReaderMode.BUFFER)
            {
                Offset = offsetInPng
            };

            crcCheck = mode == ChunkReaderMode.SKIP ? false : true; // can be changed with setter
        }
    }
}
