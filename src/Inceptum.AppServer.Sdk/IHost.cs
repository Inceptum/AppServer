using System;
using System.Reactive.Subjects;
using Inceptum.AppServer.Model;


namespace Inceptum.AppServer
{
    public interface IHost
    {
        string Name { get; }
        string MachineName { get; }
        Application[] Applications { get; }
        ApplicationInstanceInfo[] Instances { get; }
        Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged { get;  }


        void RediscoverApps();
        void Start();
        void StartInstance(string name);
        void StopInstance(string name);
        void AddInstance(ApplicationInstanceInfo config);
        void UpdateInstance(ApplicationInstanceInfo config);
        void DeleteInstance(string name);

        string ExecuteCommand(string instance, string command);
    }
}
