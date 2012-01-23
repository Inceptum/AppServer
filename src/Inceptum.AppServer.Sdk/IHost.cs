using System;
using System.Reactive.Subjects;


namespace Inceptum.AppServer
{
    public interface IHost
    {
        string Name { get; }
        string MachineName { get; }
        AppInfo[] DiscoveredApps { get; }
        Tuple<HostedAppInfo, HostedAppStatus>[] HostedApps { get; }
        void RediscoverApps();
        void StartApps(params string[] appsToStart);
        void StopApps(params string[] appsToStart);
        Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged { get;  }
    }
}
