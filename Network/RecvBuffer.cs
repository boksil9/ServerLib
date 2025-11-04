using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib
{
	public class RecvBuffer
	{
		ArraySegment<byte> _buffer;
		int _currentPos;
		int _receivedPos;

		public RecvBuffer(int maxBufSize)
		{
			_buffer = new ArraySegment<byte>(new byte[maxBufSize], 0, maxBufSize);
		}

		public int UnProcessedSize { get { return _receivedPos - _currentPos; } }
		public int WritableSize { get { return _buffer.Count - _receivedPos; } }

		public ArraySegment<byte> ReadSegment
		{
			get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _currentPos, UnProcessedSize); }
		}

		public ArraySegment<byte> WriteSegment
		{
			get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _receivedPos, WritableSize); }
		}

		public void Clean()
		{
			if(_currentPos == 0)
				return;

			int remainSize = UnProcessedSize;
			if (remainSize == 0)
			{
				_receivedPos = 0;
			}
			else
			{
				Array.Copy(_buffer.Array, _buffer.Offset + _currentPos, _buffer.Array, _buffer.Offset, remainSize);
				_receivedPos = remainSize;
			}

			_currentPos = 0;
		}

		public bool OnProcess(int bytes)
		{
			if (bytes > UnProcessedSize)
				return false;

			_currentPos += bytes;
			return true;
		}

		public bool OnRecv(int transferred)
		{
			if (transferred > WritableSize)
				return false;

			_receivedPos += transferred;
			return true;
		}
	}
}
