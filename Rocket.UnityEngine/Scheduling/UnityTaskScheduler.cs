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
        protected IDependencyContainer Container { get; set; }
        protected List<ITask> InternalTasks { get; set; }

        public ReadOnlyCollection<ITask> Tasks => 
            InternalTasks.Where(c => c.Owner.IsAlive).ToList().AsReadOnly();

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

        public virtual ITask ScheduleEveryFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.EveryFrame);
            TriggerEvent(task);
            return task;
        }

        public virtual ITask ScheduleNextFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextFrame);
            TriggerEvent(task);
            return task;
        }

        public virtual ITask Schedule(ILifecycleObject @object, Action action, ExecutionTargetContext target)
        {
            UnityTask task = new UnityTask(this, @object, action, target);

            TriggerEvent(task, (sender, @event) =>
            {
                if (target != ExecutionTargetContext.Sync && @object.IsAlive) return;

                if (@event != null && ((ICancellableEvent) @event).IsCancelled) return;

                action();
                InternalTasks.Remove(task);
            });

            return task;
        }

        public virtual ITask ScheduleNextPhysicUpdate(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextPhysicsUpdate);
            TriggerEvent(task);
            return task;
        }

        public virtual ITask ScheduleEveryPhysicUpdate(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.EveryPhysicsUpdate);
            TriggerEvent(task);
            return task;
        }

        public virtual ITask ScheduleEveryAsyncFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.EveryAsyncFrame);
            TriggerEvent(task);
            return task;
        }

        public virtual ITask ScheduleNextAsyncFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextAsyncFrame);
            TriggerEvent(task);
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

            ((UnityTask) task).IsCancelled = true;
            return true;
        }

        public virtual void Update()
        {
            var cpy = Tasks.ToList(); // we need a copy because the task list may be modified at runtime
            foreach (ITask task in cpy.Where(c => !c.IsFinished && !c.IsCancelled))
            {
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

        protected virtual void RunTask(ITask task)
        {
            try
            {
                task.Action.Invoke();
            }
            catch (Exception e)
            {
                ((UnityTask) task).Exception = e;
            }

            if (task.ExecutionTarget == ExecutionTargetContext.NextFrame
                || task.ExecutionTarget == ExecutionTargetContext.NextPhysicsUpdate)
                RemoveTask(task);
        }

        public virtual void RemoveTask(ITask task)
        {
            UnityTask t = (UnityTask) task;
            t.IsFinished = true;
            InternalTasks.Remove(task);
        }
    }
}
