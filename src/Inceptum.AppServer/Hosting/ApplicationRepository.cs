using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.Model;

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


            var applications = new List<Application>(
                                                    from info in appInfos
                                                    group info by new { vendor = info.Item1.Vendor, name = info.Item1.ApplicationId, repo=info.Item2 ,debug=info.Item1.Debug}
                                                        into app
                                                        select new Application(app.Key.repo, app.Key.name, app.Key.vendor, app.ToDictionary(a => a.Item1.Version, a => a.Item1.Description))
                                                        {
                                                            Debug = app.Key.debug
                                                        } 
                                                    );

            var cleaned = (from app in applications
                group app by new {app.Vendor,app.Name}
                into a
                select a.FirstOrDefault(a1=>a1.Debug)??a.First()).ToList();
            
            var apps = String.Join(
                            Environment.NewLine + "\t",
                            cleaned.Select(a => a.ToString()).ToArray()
                            );
            
            lock (m_SyncRoot)
            {
                m_Applications = cleaned;
            }
            Logger.InfoFormat("Discovered applications: {0}\t{1}", Environment.NewLine, apps);
        }

        public Application[] Applications
        {
            get
            {
                lock (m_SyncRoot)
                    return m_Applications.OrderBy(x => x.Vendor).ThenBy(x => x.Name).ToArray();
            }
        }
   
        public void Install(Application application,Version version, string path)
        {
            var repository = m_ApplicationRepositories.Single(r=>r.Name==application.Repository);
            lock (repository)
            {
                repository.Install(path, new ApplicationInfo { ApplicationId = application.Name, Vendor = application.Vendor, Version = version });
            }
        }

        public void Upgrade(Application application, Version version, string path)
        {
            var repository = m_ApplicationRepositories.Single(r => r.Name == application.Repository);
            lock (repository)
            {
                repository.Upgrade(path, new ApplicationInfo {ApplicationId = application.Name, Vendor = application.Vendor, Version = version});
            }
        }
    }
}