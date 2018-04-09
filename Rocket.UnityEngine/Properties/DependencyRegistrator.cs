using Rocket.API.DependencyInjection;
using Rocket.API.Scheduler;
using Rocket.Unity5.Scheduling;

namespace Rocket.Unity5.Properties
{
    public class DependencyRegistrator : IDependencyRegistrator
    {
        public void Register(IDependencyContainer container, IDependencyResolver resolver)
        {
            container.RegisterSingletonType<ITaskScheduler, UnityTaskScheduler>();
        }
    }
}