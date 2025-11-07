using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerLib
{
    public class SendBufferHelper
    {
        private static ThreadLocal<SendBuffer> _currentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

        private static int _blockSize { get; set; } = 8000;

        public static ArraySegment<byte> Open(int length)
        {
            if (_currentBuffer.Value == null)
                _currentBuffer.Value = new SendBuffer(_blockSize);

            if (_currentBuffer.Value.FreeSize < length)
                _currentBuffer.Value = new SendBuffer(_blockSize);

            return _currentBuffer.Value.Open(length);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return _currentBuffer.Value.Close(usedSize);
        }
    }

    public class SendBuffer
    {
        private byte[] _buffer;
        private int _usedSize = 0;

        public int FreeSize { get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int blockSize)
        {
            _buffer = new byte[blockSize];
        }

        public ArraySegment<byte> Open(int length)
        {
            if (length > FreeSize)
                return null;

            return new ArraySegment<byte>(_buffer, _usedSize, length);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;
            return segment;
        }
    }
}
