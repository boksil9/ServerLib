using Google.Protobuf;
using ServerLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
	public class SessionManager
	{
		public static SessionManager Instance { get; } = new SessionManager();

		int _sessionId = 0;
		ConcurrentDictionary<int, Session> _sessions = new ConcurrentDictionary<int, Session>();

		public T Generate<T>() where T : Session, new() 
		{
			int sessionId = ++_sessionId;

			T session = new T();
			session.SessionId = sessionId;
			session.SessionMgr = this;

			_sessions.TryAdd(sessionId, session);
			Console.WriteLine($"Connected : {sessionId}");

			return session;
		}

		public Session Seek(int id)
		{
			_sessions.TryGetValue(id, out Session session);
			return session;
		}

		public void Remove(Session session)
		{
			_sessions.TryRemove(session.SessionId, out Session removed);
		}

		public int Count()
		{
			return _sessions.Count;
		}

		public int DisconnectAll()
		{
			var sessions = _sessions.Values.ToList();

			foreach(var session in sessions)
				session.Disconnect();

			return sessions.Count;
		}

		public void BroadCast(IMessage packet)
		{
			var sessions = _sessions.Values.ToList();
	
			var seg = PacketBuilder.Build(packet);

			foreach (var session in sessions)
				session.Send(seg);

			PacketBuilder.ResetBuffer();
        }
	}
}
