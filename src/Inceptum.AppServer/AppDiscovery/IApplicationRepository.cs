using System.Collections.Generic;

namespace Inceptum.AppServer.AppDiscovery
{
    public interface IApplicationRepository
    {
        string Name { get; }
        IEnumerable<ApplicationInfo> GetAvailableApps();
        void Install(string path, ApplicationInfo application);
        void Upgrade(string path, ApplicationInfo application);
    }
}