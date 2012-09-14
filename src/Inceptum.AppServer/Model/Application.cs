using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Inceptum.AppServer.Model
{
    public class Application : IComparer<ApplicationVersion>
    {
        private readonly SortedDictionary<ApplicationVersion, ApplicationParams> m_Versions = new SortedDictionary<ApplicationVersion, ApplicationParams>();
        public ApplicationName Name { get; set; }
        public Version VersionToUse { get; set; }

        public Application(ApplicationName name)
        {
            Name = name;
        }

        public ApplicationVersion[] Versions
        {
            get { return m_Versions.Keys.ToArray(); }
        }

        int IComparer<ApplicationVersion>.Compare(ApplicationVersion x, ApplicationVersion y)
        {
            return Comparer<Version>.Default.Compare(x.Version, y.Version);
        }

        public ApplicationParams GetLoadParams(Version version)
        {
            ApplicationParams loadParams = m_Versions.Where(v => v.Key.Version == version).Select(p => p.Value).FirstOrDefault();
            if(loadParams==null)
                    throw new ConfigurationErrorsException(string.Format("Can not find version {0} of application {1}",version,Name));
            return loadParams;
        }


        internal void RegisterOrUpdateVersion(HostedAppInfo appInfo)
        {
            var appVersion = m_Versions.Where(v => v.Key.Version == appInfo.Version).Select(p => p.Key).FirstOrDefault();
            if (appVersion != null) m_Versions.Remove(appVersion);
            m_Versions.Add(
                    new ApplicationVersion{Description = "",Version = appInfo.Version},
                    new ApplicationParams(appInfo.AppType,appInfo.ConfigFile, appInfo.NativeDllToLoad,appInfo.AssembliesToLoad)
                    );
        }
    }
}