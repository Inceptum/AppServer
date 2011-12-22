using System;
using Castle.Core;
using Castle.Core.Logging;
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

    public class ManagementConsole : IDisposable , IStartable
    {
        private HttpListenerHostWithConfiguration m_OpenRastaHost;
        private readonly ILogger m_Logger;
        private readonly IWindsorContainer m_Container;
        private readonly string m_Uri = "http://+:9222/";
        private readonly bool m_Enabled;

        public ManagementConsole(IWindsorContainer container, ILogger logger, int port=9222, bool enabled=true)
        {
            m_Enabled = enabled;

            m_Logger = logger;
            m_Uri = string.Format("http://+:{0}/", port);
            m_Container = container;

        }


        #region IDisposable Members

        public void Dispose()
        {
            if (!m_Enabled) return; 
            m_OpenRastaHost.Close();
        }

        #endregion

        public void Start()
        {
            if (!m_Enabled) return;
            m_Logger.Info("Starting management console.");
            m_Container.Register(
                Component.For<IConfigurationSource>().ImplementedBy<Configurator>()
                );
            Accessor.SetResolver(new WindsorDependencyResolver(m_Container));
            m_OpenRastaHost = new HttpListenerHostWithConfiguration(new Configurator());

            m_OpenRastaHost.Initialize(new[] {m_Uri}, "/", typeof (Accessor));
            m_OpenRastaHost.StartListening();
            m_Logger.InfoFormat("Management console is started and listening on {0}.", m_Uri);
        }

        public void Stop()
        {
        }
    }
}