using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			var seg = PacketBuilder.Build(packet);
            Send(seg);
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
		~Session()
		{
			if(_socket != null)
			{
                try { _socket.Shutdown(SocketShutdown.Both); } catch { }
                try { _socket.Close(); } catch { }
            }
        }

		Socket _socket;
		int _disconnected = 0;
        public int SessionId { get; set; }
		public SessionManager SessionMgr { get; set; }

        protected RecvBuffer _recvBuffer = new RecvBuffer(65535);
		//protected SendBuffer _sendBuffer = new SendBuffer(65535);

		object _lock = new object();
		Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

		public abstract void OnConnected(EndPoint endPoint);
		public abstract int  OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		bool EmptySendQueue() { return _sendQueue.Count == 0; }
		bool HasPending() { return _pendingList.Count > 0; }

		void Clear()
		{
			lock (_lock)
			{
				_sendQueue.Clear();
				_pendingList.Clear();
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
            if (_disconnected == 1)
                return;

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
			if (_disconnected == 1)
				return;

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

			var socket = _socket;
			_socket = null;

			try { OnDisconnected(socket?.RemoteEndPoint); }
			catch
			(Exception e)
			{ Debug.WriteLine($"[Session::Disconnect()] : OnDiscconnected exception : {e}"); }

			Clear();
			
			if (SessionMgr != null)
				SessionMgr.Remove(this);

			if(socket != null)
			{
				try { socket.Shutdown(SocketShutdown.Both); } catch { }
				try { socket.Close(); } catch { }
			}

			GC.SuppressFinalize(this);
		}

		void SendPending()
		{
			if (_disconnected == 1)
				return;

			lock(_lock)
			{
                while (false == EmptySendQueue())
                {
                    ArraySegment<byte> buff = _sendQueue.Dequeue();
                    _pendingList.Add(buff);
                }
                _sendArgs.BufferList = _pendingList;
            }
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
			if(_disconnected == 1)
				return;
#if DEBUG
            int pendingBytes = 0;
            foreach (var segment in _pendingList) { pendingBytes += segment.Count; }
            Debug.Assert(args.BytesTransferred <= pendingBytes, "[Session::OnSendCompleted()] Transffered more than pending bytes");
#endif
			bool needSend = false;
            
			lock (_lock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						RemoveSent(args.BytesTransferred);
						_sendArgs.BufferList = null;

						OnSend(args.BytesTransferred);

						if (HasPending() || EmptySendQueue() == false) // PendingList가 남았거나 SendQueue에 새 항목이 있음
							needSend = true;
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

			if (needSend)
				SendPending();
		}

        void RemoveSent(int transferred)
        {
			int remain = transferred;
			while (remain > 0 && _pendingList.Count > 0)
			{
				var segment = _pendingList[0];

				if(segment.Count <= remain)
				{
					remain -= segment.Count;
					_pendingList.RemoveAt(0);
				}
				else
				{
					_pendingList[0] = new ArraySegment<byte>(segment.Array, segment.Offset + remain, segment.Count - remain);
					remain = 0;
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
			if (_disconnected == 1)
				return;

			if (args.BytesTransferred == 0)
			{
				Disconnect();
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
