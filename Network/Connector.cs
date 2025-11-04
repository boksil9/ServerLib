using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerLib
{
	public class Connector : NetBase
	{
		public void Connect()
		{
			Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				
			args.Completed += OnConnected;
			args.RemoteEndPoint = endPoint;
			args.UserToken = socket;

			WaitForConnect(args);
		}

		void WaitForConnect(SocketAsyncEventArgs args)
		{
			Socket socket = args.UserToken as Socket;
			if (socket == null)
				return;

			bool pending = socket.ConnectAsync(args);
			if (pending == false)
				OnConnected(null, args);
		}

		void OnConnected(object sender, SocketAsyncEventArgs args)
		{
			if (args.SocketError == SocketError.Success)
			{
				Session session = NewSession(args);
				if (session == null)
					return;

				session.Start(args.ConnectSocket);
				session.OnConnected(args.RemoteEndPoint);
			}
			else
			{
				Console.WriteLine($"OnConnected Fail: {args.SocketError}");
				WaitForConnect(args);
			}
		}
	}
}
