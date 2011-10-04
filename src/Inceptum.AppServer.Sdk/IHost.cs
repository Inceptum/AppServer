namespace Inceptum.AppServer
{
    public interface IHost
    {
        string Name { get; }
        string MachineName { get; }
        HostedAppInfo[] DiscoveredApps { get; }
        HostedAppInfo[] HostedApps { get; }
        void LoadApps();
        void StartApps(params string[] appsToStart);
        void StopApps(params string[] appsToStart);
    }
}