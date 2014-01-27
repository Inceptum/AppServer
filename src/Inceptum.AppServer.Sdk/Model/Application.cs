using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Model
{
    public class Application : IComparer<ApplicationVersion>
    {
        private readonly List<ApplicationVersion> m_Versions;
        public bool Debug { get; set; }
        public string Name { get; private set; }
        public string Vendor { get; private set; }
        public string Repository { get; private set; }

        public Application(string repository,string id, string vendor, Dictionary<Version, string> versions)
        {
            Repository = repository;
            Name = id;
            Vendor = vendor;
            m_Versions = new List<ApplicationVersion>();
            foreach (var version in versions)
            {
                var appVersion = m_Versions.FirstOrDefault(v => v.Version == version.Key);
                if (appVersion != null) m_Versions.Remove(appVersion);
                m_Versions.Add(new ApplicationVersion { Description = version.Value, Version = version.Key});
            }
            m_Versions.Sort(this);
        }



        public ApplicationVersion[] Versions
        {
            get { return m_Versions.ToArray(); }
        }

        int IComparer<ApplicationVersion>.Compare(ApplicationVersion x, ApplicationVersion y)
        {
            return -1 * Comparer<Version>.Default.Compare(x.Version, y.Version);
        }

        public override string ToString()
        {
            return string.Format("{0}(c) {1} {2}", Vendor, Name, string.Join(", ",Versions.Select(v=>"v"+v.Version)));
        }
    }
}