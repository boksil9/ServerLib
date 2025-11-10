using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
	public class SessionManager
	{
		public static SessionManager Instance { get; } = new SessionManager();

		int _sessionId = 0;

		Dictionary<int, Session> _sessions = new Dictionary<int, Session>();
		object _lock = new object();

		public List<Session> GetSessions()
		{
			List<Session> sessions = new List<Session>();

			lock (_lock)
			{
				sessions = _sessions.Values.ToList();
			}

			return sessions;
		}

		public T Generate<T>() where T : Session, new() 
		{
			lock (_lock)
			{
				int sessionId = ++_sessionId;

				T session = new T();
				session.SessionId = sessionId;
				session.SessionMgr = this;

				_sessions.Add(sessionId, session);
				Console.WriteLine($"Connected : {sessionId}");

				return session;
			}
		}

		public Session Seek(int id)
		{
			lock (_lock)
			{
				_sessions.TryGetValue(id, out Session session);
				return session;
			}
		}

		public void Remove(Session session)
		{
			lock (_lock)
			{
				_sessions.Remove(session.SessionId);
			}
		}

		public int Count()
		{
			lock (_lock)
			{
				return _sessions.Count;
			}
		}

		public int DisconnectAll()
		{
			var sessions = GetSessions();

			lock(_lock)
			{
				foreach(var session in sessions)
					session.Disconnect();

				return sessions.Count;
			}
		}

		public void BroadCast(ArraySegment<byte> sendBuff)
		{
			lock(_lock)
			{
				foreach(var session in _sessions)
				{
					session.Value.Send(sendBuff);
				}
			}
		}
	}
}
