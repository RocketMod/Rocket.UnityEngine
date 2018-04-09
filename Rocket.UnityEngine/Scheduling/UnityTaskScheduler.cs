using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Rocket.API;
using Rocket.API.DependencyInjection;
using Rocket.API.Eventing;
using Rocket.API.Scheduler;
using UnityEngine;

namespace Rocket.Unity5.Scheduling
{
    public class UnityTaskScheduler : MonoBehaviour, ITaskScheduler
    {
        private readonly IDependencyContainer container;
        private readonly List<ITask> tasks;
        public ReadOnlyCollection<ITask> Tasks => tasks.AsReadOnly();

        public UnityTaskScheduler(IDependencyContainer container)
        {
            this.container = container;
            tasks = new List<ITask>();
        }

        public ITask ScheduleEveryFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextFrame);
            TriggerEvent(task);
            return task;
        }

        public ITask ScheduleNextFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextFrame);
            TriggerEvent(task);
            return task;
        }

        public ITask Schedule(ILifecycleObject @object, Action action, ExecutionTargetContext target)
        {
            UnityTask task = new UnityTask(this, @object, action, target);

            TriggerEvent(task, (sender, @event) =>
            {
                if (target != ExecutionTargetContext.Sync && @object.IsAlive) return;

                if (@event != null && ((ICancellableEvent) @event).IsCancelled) return;

                action();
                tasks.Remove(task);
            });

            return task;
        }

        public ITask ScheduleNextPhysicUpdate(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextPhysicsUpdate);
            TriggerEvent(task);
            return task;
        }

        public ITask ScheduleEveryPhysicUpdate(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.EveryPhysicsUpdate);
            TriggerEvent(task);
            return task;
        }

        public ITask ScheduleEveryAsyncFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.EveryAsyncFrame);
            TriggerEvent(task);
            return task;
        }

        public ITask ScheduleNextAsyncFrame(ILifecycleObject @object, Action action)
        {
            UnityTask task = new UnityTask(this, @object, action, ExecutionTargetContext.NextAsyncFrame);
            TriggerEvent(task);
            return task;
        }

        private void TriggerEvent(UnityTask task, EventCallback cb = null)
        {
            TaskScheduleEvent e = new TaskScheduleEvent(task);

            if (!(task.Owner is IEventEmitter owner)) return;

            IEventManager eventManager = container.Get<IEventManager>();

            if (eventManager == null)
            {
                tasks.Add(task);
                cb?.Invoke(owner, null);
                return;
            }

            eventManager?.Emit(owner, e, @event =>
            {
                task.IsCancelled = e.IsCancelled;

                if (!e.IsCancelled)
                    tasks.Add(task);

                cb?.Invoke(owner, @event);
            });
        }

        public bool CancelTask(ITask task)
        {
            if (task.IsFinished)
                return false;

            ((UnityTask) task).IsCancelled = true;
            return true;
        }

        public void Update()
        {
            foreach (ITask task in Tasks.Where(c => !c.IsFinished && !c.IsCancelled))
            {
                if (task.ExecutionTarget != ExecutionTargetContext.EveryFrame
                    && task.ExecutionTarget != ExecutionTargetContext.NextFrame)
                    continue;

                RunTask(task);
            }
        }

        public void FixedUpdate()
        {
            foreach (ITask task in Tasks.Where(c => !c.IsFinished && !c.IsCancelled))
            {
                if (task.ExecutionTarget != ExecutionTargetContext.EveryPhysicsUpdate
                    && task.ExecutionTarget != ExecutionTargetContext.NextPhysicsUpdate)
                    continue;

                RunTask(task);
            }
        }

        private void RunTask(ITask task)
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

        public void RemoveTask(ITask task)
        {
            UnityTask t = (UnityTask) task;
            t.IsFinished = true;
            tasks.Remove(task);
        }
    }
}