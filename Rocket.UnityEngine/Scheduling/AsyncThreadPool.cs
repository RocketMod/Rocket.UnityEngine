﻿using System.Linq;
using System.Threading;
using Rocket.API.Scheduling;

namespace Rocket.UnityEngine.Scheduling
{
    public class AsyncThreadPool
    {
        private readonly UnityTaskScheduler scheduler;

        public AsyncThreadPool(UnityTaskScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private Thread singleCallsThread;
        private Thread continousCallsThreads;

        public void Start()
        {
            singleCallsThread = new Thread(SingleThreadLoop);
            singleCallsThread.Start();

            continousCallsThreads = new Thread(ContinousThreadLoop);
            continousCallsThreads.Start();
        }

        private void ContinousThreadLoop()
        {
            while (true)
            {
                var cpy = scheduler.Tasks.ToList(); // we need a copy because the task list may be modified at runtime

                foreach (ITask task in cpy.Where(c => !c.IsFinished && !c.IsCancelled))
                {
                    if(task.Period == null || (task.Period != null  && task.ExecutionTarget != ExecutionTargetContext.Async))         
                        if (task.ExecutionTarget != ExecutionTargetContext.EveryAsyncFrame)
                            continue;

                    scheduler.RunTask(task);
                }

                Thread.Sleep(20);
            }
        }

        private void SingleThreadLoop()
        {
            while (true)
            {
                _waitHandle.WaitOne();
                var cpy = scheduler.Tasks.ToList(); // we need a copy because the task list may be modified at runtime

                foreach (ITask task in cpy.Where(c => !c.IsFinished && !c.IsCancelled))
                {
                    if (task.ExecutionTarget != ExecutionTargetContext.NextAsyncFrame &&
                        task.ExecutionTarget != ExecutionTargetContext.Async)
                        continue;

                    scheduler.RunTask(task);
                }
            }
        }
    }
}
