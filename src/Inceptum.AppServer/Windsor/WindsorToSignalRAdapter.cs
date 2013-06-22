using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Castle.Windsor;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Inceptum.AppServer.Windsor
{
    /// <summary>
    /// An adapter to make SignalR resolve its components from Castle
    /// </summary>
    public class WindsorToSignalRAdapter : DefaultDependencyResolver
    {
        private readonly IKernel m_Kernel;

        public WindsorToSignalRAdapter(IKernel kernel)
        {
            m_Kernel = kernel;
        }

        public override object GetService(Type serviceType)
        {
            if (m_Kernel.HasComponent(serviceType)) return m_Kernel.Resolve(serviceType);

            return base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            if (m_Kernel.HasComponent(serviceType)) return m_Kernel.ResolveAll(serviceType).Cast<object>();

            var enumerable = base.GetServices(serviceType);
            return enumerable;
        }

        public override void Register(Type serviceType, Func<object> activator)
        {
            base.Register(serviceType, activator);
        }

        public override void Register(Type serviceType, IEnumerable<Func<object>> activators)
        {
            base.Register(serviceType, activators);
        }

    }

}