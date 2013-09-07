using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Model
{
    public class Application : IComparer<ApplicationVersion>
    {
        private readonly SortedDictionary<ApplicationVersion, ApplicationParams> m_Versions;
        public string Name { get; set; }
        public string Vendor { get; set; }

        public Application(string id,  string vendor)
        {
            Name = id;
            Vendor = vendor;
            m_Versions = new SortedDictionary<ApplicationVersion, ApplicationParams>(this);
        }

        public Application(string id, string vendor, IEnumerable<HostedAppInfo> versions)
        {
            Name = id;
            Vendor = vendor;
            m_Versions = new SortedDictionary<ApplicationVersion, ApplicationParams>(this);
            foreach (var appInfo in versions)
            {
                var appVersion = m_Versions.Where(v => v.Key.Version == appInfo.Version).Select(p => p.Key).FirstOrDefault();
                if (appVersion != null) m_Versions.Remove(appVersion);
                m_Versions.Add(
                        new ApplicationVersion { Description = appInfo.Description, Version = appInfo.Version,Browser=appInfo.Browser },
                        appInfo.AssembliesToLoad==null
                                ?null
                                :new ApplicationParams(appInfo.AppType, appInfo.ConfigFile,  appInfo.NLogConfigFile, appInfo.NativeDllToLoad, appInfo.AssembliesToLoad)
                                {
                                    Debug = appInfo.Debug
                                }
                        );
            }
        }


        public ApplicationVersion[] Versions
        {
            get { return m_Versions.Keys.ToArray(); }
        }

        int IComparer<ApplicationVersion>.Compare(ApplicationVersion x, ApplicationVersion y)
        {
            return -1 * Comparer<Version>.Default.Compare(x.Version, y.Version);
        }

        public ApplicationParams GetLoadParams(Version version)
        {
            ApplicationParams loadParams = m_Versions.Where(v => v.Key.Version == version).Select(p => p.Value).FirstOrDefault();
            return loadParams;
        }
        
        public ApplicationParams EnsureLoadParams(Version version,Func<string,ApplicationParams> loader )
        {
            if (m_Versions.All(v => v.Key.Version != version))
                return null;

            ApplicationParams applicationParams = m_Versions.First(v => v.Key.Version == version).Value;
            ApplicationVersion applicationVersion = m_Versions.First(v => v.Key.Version == version).Key;
            if (applicationParams == null)
            {
                applicationParams = loader(applicationVersion.Browser);
                m_Versions[applicationVersion] = applicationParams;
            }
            return applicationParams;
 
        }

        public override string ToString()
        {
            return string.Format("{0}(c) {1} {2}", Vendor, Name, string.Join(", ",Versions.Select(v=>"v"+v.Version)));
        }
    }
}