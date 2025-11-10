using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerLib
{
	public abstract class PacketSession : Session
	{
		public static readonly int HeaderSize = 2;

		public virtual void Send(IMessage packet)
		{
            Send(new ArraySegment<byte>(MakeSendBuffer(packet)));
        }

        public virtual byte[] MakeSendBuffer(IMessage packet)
        {
            ushort size = (ushort)packet.CalculateSize();

            ArraySegment<byte> segment = SendBufferHelper.Open(4096);
            Array.Copy(BitConverter.GetBytes((ushort)(size + sizeof(ushort))), 0, segment.Array, segment.Offset, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, segment.Array, segment.Offset + sizeof(ushort), size);

            return SendBufferHelper.Close(size + sizeof(ushort)).Array;
        }
		

        public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			int processLen = 0;

			while (true)
			{
				if (buffer.Count < HeaderSize)
					break;

				ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
				if (buffer.Count < dataSize)
					break;

				if (dataSize < HeaderSize)
					break;

				OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

				processLen += dataSize;
				buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
			}

			return processLen;
		}

		public abstract void OnRecvPacket(ArraySegment<byte> buffer);
	}

	public abstract class Session
	{
		Socket _socket;
		int _disconnected = 0;
        public int SessionId { get; set; }
		public SessionManager SessionMgr { get; set; }

        RecvBuffer _recvBuffer = new RecvBuffer(65535);

		object _lock = new object();
		Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

		public abstract void OnConnected(EndPoint endPoint);
		public abstract int  OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		bool IsDone() { return _sendQueue.Count > 0; }
		bool HasPending() { return _pendingList.Count > 0; }

		void Clear()
		{
			lock (_lock)
			{
				_sendQueue.Clear();
				_pendingList.Clear();
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
		}

		public void Start(Socket socket)
		{
			_socket = socket;

			_recvArgs.Completed += OnRecvCompleted;
			_sendArgs.Completed += OnSendCompleted;

			RegisterRecv();
		}

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (false == HasPending())
					SendPending();
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (_lock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (false == HasPending())
					SendPending();
			}
		}

		public void Disconnect()
		{
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			OnDisconnected(_socket.RemoteEndPoint);
			Clear();
			
			if (SessionMgr != null)
				SessionMgr.Remove(this);
		}

		void SendPending()
		{
			if (_disconnected == 1)
				return;

			while (false == IsDone())
			{
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;

			try
			{
				bool pending = _socket.SendAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Session::SendPending Failed : {e}");
				Disconnect();
			}
		}

		void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (_lock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						_sendArgs.BufferList = null;
						_pendingList.Clear();

						OnSend(_sendArgs.BytesTransferred);

						if (false == IsDone())
							SendPending();
					}
					catch (Exception e)
					{
						Console.WriteLine($"OnSendCompleted Failed {e}");
					}
				}
				else
				{
					Disconnect();
				}
			}
		}

		void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			_recvBuffer.Grow();

            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
				bool pending = _socket.ReceiveAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}

		void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (args.BytesTransferred == 0)
			{
				Console.WriteLine($"OnRecvComplted Failed. Recv Zero.");
				return;
			}

			if (args.SocketError == SocketError.Success)
			{
				try
				{
					if (_recvBuffer.OnRecv(args.BytesTransferred) == false)
					{
						Disconnect();
						return;
					}

					int processLen = OnRecv(_recvBuffer.ReadSegment);
					if (processLen < 0 || _recvBuffer.UnProcessedSize < processLen)
					{
						Disconnect();
						return;
					}

					if (_recvBuffer.OnProcess(processLen) == false)
					{
						Disconnect();
						return;
					}

                    _recvBuffer.Clean();
                    RegisterRecv();
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnRecvCompleted Failed {e}");
					Disconnect();
				}
			}
			else
			{
				Disconnect();
			}
		}
	}
}
