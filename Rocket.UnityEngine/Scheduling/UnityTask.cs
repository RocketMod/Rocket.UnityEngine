using System;
using Rocket.API;
using Rocket.API.Scheduler;

namespace Rocket.UnityEngine.Scheduling
{
    public class UnityTask : ITask
    {
        private readonly UnityTaskScheduler scheduler;

        public UnityTask(UnityTaskScheduler scheduler, ILifecycleObject owner, Action action,
                         ExecutionTargetContext executionTargetContext)
        {
            this.scheduler = scheduler;
            Owner = owner;
            Action = action;
            ExecutionTarget = executionTargetContext;
        }

        public ILifecycleObject Owner { get; }

        public Action Action { get; }

        public bool IsCancelled { get; internal set; }

        public Exception Exception { get; internal set; }

        public ExecutionTargetContext ExecutionTarget { get; }

        public bool IsFinished { get; internal set; }

        public void Cancel()
        {
            scheduler.CancelTask(this);
        }
    }
}