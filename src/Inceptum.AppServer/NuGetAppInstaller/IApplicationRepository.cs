using System;
using System.Collections.Generic;

namespace Inceptum.AppServer.NuGetAppInstaller
{
    public class ApplicationInfo
    {
        public string Vendor { get; set; } 
        public string ApplicationId { get; set; }
        public Version Version { get; set; }
    }
    public interface IApplicationRepository
    {
        IEnumerable<ApplicationInfo> GetAvailableApps();
        string Install(string path,ApplicationInfo application);
        string Upgrade(string path,ApplicationInfo application);
        string Name { get;  }
    }
}