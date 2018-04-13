using System.Linq;
using System.Threading;
using Rocket.API.Scheduler;

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
                foreach (ITask task in scheduler.Tasks.Where(c => !c.IsFinished && !c.IsCancelled))
                {
                    if (task.ExecutionTarget != ExecutionTargetContext.EveryAsyncFrame)
                        continue;

                    task.Action.Invoke();
                }

                Thread.Sleep(20);
            }
        }

        private void SingleThreadLoop()
        {
            while (true)
            {
                _waitHandle.WaitOne();
                foreach (ITask task in scheduler.Tasks.Where(c => !c.IsFinished && !c.IsCancelled))
                {
                    if (task.ExecutionTarget != ExecutionTargetContext.NextAsyncFrame)
                        continue;

                    task.Action.Invoke();
                    scheduler.RemoveTask(task);
                }
            }
        }
    }
}