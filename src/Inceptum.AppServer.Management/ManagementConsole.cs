using System;
using Castle.Core;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Management.OpenRasta;
using Inceptum.AppServer.Management.Handlers;
using Newtonsoft.Json.Linq;
using OpenRasta.Configuration;
using OpenRasta.DI;
using OpenRasta.Pipeline;

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
        private readonly bool m_Enabled=true;

        public ManagementConsole(IWindsorContainer container, ILogger logger,IManageableConfigurationProvider configurationProvider )
        {
            m_Container = container;
            m_Logger = logger;

            try
            {
                var bundleString = configurationProvider.GetBundle("AppServer", "server.host", "{environment}", "{machineName}");
                dynamic bundle = JObject.Parse(bundleString).SelectToken("ManagementConsole");
                m_Enabled = bundle.enabled;
            }
            catch (Exception e)
            {
                m_Logger.WarnFormat(e,"Failed to get management console enabled value from configuration , using default: true");
            }
            
            int port=9223;
            try
            {
                var bundleString = configurationProvider.GetBundle("AppServer", "server.host", "{environment}", "{machineName}");
                dynamic bundle = JObject.Parse(bundleString).SelectToken("ManagementConsole");
                port = bundle.port;
            }
            catch (Exception e)
            {
                m_Logger.WarnFormat(e,"Failed to get port from configuration , using default 9223");
            }
            m_Uri = string.Format("http://*:{0}/", port);
            

        }

        public ManagementConsole(IWindsorContainer container, ILogger logger, int port=9222, bool enabled=true)
        {
            m_Enabled = enabled;

            m_Logger = logger;
            m_Uri = string.Format("http://*:{0}/", port);
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
                Component.For<IConfigurationSource>().ImplementedBy<Configurator>(),
                Classes.FromThisAssembly().BasedOn<IPipelineContributor>().WithServiceSelf().LifestyleTransient(),
                Component.For<ErrorHandlingOperationInterceptor>().LifestyleTransient()
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