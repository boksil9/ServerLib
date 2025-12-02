using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerLib
{
	public class Listener : NetBase
	{
		Socket _listenSocket;
		private volatile bool _stopped = false;

		public void Listen(int backLog = 100)
		{
			if (_endPoint == null)
				throw new InvalidOperationException("[Listener::Listen()] Listen before initialized.\"");

			if (_listenSocket != null)
				Stop();

			_listenSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_listenSocket.Bind(_endPoint);
			_listenSocket.Listen(backLog);

            for (int i = 0; i < _maxConnections; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                _eventPool.Push(args);
            }

            _stopped = false;

			Console.WriteLine($"[Listenter::Listen()] Listening on. endpoint : {_endPoint}, backlog : {backLog}");

			_thread = new Thread(AcceptThread);
			_thread.Start();
        }

		void Stop()
		{
			if (_listenSocket == null)
				return;

			_stopped = true;

			try
			{
				_listenSocket.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine($"[Listener::Stop()] Socket close exception : {e}");
			}

            if (_thread != null)
            {
                _thread.Join();
                _thread = null;
            }

			_listenSocket = null;
		}

		void AcceptThread()
		{
			for (int i = 0; i < _maxConnections; i++)
			{
				if (_stopped)
					break;

				var args = _eventPool.Pop();
				if (args == null)
					break;

				StartAccept(args);
			}
		}

		void StartAccept(SocketAsyncEventArgs args)
		{
			if (_stopped)
				return;

			args.AcceptSocket = null;
			try
			{
				bool pending = _listenSocket.AcceptAsync(args);
				if (pending == false)
					OnAcceptCompleted(this, args);
			}
			catch(ObjectDisposedException)
			{
				return;
			}
            catch (Exception e)
            {
                Console.WriteLine($"[Listenter::StartAccept()] Accept exception : {e}");
                _eventPool.Push(args);
            }
        }

		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
		{
			if(_stopped)
			{
				CloseSocket(args.AcceptSocket);
				return;
			}
			try
			{
				if (args.SocketError == SocketError.Success)
				{
					Socket sock = args.AcceptSocket;
					if (sock == null || sock.Connected == false)
					{
						Console.WriteLine("[Listenter::OnAcceopCompleted()] Invalid socket");
						// TODO : 연결은 됐는데 소켓 에러일 때 정책
					}
					else
					{
						Session session = NewSession(args);
						if (session == null)
						{
							Console.WriteLine("[Listener::OnAccepCompleted()] Session creation failed. Close socket");
							CloseSocket(sock);
						}
						else
						{
							try
							{
								session.Start(args.AcceptSocket);
								session.OnConnected(args.AcceptSocket.RemoteEndPoint);
							}
							catch (Exception e)
							{
								Console.WriteLine($"[Listener::OnAccepCompleted()] Session Start/OnConnected exception : {e}");
								CloseSocket(sock);
							}
						}
					}
				}
				else
				{
					Console.WriteLine($"[Listener::OnAccepCompleted()] Accept failed : {args.SocketError}");
					// TODO : 소켓 연결 실패 정책
                }
			}
			catch (Exception e)
			{
				Console.WriteLine($"[Listener::OnAccepCompleted()] Accept exception : {e}");
            }
			finally
			{
				if(_stopped == false)
				{
					StartAccept(args);
				}
				else
				{
					CloseSocket(args.AcceptSocket);
                }
			}
		}
	}
}
