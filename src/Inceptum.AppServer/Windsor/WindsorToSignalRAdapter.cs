using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using SignalR;

namespace Inceptum.AppServer.Windsor
{
    /// <summary>
    /// An adapter to make SignalR resolve its components from Castle
    /// </summary>
    public class WindsorToSignalRAdapter : DefaultDependencyResolver
    {
        private readonly IWindsorContainer m_Container;

        public WindsorToSignalRAdapter(IWindsorContainer container)
        {
            m_Container = container;
        }

        public override object GetService(Type serviceType)
        {
            if (m_Container.Kernel.HasComponent(serviceType)) return m_Container.Resolve(serviceType);

            return base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            if (m_Container.Kernel.HasComponent(serviceType)) return m_Container.ResolveAll(serviceType).Cast<object>();

            return base.GetServices(serviceType);
        }
    }

}