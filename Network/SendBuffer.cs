using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerLib
{
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

        public static int BlockSize { get; set; } = 8000;

        public static ArraySegment<byte> Open(int length)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(BlockSize);

            if (CurrentBuffer.Value.FreeSize < length)
                CurrentBuffer.Value = new SendBuffer(BlockSize);

            return CurrentBuffer.Value.Open(length);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }

    public class SendBuffer
    {
        byte[] buffer;
        int usedSize = 0;

        public int FreeSize { get { return buffer.Length - usedSize; } }

        public SendBuffer(int chunkSize)
        {
            buffer = new byte[chunkSize];
        }

        public ArraySegment<byte> Open(int length)
        {
            if (length > FreeSize)
                return null;

            return new ArraySegment<byte>(buffer, usedSize, length);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer, this.usedSize, usedSize);
            this.usedSize += usedSize;
            return segment;
        }
    }
}
