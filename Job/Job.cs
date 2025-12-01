using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerLib
{
	public abstract class JobBase
	{
		public abstract void Execute();
		public bool Cancel { get; set; } = false;
        public JobQueue? Queue { get; set; }
	}

	public class Job<T> : JobBase
	{
        private readonly Action<T> _action;
        private readonly T _param;

		public Job(Action<T> action, T param)
		{
			_action = action;
			_param = param;
		}

		public override void Execute()
		{
			if (Cancel == false)
				_action.Invoke(_param);
		}
	}
}
