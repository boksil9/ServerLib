using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib
{
	public class RecvBuffer
	{
		private ArraySegment<byte> _buffer;
		private int _currentPos;
		private int _receivedPos;
		private int _origBufSize;

		public RecvBuffer(int bufSize)
		{
			_buffer = new ArraySegment<byte>(new byte[bufSize], 0, bufSize);
			_origBufSize = bufSize;
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

				if(_buffer.Count != _origBufSize)
					_buffer = new ArraySegment<byte>(new byte[_origBufSize], 0, _origBufSize);
			}
			else
			{
				Array.Copy(_buffer.Array, _buffer.Offset + _currentPos, _buffer.Array, _buffer.Offset, remainSize);
				_receivedPos = remainSize;
			}

			_currentPos = 0;
		}

		public void Grow()
		{
			if(WritableSize == 0)
			{
				int newSize = _buffer.Count + _origBufSize;
				var newBuffer = new ArraySegment<byte>(new byte[newSize], 0 , newSize);

				Array.Copy(_buffer.Array, _buffer.Offset + _currentPos, newBuffer.Array, newBuffer.Offset, _currentPos);

				_buffer = newBuffer;
			}
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
