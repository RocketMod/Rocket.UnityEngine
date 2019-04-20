using Rocket.API.DependencyInjection;
using Rocket.UnityEngine.DependencyInjection;
using Rocket.UnityEngine.Scheduling;
using UnityEngine;

namespace Rocket.UnityEngine.Properties
{
    public class ServiceConfigurator : IServiceConfigurator
    {
        public void ConfigureServices(IDependencyContainer container)
        {
            GameObject o = new GameObject("Rocket.UnityEngine Task Scheduler");
            Object.DontDestroyOnLoad(o);
            o.AddComponentWithInjection<UnityTaskRunnerComponent>(container);
        }
    }
}