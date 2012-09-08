using System;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Hosting
{
    internal class ApplicationHostProxy : IApplicationHost
    {
        private readonly IApplicationHost m_ApplicationHost;
        private readonly AppDomain m_Domain;


        public ApplicationHostProxy(IApplicationHost applicationHost, AppDomain domain)
        {
            if (applicationHost == null) throw new ArgumentNullException("applicationHost");
            if (domain == null) throw new ArgumentNullException("domain");
            m_ApplicationHost = applicationHost;
            m_Domain = domain;
        }

        #region IApplicationHost Members

        public HostedAppStatus Status
        {
            get { return m_ApplicationHost.Status; }
        }

        public HostedAppInfo AppInfo
        {
            get { return m_ApplicationHost.AppInfo; }
        }

        public void Start(IConfigurationProvider configurationProvider, AppServerContext context)
        {
            m_ApplicationHost.Start(configurationProvider, context);
        }

        public void Stop()
        {
            m_ApplicationHost.Stop();
            AppDomain.Unload(m_Domain);
        }

        #endregion
    }
}