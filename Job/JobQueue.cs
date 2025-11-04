using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib
{
	public class JobQueue
	{
		JobTimer _timer = new JobTimer();
		Queue<JobBase> _jobQueue = new Queue<JobBase>();
		object _lock = new object();

		public int TimerCount { get { return _timer.Count; } }
		public int JobCount { get { lock (_lock) { return _jobQueue.Count; } } }

		public JobBase PushAfter(int tickAfter, Action action) { return PushAfter(tickAfter, new Job(action)); }
		public JobBase PushAfter<T1>(int tickAfter, Action<T1> action, T1 t1) { return PushAfter(tickAfter, new Job<T1>(action, t1)); }
		public JobBase PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { return PushAfter(tickAfter, new Job<T1, T2>(action, t1, t2)); }
		public JobBase PushAfter<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { return PushAfter(tickAfter, new Job<T1, T2, T3>(action, t1, t2, t3)); }
		public JobBase PushAfter<T1, T2, T3, T4>(int tickAfter, Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4) { return PushAfter(tickAfter, new Job<T1, T2, T3, T4>(action, t1, t2, t3, t4)); }
		public JobBase PushAfter<T1, T2, T3, T4, T5>(int tickAfter, Action<T1, T2, T3, T4, T5> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) { return PushAfter(tickAfter, new Job<T1, T2, T3, T4, T5>(action, t1, t2, t3, t4, t5)); }
		public JobBase PushAfter<T1, T2, T3, T4, T5, T6>(int tickAfter, Action<T1, T2, T3, T4, T5, T6> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) { return PushAfter(tickAfter, new Job<T1, T2, T3, T4, T5, T6>(action, t1, t2, t3, t4, t5, t6)); }
		public JobBase PushAfter<T1, T2, T3, T4, T5, T6, T7>(int tickAfter, Action<T1, T2, T3, T4, T5, T6, T7> action, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) { return PushAfter(tickAfter, new Job<T1, T2, T3, T4, T5, T6, T7>(action, t1, t2, t3, t4, t5, t6, t7)); }

		public JobBase PushAfter(int tickAfter, JobBase job)
		{
			_timer.Push(job, tickAfter);
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
				_jobQueue.Enqueue(job);
			}
		}

		public void Flush()
		{
			_timer.Flush();

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
				if (_jobQueue.Count == 0)
				{
					return null;
				}
				return _jobQueue.Dequeue();
			}
		}
	}
}
