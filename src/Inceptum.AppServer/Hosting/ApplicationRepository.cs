using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.NuGetAppInstaller;

namespace Inceptum.AppServer.Hosting
{
    public class ApplicationRepository
    {
        private readonly IEnumerable<IApplicationRepository> m_ApplicationRepositories;
        private readonly object m_SyncRoot=new object();
        private List<Application> m_Applications;
        private ILogger Logger { get; set; }

        public ApplicationRepository(IEnumerable<IApplicationRepository> repositories, ILogger logger = null)
        {
            Logger = logger;
            m_ApplicationRepositories = repositories;
        }

        public void RediscoverApps()
        {
            Logger.InfoFormat("Applications discovery");

            var appInfos = m_ApplicationRepositories.SelectMany(x =>
                {
                    Logger.InfoFormat("Loading apps from {0}", x.Name);
                    return x.GetAvailableApps().Select(a=>Tuple.Create(a,x.Name));
                });
            /*  var appInfos = m_ApplicationRepositories.SelectMany(x =>
                {
                    Logger.InfoFormat("Loading apps from {0}", x.Name);
                    return x.GetAvailableApps();
                });*/
            var applications = new List<Application>(
                                                    from info in appInfos
                                                    group info by new { vendor = info.Item1.Vendor, name = info.Item1.ApplicationId, repo=info.Item2 }
                                                        into app
                                                        select new Application(app.Key.repo, app.Key.name, app.Key.vendor, app.ToDictionary(a => a.Item1.Version, a => a.Item1.Description))
                                                    );
            var apps = String.Join(
                            Environment.NewLine + "\t",
                            applications.Select(a => a.ToString()).ToArray()
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
                lock (m_SyncRoot)
                    return m_Applications.OrderBy(a => Tuple.Create(a.Vendor, a.Name)).ToArray();
            }
        }
        
        /*
    public class ApplicationRepository
    {
        private readonly IEnumerable<IApplicationBrowser> m_ApplicationRepositories;
        private readonly object m_SyncRoot=new object();
        private List<Application> m_Applications;
        private ILogger Logger { get; set; }

        public ApplicationRepository(IEnumerable<IApplicationBrowser> repositories, ILogger logger = null)
        {
            Logger = logger;
            m_ApplicationRepositories = repositories;
        }

        public void RediscoverApps()
        {
            Logger.InfoFormat("Applications discovery");

            var appInfos = m_ApplicationRepositories.SelectMany(x =>
                {
                    Logger.InfoFormat("Loading apps from {0}",x.Name);
                    return x.GetAvailableApps().Select(i =>
                        {
                            i.Browser = x.Name;
                            return i;
                        });
                });
            var applications = new List<Application>(
                                                    from info in appInfos
                                                    group info by new { vendor = info.Vendor, name = info.Name }
                                                        into app
                                                        select new Application(app.Key.name, app.Key.vendor, app)
                                                    );
            var apps = String.Join(
                            Environment.NewLine + "\t",
                            applications.Select(a => a.ToString()).ToArray()
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
                    return m_Applications.OrderBy(a=>Tuple.Create(a.Vendor,a.Name)).ToArray();
            }
        }

        public void EnsureLoadParams(string application, Version version)
        {
            var app = Applications.FirstOrDefault(a => a.Name == application);
            if(app==null)
                throw new InvalidOperationException(string.Format("Application {0}  not found",application));
            app.EnsureLoadParams(version, browser => m_ApplicationRepositories.FirstOrDefault(x => x.Name == browser).GetApplicationParams(application, version));
        }
*/

        public void Install(Application application,Version version, string path)
        {
            var repository = m_ApplicationRepositories.Single(r=>r.Name==application.Repository);
            repository.Install(path, new ApplicationInfo{ApplicationId = application.Name,Vendor = application.Vendor,Version = version});
        }
    }
}