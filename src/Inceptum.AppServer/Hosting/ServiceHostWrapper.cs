using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;
using Castle.Core.Logging;
using Inceptum.AppServer.Utils;

namespace Inceptum.AppServer.Hosting
{
    public class ServiceHostWrapper<TService>:IDisposable
    {
        private readonly ILogger m_Logger;
        private ServiceHost m_ServiceHost;
        private readonly object m_SyncRoot=new object();
        private readonly TService m_Instance;
        private readonly string m_Name;

        public ServiceHostWrapper(ILogger logger, TService instance, string name)
        {
            m_Instance = instance;
            m_Name = name;
            m_Logger = logger;
            reset();
        }

        private void reset()
        {
            lock (m_SyncRoot)
            {
                if (m_ServiceHost != null)
                {
                    m_ServiceHost.Close();
                    m_ServiceHost = null;
                }
                var serviceHost = createServiceHost(m_Instance);
                serviceHost.AddServiceEndpoint(typeof(TService), WcfHelper.CreateUnlimitedQuotaNamedPipeLineBinding(), m_Name);

                EventHandler faulted = null;
                faulted = (o, args) =>
                {
                    m_Logger.InfoFormat("Recreating {0} service host.", m_Name);
                    serviceHost.Faulted -= faulted;
                    reset();
                };
                serviceHost.Faulted += faulted;

                serviceHost.Open();
                m_ServiceHost = serviceHost;
            }
        }

        private ServiceHost createServiceHost(object serviceInstance)
        {
            var serviceHost = new ServiceHost(serviceInstance, new[] { new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id) });
            var debug = serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (debug == null)
                serviceHost.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
            else
                debug.IncludeExceptionDetailInFaults = true;
            return serviceHost;
        }

        public void Dispose()
        {
            lock (m_SyncRoot)
            {
                if (m_ServiceHost != null)
                {
                    m_ServiceHost.Close();
                    m_ServiceHost = null;
                }
            }
        }
    }
}