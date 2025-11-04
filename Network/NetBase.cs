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
        protected Func<Session> fnMakeSession;
        protected IPEndPoint endPoint;

        public void Init(IPEndPoint endPoint, Func<Session> fn)
        {
            this.endPoint = endPoint;
            fnMakeSession = fn;
        }

        protected Session NewSession(SocketAsyncEventArgs args)
        {
            Session session = fnMakeSession.Invoke();
            if (session == null)
            {
                Console.WriteLine($"Make New Session Failed. EndPoint : {args.RemoteEndPoint}");
                Socket socket = args.UserToken as Socket;
                if (socket == null)
                    return null;

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            return session;
        }
    }
}
