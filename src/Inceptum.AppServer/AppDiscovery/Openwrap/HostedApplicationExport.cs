using System;
using OpenWrap.PackageModel;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    class HostedApplicationExport : IHostedApplicationExport
    {
        public HostedApplicationExport(IPackage package, string name, string vendor, Version version, string type)
        {
            Package = package;
            Name = name;
            Version = version;
            Type = type;
            Vendor = vendor;
        }

        public string Path { get { return null; } }
        public IPackage Package{get; private set; }
        public string Name { get; private set; }
        public Version Version { get; private set; }
        public string Type { get; private set; }
        public string Vendor { get; private set; }
    }
}
