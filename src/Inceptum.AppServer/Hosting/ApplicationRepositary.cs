using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
    public class ApplicationRepositary
    {
        private readonly IEnumerable<IApplicationBrowser> m_ApplicationBrowsers;
        private readonly object m_SyncRoot=new object();
        private List<Application> m_Applications;
        private ILogger Logger { get; set; }

        public ApplicationRepositary(IEnumerable<IApplicationBrowser> browsers, ILogger logger = null)
        {
            Logger = logger;
            m_ApplicationBrowsers = browsers;
        }

        public void RediscoverApps()
        {
            Logger.InfoFormat("Applications discovery");

            var appInfos = m_ApplicationBrowsers.SelectMany(x=>x.GetAvailabelApps());
            var applications = new List<Application>(
                                                    from info in appInfos
                                                    group info by new { vendor = info.Vendor, name = info.Name }
                                                        into app
                                                        select new Application(app.Key.name, app.Key.vendor, app)
                                                    );
            var apps = String.Join(
                            Environment.NewLine+"\t",  
                            applications.Select(a =>a.ToString()).ToArray()
                                  );
            lock (m_SyncRoot)
            {
                m_Applications = applications;
            }
            Logger.InfoFormat("Discovered applications: {0}\t{1}", Environment.NewLine, apps);
        }

        public Application[] Applications
        {
            get
            {
                lock(m_SyncRoot)
                    return m_Applications.ToArray();
            }
        }
    }
}