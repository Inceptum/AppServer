using System;
//using System.Reactive.Subjects;
using System.Threading.Tasks;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Model;


namespace Inceptum.AppServer
{
    public interface IHost
    {
        string Name { get; }
        string MachineName { get; }
        Application[] Applications { get; }
        ApplicationInstanceInfo[] Instances { get; }
/*
        Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged { get;  }
*/


        void RediscoverApps();
        void Start();
        void Stop();
        Task StartInstance(string name, bool doDebug);
        Task StopInstance(string name);
        void AddInstance(ApplicationInstanceInfo config);
        void UpdateInstance(ApplicationInstanceInfo config);
        void SetInstanceVersion(string name, Version version);
        void DeleteInstance(string name);

        string ExecuteCommand(string instance, InstanceCommand command);
    }
}
