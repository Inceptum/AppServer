using System;

namespace Inceptum.AppServer.Model
{
    public class HostedApp
    {
        public string Name { get; set; }
        public string Vendor { get; set; }
        public Version[] Versions { get; set; }
        public Version VersionToUse { get; set; }
        public HostedAppStatus Status { get; set; }
    }
}