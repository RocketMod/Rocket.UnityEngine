using Rocket.API;
using Rocket.API.Eventing;
using Rocket.API.Scheduling;
using Rocket.Core.Logging;
using Rocket.Core.Scheduling;
using Rocket.UnityEngine.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using ILogger = Rocket.API.Logging.ILogger;

namespace Rocket.UnityEngine.Scheduling
{
    public class UnityTaskScheduler : MonoBehaviour, ITaskScheduler
    {
        public IEnumerable<ITask> Tasks => m_Tasks;

        private static volatile int m_NextTaskId;

        [UnityAutoInject]
        private ILogger m_Logger { get; set; }

        [UnityAutoInject]
        private IEventBus m_EventBus { get; set; }

        public Thread MainThread { get; private set; }

        private readonly List<ITask> m_Tasks = new List<ITask>();
        private AsyncThreadPool m_AsyncThreadPool;

        protected virtual void Awake()
        {
            MainThread = Thread.CurrentThread;
            m_AsyncThreadPool = new AsyncThreadPool(this);
            m_AsyncThreadPool.Start();
        }

        protected virtual void OnDestroy()
        {
            foreach (var task in Tasks)
                task.Cancel();

            m_Tasks.Clear();
        }

        public virtual ITask ScheduleUpdate(ILifecycleObject @object, Action action, string taskName, ExecutionTargetContext target)
        {
            UnityTask task = new UnityTask(++m_NextTaskId, taskName, this, @object, action, target);
            TriggerEvent(task, async (sender, @event) =>
            {
                if (target != ExecutionTargetContext.Sync)
                    return;

                if (@event != null && ((ICancellableEvent)@event).IsCancelled)
                    return;

                action();
                m_Tasks.Remove(task);
            });

            return task;
        }

        public virtual void TriggerEvent(UnityTask task, EventCallback cb = null)
        {
            if (task.ExecutionTarget == ExecutionTargetContext.Async || task.ExecutionTarget == ExecutionTargetContext.NextAsyncFrame ||
                task.ExecutionTarget == ExecutionTargetContext.EveryAsyncFrame)
                m_AsyncThreadPool.EventWaitHandle.Set();

            if (!(task.Owner is IEventEmitter owner)) return;

            TaskScheduleEvent e = new TaskScheduleEvent(task);

            if (m_EventBus == null)
            {
                m_Tasks.Add(task);
                cb?.Invoke(owner, null);
                return;
            }

            m_EventBus.Emit(owner, e, async @event =>
            {
                task.IsCancelled = e.IsCancelled;

                if (!e.IsCancelled)
                    m_Tasks.Add(task);

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
            UnityTask task = new UnityTask(++m_NextTaskId, taskName, this, @object, action, runAsync ? ExecutionTargetContext.Async : ExecutionTargetContext.Sync)
            {
                Period = period
            };

            if (delay != null)
            {
                task.StartTime = DateTime.UtcNow + delay;
            }

            TriggerEvent(task);
            return task;
        }

        public ITask ScheduleAt(ILifecycleObject @object, Action action, string taskName, DateTime date, bool runAsync = false)
        {
            UnityTask task = new UnityTask(++m_NextTaskId, taskName, this, @object, action,
                runAsync ? ExecutionTargetContext.Async : ExecutionTargetContext.Sync)
            {
                StartTime = date
            };

            TriggerEvent(task);
            return task;
        }

        protected virtual void Update()
        {
            var cpy = Tasks.ToList(); // we need a copy because the task list may be modified at runtime
            foreach (ITask task in cpy.Where(c => !c.IsFinished && !c.IsCancelled))
            {
                if (task.Period == null || (task.Period != null && task.ExecutionTarget != ExecutionTargetContext.Sync))
                    if (task.ExecutionTarget != ExecutionTargetContext.EveryFrame
                        && task.ExecutionTarget != ExecutionTargetContext.NextFrame)
                        continue;

                RunTask(task);
            }
        }

        protected virtual void FixedUpdate()
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
            if (task.StartTime != null && task.StartTime > DateTime.UtcNow)
            {
                return;
            }

            if (task.EndTime != null && task.EndTime < DateTime.UtcNow)
            {
                ((UnityTask)task).EndTime = DateTime.UtcNow;
                RemoveTask(task);
                return;
            }

            if (task.Period != null
                && ((UnityTask)task).LastRunTime != null
                && DateTime.UtcNow - ((UnityTask)task).LastRunTime < task.Period)
            {
                return;
            }

            try
            {
                task.Action.Invoke();
                ((UnityTask)task).LastRunTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                m_Logger.LogError("An exception occured in task: " + task.Name, e);
            }

            if (task.ExecutionTarget == ExecutionTargetContext.NextFrame
                || task.ExecutionTarget == ExecutionTargetContext.NextPhysicsUpdate
                || (task.ExecutionTarget == ExecutionTargetContext.Async && task.Period == null)
                || task.ExecutionTarget == ExecutionTargetContext.NextAsyncFrame
                || task.ExecutionTarget == ExecutionTargetContext.Sync && task.Period == null)
            {
                ((UnityTask)task).EndTime = DateTime.UtcNow;
                RemoveTask(task);
            }
        }

        public virtual void RemoveTask(ITask task)
        {
            m_Tasks.Remove(task);
        }
    }
}