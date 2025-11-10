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

		public void Listen(int register = 10)
		{
			if (_listenSocket != null)
				Clear();

			_listenSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_listenSocket.Bind(_endPoint);
			_listenSocket.Listen(100);

			for (int i = 0; i < register; i++)
			{
				SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
				WaitForAccept(args);
			}
		}

		void Clear()
		{
			_listenSocket.Shutdown(SocketShutdown.Both);
			_listenSocket.Close();
			_listenSocket = null;
		}

		void WaitForAccept(SocketAsyncEventArgs args)
		{
			args.AcceptSocket = null;

			bool pending = _listenSocket.AcceptAsync(args);
			if (pending == false)
				OnAcceptCompleted(null, args);
		}

		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
		{
			try
			{
				if (args.SocketError == SocketError.Success)
				{
					Session session = NewSession(args);
					if (session == null)
					{
						args.AcceptSocket.Shutdown(SocketShutdown.Both);
						args.AcceptSocket.Close();
						args.AcceptSocket = null;

						return;
					}

                    session.Start(args.AcceptSocket);
					session.OnConnected(args.AcceptSocket.RemoteEndPoint);
				}
				else
					Console.WriteLine(args.SocketError.ToString());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}			

			WaitForAccept(args);
		}
	}
}
