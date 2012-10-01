using System.Collections.Generic;
using System.Linq;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
    public class ApplicationsRepository
    {
        private readonly IApplicationBrowser m_ApplicationBrowser;
        readonly List<Application> m_Applications = new List<Application>();


        public ApplicationsRepository(IApplicationBrowser applicationBrowser)
        {
            m_ApplicationBrowser = applicationBrowser;
        }

        public void RediscoverApps()
        {
            var appInfos = m_ApplicationBrowser.GetAvailabelApps();
            foreach (var appInfo in appInfos)
            {
                Application application;
                lock (m_Applications)
                {
                    application = m_Applications.FirstOrDefault(a => a.Name == appInfo.Name);
                    if (application == null)
                    {
                        application = new Application(appInfo.Name, appInfo.Vendor);
                        m_Applications.Add(application);
                    }
                }

                lock (application)
                    application.RegisterOrUpdateVersion(appInfo);
            }
        }
    }
}