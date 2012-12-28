using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Castle.Windsor;
using SignalR;

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

            return base.GetServices(serviceType);
        }


    }

}