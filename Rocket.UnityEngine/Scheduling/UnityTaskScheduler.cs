using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Rocket.API;
using Rocket.API.Eventing;
using Rocket.API.DependencyInjection;
using Rocket.API.Scheduler;
using Rocket.Core.Logging;
using Rocket.Core.Scheduler;
using UnityEngine;
using ILogger = Rocket.API.Logging.ILogger;

namespace Rocket.UnityEngine.Scheduling
{
    public class UnityTaskScheduler : MonoBehaviour, ITaskScheduler
    {
        private static volatile int _taskIds = 0;

        protected IDependencyContainer Container { get; set; }
        protected List<ITask> InternalTasks { get; set; }

        public IEnumerable<ITask> Tasks =>
            InternalTasks.Where(c => c.Owner.IsAlive);

        public virtual void Load(IDependencyContainer container)
        {
            (new AsyncThreadPool(this)).Start();

            Container = container;
            InternalTasks = new List<ITask>();
        }

        protected virtual void OnDestroy()
        {
            var logger = Container.Resolve<ILogger>();
            logger.LogDebug("[UnityTaskScheduler] OnDestroy");

            foreach (var task in Tasks)
                task.Cancel();
            InternalTasks.Clear();
        }

        public virtual ITask ScheduleUpdate(ILifecycleObject @object, Action action, string taskName, ExecutionTargetContext target)
        {
            UnityTask task = new UnityTask(++_taskIds, taskName, this, @object, action, target);

            TriggerEvent(task, (sender, @event) =>
            {
                if (target != ExecutionTargetContext.Sync && @object.IsAlive) return;

                if (@event != null && ((ICancellableEvent)@event).IsCancelled) return;

                action();
                InternalTasks.Remove(task);
            });

            return task;
        }

        public virtual void TriggerEvent(UnityTask task, EventCallback cb = null)
        {
            TaskScheduleEvent e = new TaskScheduleEvent(task);

            if (!(task.Owner is IEventEmitter owner)) return;

            IEventManager eventManager = Container.Resolve<IEventManager>();

            if (eventManager == null)
            {
                InternalTasks.Add(task);
                cb?.Invoke(owner, null);
                return;
            }

            eventManager.Emit(owner, e, @event =>
            {
                task.IsCancelled = e.IsCancelled;

                if (!e.IsCancelled)
                    InternalTasks.Add(task);

                cb?.Invoke(owner, @event);
            });
        }

        public virtual bool CancelTask(ITask task)
        {
            if (task.IsFinished || task.IsCancelled)
                return false;

            ((UnityTask)task).IsCancelled = true;
            return true;
        }

        public ITask SchedulePeriodically(ILifecycleObject @object, Action action, string taskName, TimeSpan period,
                                          TimeSpan? delay = null, bool runAsync = false)
        {
            UnityTask task = new UnityTask(++_taskIds, taskName, this, @object, action,
                runAsync ? ExecutionTargetContext.Async : ExecutionTargetContext.Sync)
            {
                Period = period
            };

            if (delay != null)
                task.StartTime = DateTime.Now + delay;

            TriggerEvent(task);
            return task;
        }

        public ITask ScheduleAt(ILifecycleObject @object, Action action, string taskName, DateTime date, bool runAsync = false)
        {
            UnityTask task = new UnityTask(++_taskIds, taskName, this, @object, action,
                runAsync ? ExecutionTargetContext.Async : ExecutionTargetContext.Sync)
            {
                StartTime = date
            };
            TriggerEvent(task);
            return task;
        }

        public virtual void Update()
        {
            var cpy = Tasks.ToList(); // we need a copy because the task list may be modified at runtime
            foreach (ITask task in cpy.Where(c => !c.IsFinished && !c.IsCancelled))
            {
                if(task.Period == null && task.ExecutionTarget != ExecutionTargetContext.Sync) 
                    if (task.ExecutionTarget != ExecutionTargetContext.EveryFrame
                        && task.ExecutionTarget != ExecutionTargetContext.NextFrame)
                        continue;

                RunTask(task);
            }
        }

        public virtual void FixedUpdate()
        {
            var cpy = Tasks.ToList(); // we need a copy because the task list may be modified at runtime
            foreach (ITask task in cpy.Where(c => !c.IsFinished && !c.IsCancelled))
            {
                if (task.ExecutionTarget != ExecutionTargetContext.EveryPhysicsUpdate
                    && task.ExecutionTarget != ExecutionTargetContext.NextPhysicsUpdate)
                    continue;

                RunTask(task);
            }
        }

        protected internal virtual void RunTask(ITask task)
        {
            if (task.StartTime != null && task.StartTime > DateTime.Now)
                return;

            if (task.EndTime != null && task.EndTime < DateTime.Now)
            {
                ((UnityTask)task).EndTime = DateTime.Now;
                RemoveTask(task);
                return;
            }

            if (task.Period != null
                && ((UnityTask) task).LastRunTime != null
                && DateTime.Now - ((UnityTask) task).LastRunTime < task.Period)
                return;

            try
            {
                task.Action.Invoke();
                ((UnityTask)task).LastRunTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Container.Resolve<ILogger>().LogError("An exception occured in task: " + task.Name, e);
            }

            if (task.ExecutionTarget == ExecutionTargetContext.NextFrame
                || task.ExecutionTarget == ExecutionTargetContext.NextPhysicsUpdate
                || task.ExecutionTarget == ExecutionTargetContext.Async
                || task.ExecutionTarget == ExecutionTargetContext.NextAsyncFrame
                || task.ExecutionTarget == ExecutionTargetContext.Sync)
            {
                ((UnityTask)task).EndTime = DateTime.Now;
                RemoveTask(task);
            }
        }

        public virtual void RemoveTask(ITask task)
        {
            InternalTasks.Remove(task);
        }
    }
}
