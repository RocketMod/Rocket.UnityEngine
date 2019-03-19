﻿using System.Linq;
using System.Threading;
using Rocket.API.Scheduling;

namespace Rocket.UnityEngine.Scheduling
{
    public class AsyncThreadPool
    {
        private readonly UnityTaskScheduler m_TaskScheduler;
        public EventWaitHandle EventWaitHandle { get; }
        public AsyncThreadPool(UnityTaskScheduler scheduler)
        {
            m_TaskScheduler = scheduler;
            EventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        private Thread m_TaskThread;

        public void Start()
        {
            m_TaskThread = new Thread(ContinousThreadLoop);
            m_TaskThread.Start();
        }

        private void ContinousThreadLoop()
        {
            while (m_TaskScheduler)
            {
                var cpy = m_TaskScheduler.Tasks.Where(c => !c.IsFinished && !c.IsCancelled).ToList(); // we need a copy because the task list may be modified at runtime

                foreach (IScheduledTask task in cpy)
                {
                    if (task.Period == null || (task.Period != null && task.ExecutionTarget != ExecutionTargetContext.Async))
                        if (task.ExecutionTarget != ExecutionTargetContext.EveryAsyncFrame)
                            continue;

                    m_TaskScheduler.RunTask(task);
                }

                foreach (IScheduledTask task in cpy)
                {
                    if (task.ExecutionTarget != ExecutionTargetContext.NextAsyncFrame &&
                        task.ExecutionTarget != ExecutionTargetContext.Async)
                        continue;

                    m_TaskScheduler.RunTask(task);
                }

                if (cpy.Count == 0)
                {
                    EventWaitHandle.WaitOne();
                }

                Thread.Sleep(20);
            }
        }
    }
}