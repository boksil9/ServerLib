using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerLib
{
	public class Connector : NetBase, IDisposable
	{
		private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(10);
		private CancellationTokenSource _cts = new CancellationTokenSource();
		private bool _reconnect = false;

		public void Connect(bool reconnect)
		{
			if (_endPoint == null)
				throw new InvalidOperationException("[Connector::Connect()] Connect before initialized.");

			_reconnect = reconnect;

			StartConnect();
		}

        void Stop()
        {
			if (_cts.IsCancellationRequested)
				return;

			_cts.Cancel();
			Console.WriteLine("[Connector::Stop()] Stop Connection");
        }

        public void Dispose()
        {
			Stop();
			_cts.Dispose();
        }

        void StartConnect()
		{
            if (_cts.IsCancellationRequested)
                return;

            Socket socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            args.RemoteEndPoint = _endPoint;
            args.UserToken = socket;
            args.Completed += OnConnected;

            Console.WriteLine($"[Connector::Connect()] Connecting to server... endPoint : {_endPoint}");
            TryConnect(args);
        }

		void TryConnect(SocketAsyncEventArgs args)
		{
			Socket sock = args.UserToken as Socket;
			if(sock == null)
			{
				Console.WriteLine("[Connector::TryConnect()] Connection failed. Socket is null");
				return;
			}

			bool pending = false;
			try
			{
				pending = sock.ConnectAsync(args);
			}
			catch(Exception e)
			{
				Console.WriteLine($"[Connector::TryConnect()] Connection failed. ConnectAsync Exception : {e}");
				
				CloseSocket(sock);
				Reconnect();
				
				return;
			}

			if(pending == false)
			{
				OnConnected(this, args);
			}
		}

		void OnConnected(object sender, SocketAsyncEventArgs args)
		{
			Socket sock = args.UserToken as Socket;
            if (sock == null)
            {
                Console.WriteLine("[Connector::OnConnected()] OnConnected failed. Socket is null");
                return;
            }

            if (args.SocketError == SocketError.Success && sock.Connected)
			{
				Console.WriteLine($"[Connector::OnConnected()] Server connected : {args.RemoteEndPoint}");

				Session session = NewSession(args);
				if (session == null)
				{
					Console.WriteLine("[Connector::OnConnected()] Session creation failed. Close socket");
					CloseSocket(sock);
					return;
				}

				try
				{
					session.Start(sock);
					session.OnConnected(args.RemoteEndPoint);
				}
				catch(Exception e)
				{
					Console.WriteLine($"[Connector::OnConnected()] Session Start/OnConnected exception : {e}" );
					CloseSocket(sock);
				}
			}
			else
			{
				Console.WriteLine($"[Connector::OnConnected()] Connection failed : {args.SocketError}");

				CloseSocket(sock);
				args.Dispose();

				Reconnect();
			}
		}

		async void Reconnect()
		{
			if (_reconnect == false)
				return;

			if (_cts.IsCancellationRequested)
				return;

			try
			{
				await Task.Delay(_reconnectDelay, _cts.Token);
			}
			catch (TaskCanceledException) { return; }

			if (_cts.IsCancellationRequested)
				return;

			StartConnect();
		}
	}
}
