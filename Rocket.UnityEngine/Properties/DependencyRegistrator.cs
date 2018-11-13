using Rocket.API.DependencyInjection;
using Rocket.API.Scheduling;
using Rocket.UnityEngine.Scheduling;
using UnityEngine;

namespace Rocket.UnityEngine.Properties
{
    public class DependencyRegistrator : IDependencyRegistrator
    {
        public void Register(IDependencyContainer container, IDependencyResolver resolver)
        {
            GameObject o = new GameObject("Rocket.UnityEngine Task Runner");
            Object.DontDestroyOnLoad(o);
            var component = o.AddComponent<UnityTaskScheduler>();
            component.Load(container);
            container.RegisterInstance<ITaskScheduler>(component);
        }
    }
}