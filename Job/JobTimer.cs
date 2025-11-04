using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib
{
	struct JobTimerItem : IComparable<JobTimerItem>
	{
		public long execTick; // 실행 시간
		public JobBase job;

		public int CompareTo(JobTimerItem other)
		{
			return (int)(other.execTick - execTick);
		}
	}

	public class JobTimer
	{
		PriorityQueue<JobTimerItem> _pq = new PriorityQueue<JobTimerItem>();
		object _lock = new object();

		public int Count { get { lock (_lock) { return _pq.Count; } } }

        public static long TickCount { get { return System.Environment.TickCount64; } }

        public void Push(JobBase job, int tickAfter = 0)
		{
			JobTimerItem jobItem;
			jobItem.execTick = TickCount + tickAfter;
			jobItem.job = job;

			lock (_lock)
			{
				_pq.Push(jobItem);
			}
		}

		public void Flush()
		{
			while (true)
			{
				long now = TickCount;

				JobTimerItem jobElement;

				lock (_lock)
				{
					if (_pq.Count == 0)
						break;

					jobElement = _pq.Peek();
					if (jobElement.execTick > now)
						break;

					_pq.Pop();
				}

				jobElement.job.Execute();
			}
		}
	}
}
