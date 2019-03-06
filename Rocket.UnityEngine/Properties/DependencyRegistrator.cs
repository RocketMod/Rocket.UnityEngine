using Rocket.API.DependencyInjection;
using Rocket.API.Scheduling;
using Rocket.UnityEngine.DependencyInjection;
using Rocket.UnityEngine.Scheduling;
using UnityEngine;

namespace Rocket.UnityEngine.Properties
{
    public class DependencyRegistrator : IDependencyRegistrator
    {
        public void Register(IDependencyContainer container, IDependencyResolver resolver)
        {
            GameObject o = new GameObject("Rocket.UnityEngine Task Scheduler");
            Object.DontDestroyOnLoad(o);
            var component = o.AddComponentWithInjection<UnityTaskScheduler>(container);
            container.RegisterInstance<ITaskScheduler>(component);
        }
    }
}