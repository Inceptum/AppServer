namespace Inceptum.AppServer
{
    public interface IHost
    {
        string Name { get; }
        string MachineName { get; }
        AppInfo[] DiscoveredApps { get; }
        HostedAppInfo[] HostedApps { get; }
        void RediscoverApps();
        void StartApps(params string[] appsToStart);
        void StopApps(params string[] appsToStart);
    }
}