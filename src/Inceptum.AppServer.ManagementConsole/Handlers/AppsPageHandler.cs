﻿using System.Linq;
using Inceptum.AppServer.Management.Resources;

namespace Inceptum.AppServer.Management.Handlers
{
    public class AppsPageHandler
    {
        private readonly IHost m_Host;

        public AppsPageHandler(IHost host)
        {
            m_Host = host;
        }


        public void Post(string app)
        {
            if (m_Host.HostedApps.Any(a => a.Name == app))
                m_Host.StopApps(app);
            else
                m_Host.StartApps(app);

        }


        public AppsPage Get()
        {
            return new AppsPage
                       {
                           Hosts = new[]
                                       {
                                           new HostInfo
                                               {
                                                   Name = m_Host.Name,
                                                   MachineName = m_Host.MachineName,
                                                   Apps = 
                                                        m_Host.DiscoveredApps.Select(a => new AppInfo{Name=a.Name,IsStarted = m_Host.HostedApps.Contains(a)}).
                                                        Concat(new []
                                                                   {
                                                                       new AppInfo{Name="Fake inactive application",IsStarted = false},
                                                                       new AppInfo{Name="Fake active application",IsStarted = true}
                                                                   }).
                                                        ToArray()
                                               }
                                       }
                       };
        }
    }
}