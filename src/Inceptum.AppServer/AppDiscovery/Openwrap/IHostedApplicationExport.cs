using System;
using OpenWrap.PackageManagement;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    public interface IHostedApplicationExport:IExportItem
    {
        string Type { get; }
        string Name { get; }
        string Vendor { get; }
        Version Version { get; }
    }
}
