using System;

namespace Ar.Com.Hjg.Pngcs
{
    interface IBytesConsumer
    {
        int Consume(Byte[] buf, int offset, int tofeed);
    }
}