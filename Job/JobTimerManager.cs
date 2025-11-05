using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace ServerLib
{
	class JobTimer : System.Timers.Timer
	{
		public JobTimer(double interval, JobBase job)
			: base(interval)
		{
			Job = job;
		}

        public JobBase Job { get; set; }
	}

	public class JobTimerManager
	{
        public void Push(JobBase job, int delay = 0)
		{
			JobTimer timer = new JobTimer(delay, job);
	
			timer.Elapsed += TimerCallBack;
            timer.AutoReset = false;
			timer.Start();
		}

		static void TimerCallBack(object sender, ElapsedEventArgs e)
		{
			JobTimer timer = sender as JobTimer;
			if (timer == null)
				return;

			JobQueue queue = timer.Job.Queue;
			if (queue == null)
				return;

			queue.Push(timer.Job);
		}
	}
}
