using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Management.Windsor;
using OpenRasta.Configuration;
using OpenRasta.DI;

namespace Inceptum.AppServer.Management
{
    internal class Accessor : IDependencyResolverAccessor
    {
        private static IDependencyResolver m_InternalDependencyResolver;
        internal static  void SetResolver(IDependencyResolver resolver)
        {
            m_InternalDependencyResolver = resolver;
        }


        #region IDependencyResolverAccessor Members

        public IDependencyResolver Resolver
        {
            get
            {
                return m_InternalDependencyResolver;
            }
        }

        #endregion
    }

    public class ManagementConsole : IDisposable 
    {
        private readonly HttpListenerHostWithConfiguration m_OpenRastaHost;

        public ManagementConsole(WindsorContainer container)
        {
            var openRastaContainer = new WindsorContainer();
            openRastaContainer.Register(
                Component.For<IConfigurationSource>().ImplementedBy<Configurator>()
                );
            container.AddChildContainer(openRastaContainer);
            Accessor.SetResolver(new WindsorDependencyResolver(openRastaContainer));
            m_OpenRastaHost = new HttpListenerHostWithConfiguration(new Configurator());
            m_OpenRastaHost.Initialize(new[] {"http://+:9222/"}, "/", typeof (Accessor));
            m_OpenRastaHost.StartListening();
        }


        #region IDisposable Members

        public void Dispose()
        {
            m_OpenRastaHost.Close();
        }

        #endregion
    }
}