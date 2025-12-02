using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerLib
{
    public class NetBase
    {
        protected Func<Session> _fnMakeSession;
        protected IPEndPoint _endPoint;
        protected Thread _thread; 
        protected SocketAsyncEventArgsPool _eventPool; 
        protected readonly int _maxConnections = 100;

        public NetBase()
        {
            _eventPool = new SocketAsyncEventArgsPool(_maxConnections);
        }

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _fnMakeSession = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        protected Session NewSession(SocketAsyncEventArgs args)
        {
            Socket sock = args.UserToken as Socket;
            if (sock == null)
            {
                Console.WriteLine("[NetBase::NewSession()] Create new session failed. Socket is null.");
                return null;
            }

            Session session = null;

            try
            {
                session = _fnMakeSession.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NetBase::NewSession()] Session factory exception : {e}");
            }

            if (session == null)
            {
                Console.WriteLine($"[NetBase::NewSession()] Create New Session Failed. EndPoint : {args.RemoteEndPoint}");
                CloseSocket(sock);
                return null;
            }

            return session;
        }

        protected void CloseSocket(Socket socket)
        {
            if (socket == null)
                return;

            try
            {
                if(socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine($"[NetBase::CloseSocket()] Socket shutdown socket exception: {e.SocketErrorCode}");
            }
            catch(Exception e)
            {
                Console.WriteLine($"[NetBase::CloseSocket()] Socket shutdown exception : {e}");
            }
            finally
            {
                try
                {
                    socket.Close();
                }
                catch(Exception e)
                {
                    Console.WriteLine($"[NetBase::CloseSocket()] Socket close exception : {e}");
                }
            }
        }
    }
}
