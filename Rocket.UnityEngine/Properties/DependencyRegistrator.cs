using Rocket.API.DependencyInjection;
using Rocket.API.Scheduler;
using Rocket.UnityEngine.Scheduling;

namespace Rocket.UnityEngine.Properties
{
    public class DependencyRegistrator : IDependencyRegistrator
    {
        public void Register(IDependencyContainer container, IDependencyResolver resolver)
        {
            container.RegisterSingletonType<ITaskScheduler, UnityTaskScheduler>();
        }
    }
}