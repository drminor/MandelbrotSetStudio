using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PngImageLib.Zlib {

    public class ZlibStreamFactory {        
        public static AZlibInputStream CreateZlibInputStream(Stream st, bool leaveOpen) {
#if SHARPZIPLIB
            return new ZlibInputStreamIs(st, leaveOpen);
#else
            return new ZlibInputStreamMs(st, leaveOpen);
#endif
        }



        public static AZlibInputStream createZlibInputStream(Stream st) {
            return CreateZlibInputStream(st, false);
        }

        public static AZlibOutputStream CreateZlibOutputStream(Stream st, int compressLevel, EDeflateCompressStrategy strat, bool leaveOpen) {
#if SHARPZIPLIB
            return new ZlibOutputStreamIs(st, compressLevel, strat, leaveOpen);
#else
            return new ZlibOutputStreamMs(st, compressLevel, strat, leaveOpen);
#endif
        }

        public static AZlibOutputStream CreateZlibOutputStream(Stream st) {
            return CreateZlibOutputStream(st, false);
        }

        public static AZlibOutputStream CreateZlibOutputStream(Stream st, bool leaveOpen) {
            return CreateZlibOutputStream(st, DeflateCompressLevel.DEFAULT, EDeflateCompressStrategy.Default, leaveOpen);
        }
    }
}
