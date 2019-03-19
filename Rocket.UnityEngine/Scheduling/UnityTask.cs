using System;
using System.Linq;
using Rocket.API;
using Rocket.API.Scheduling;

namespace Rocket.UnityEngine.Scheduling
{
    public class UnityTask : IScheduledTask
    {
        public UnityTask(int taskId, string name, UnityTaskScheduler scheduler, 
                         ILifecycleObject owner, Action action,
                         ExecutionTargetContext executionTargetContext)
        {
            TaskId = taskId;
            Name = name;
            Scheduler = scheduler;
            Owner = owner;
            Action = action;
            ExecutionTarget = executionTargetContext;
        }

        public int TaskId { get; }

        public string Name { get; }
        public TimeSpan? Period { get; internal set; }
        public DateTime? StartTime { get; internal set; }
        public DateTime? EndTime { get; internal set; }
        public DateTime? LastRunTime { get; internal set; }
        public ILifecycleObject Owner { get; }

        public Action Action { get; }

        public bool IsCancelled { get; internal set; }

        public ExecutionTargetContext ExecutionTarget { get; }

        public bool IsFinished => IsCancelled || !Scheduler.Tasks.Contains(this);

        public ITaskScheduler Scheduler { get; }
    }
}