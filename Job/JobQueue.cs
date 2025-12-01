using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib
{
	public class JobQueue
	{
		JobTimerManager _timer = new JobTimerManager();
		Queue<JobBase> _queue = new Queue<JobBase>();
		object _lock = new object();

		public int JobCount { get { lock (_lock) { return _queue.Count; } } }

        public void PushAfter(int delay, Action action)
        {
			PushAfter(delay, (param) => { action.Invoke(); }, ValueTuple.Create());
        }

        public void PushAfter<T>(int delay, Action<T> action, T param)
		{
			var job = new Job<T>(action, param);
			PushAfter(delay, new Job<T>(action, param));
		}
		
		public void PushAfter(int delay, JobBase job)
		{
			lock(_lock)
			{
				job.Queue = this;
				_timer.Push(job, delay);
			}
		}

		public void Push(Action action)
		{
			Push((param) => { action.Invoke(); }, ValueTuple.Create());
		}

		public void Push<T>(Action<T> action, T param)
		{
			var job = new Job<T>(action, param);
			Push(job);
		}

		public void Push(JobBase job)
		{
			lock (_lock)
			{
				job.Queue = this;
				_queue.Enqueue(job);
			}
		}

		public void Flush()
		{
			while (true)
			{
				JobBase job = Pop();
				if (job == null)
					return;

				job.Execute();
			}
		}

		JobBase Pop()
		{
			lock (_lock)
			{
				if (_queue.Count == 0)
				{
					return null;
				}
				return _queue.Dequeue();
			}
		}
	}
}
