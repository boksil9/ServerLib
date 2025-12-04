using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerLib
{
    public class SendBuffer
    {
        private int _blockSize { get; set; } = 8000;
        private byte[] _buffer;
        private int _usedSize = 0;

        public int FreeSize { get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int blockSize)
        {
            _buffer = new byte[blockSize];
        }

        public ArraySegment<byte> Open(int length)
        {
            if (FreeSize < length)
                return null;

            return new ArraySegment<byte>(_buffer, _usedSize, length);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;
            return segment;
        }

        public void Reset()
        {
            _usedSize = 0; 
        }
    }
}
