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

		public JobBase PushAfter(int delay, Action action) { return PushAfter(delay, new Job(action)); }
		public JobBase PushAfter<T1>(int delay, Action<T1> action, T1 t1) { return PushAfter(delay, new Job<T1>(action, t1)); }
		public JobBase PushAfter<T1, T2>(int delay, Action<T1, T2> action, T1 t1, T2 t2) { return PushAfter(delay, new Job<T1, T2>(action, t1, t2)); }
		public JobBase PushAfter<T1, T2, T3>(int delay, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { return PushAfter(delay, new Job<T1, T2, T3>(action, t1, t2, t3)); }
		public JobBase PushAfter<T1, T2, T3, T4>(int delay, Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4) { return PushAfter(delay, new Job<T1, T2, T3, T4>(action, t1, t2, t3, t4)); }
		public JobBase PushAfter<T1, T2, T3, T4, T5>(int delay, Action<T1, T2, T3, T4, T5> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) { return PushAfter(delay, new Job<T1, T2, T3, T4, T5>(action, t1, t2, t3, t4, t5)); }
		public JobBase PushAfter<T1, T2, T3, T4, T5, T6>(int delay, Action<T1, T2, T3, T4, T5, T6> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) { return PushAfter(delay, new Job<T1, T2, T3, T4, T5, T6>(action, t1, t2, t3, t4, t5, t6)); }
		public JobBase PushAfter<T1, T2, T3, T4, T5, T6, T7>(int delay, Action<T1, T2, T3, T4, T5, T6, T7> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) { return PushAfter(delay, new Job<T1, T2, T3, T4, T5, T6, T7>(action, t1, t2, t3, t4, t5, t6, t7)); }

		public JobBase PushAfter(int delay, JobBase job)
		{
			job.Queue = this;
			_timer.Push(job, delay);
			return job;
		}

		public void Push(Action action) { Push(new Job(action)); }
		public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
		public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); }
		public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }
		public void Push<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4) { Push(new Job<T1, T2, T3, T4>(action, t1, t2, t3, t4)); }
		public void Push<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) { Push(new Job<T1, T2, T3, T4, T5>(action, t1, t2, t3, t4, t5)); }
		public void Push<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) { Push(new Job<T1, T2, T3, T4, T5, T6>(action, t1, t2, t3, t4, t5, t6)); }
		public void Push<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) { Push(new Job<T1, T2, T3, T4, T5, T6, T7>(action, t1, t2, t3, t4, t5, t6, t7)); }

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
