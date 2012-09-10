using System;
using System.Collections.Generic;
using System.Linq;

namespace Inceptum.AppServer.Hosting
{
    class ApplicationInfo
    {
        public string Name { get; set; }
        public string Vendor { get; set; }
    }

    public class Application
    {
        private readonly Dictionary<Version, HostedAppInfo> m_Versions = new Dictionary<Version, HostedAppInfo>();
        public string Name { get; set; }
        public string Vendor { get; set; }
        private IApplicationHost m_ApplicationHost;

        public void Start()
        {
         //   m_ApplicationHost=ApplicationHost.Create(m_Versions.OrderByDescending(p => p.Key).Select(p => p.Value).First());            
          //  m_ApplicationHost.Start();
        }

        public void Stop()
        {
            m_ApplicationHost.Stop();
        }
    }
}