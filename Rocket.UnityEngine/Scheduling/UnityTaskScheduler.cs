using Rocket.API.Scheduling;
using Rocket.UnityEngine.DependencyInjection;
using UnityEngine;

namespace Rocket.UnityEngine.Scheduling
{
    public class UnityTaskRunnerComponent : MonoBehaviour
    {
        [UnityAutoInject]
        private ITaskScheduler m_TaskScheduler { get; set; }


        protected virtual void Update()
        {
            m_TaskScheduler.RunFrameUpdate(ExecutionTargetSide.SyncFrame);
        }

        protected virtual void FixedUpdate()
        {
            m_TaskScheduler.RunFrameUpdate(ExecutionTargetSide.PhysicsFrame);
        }
    }
}