using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Castle.Core.Logging;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Hosting
{
    class Host:IHost
    {
        private ILogger Logger { get; set; }
        public string Name { get; set; }
        readonly List<Application> m_Applications=new List<Application>();
        readonly List<ApplicationInstance> m_Instances = new List<ApplicationInstance>();
        private readonly IApplicationBrowser m_ApplicationBrowser;
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly IApplicationInstanceFactory m_InstanceFactory;
        private readonly AppServerContext m_Context;

        public Host(IApplicationBrowser applicationBrowser,IApplicationInstanceFactory instanceFactory, ILogger logger = null, IConfigurationProvider configurationProvider = null, string name = null)
        {
            m_InstanceFactory = instanceFactory;
            m_ConfigurationProvider = configurationProvider;
            Logger = logger;
            Name = name??Environment.MachineName;;
            m_ApplicationBrowser = applicationBrowser;
            m_Context = new AppServerContext
            {
                Name = Name,
                AppsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "apps"),
                BaseDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
        }


     
        public string MachineName
        {
            get { return Environment.MachineName; }
        }

        public AppInfo[] DiscoveredApps
        {
            get {
                lock (m_Applications)
                {
                    //TODO: version selection is fake
                    return m_Applications.Select(a => new AppInfo(a.Name.Name,new Version(0,0))).ToArray();
                }
            }
        }

        public Tuple<HostedAppInfo, HostedAppStatus>[] HostedApps
        {
            get { throw new NotImplementedException(); }
        }

        public void RediscoverApps()
        {
            var appInfos = m_ApplicationBrowser.GetAvailabelApps();
            foreach (var appInfo in appInfos)
            {
                Application application;
                var appName = new ApplicationName(appInfo.Name, appInfo.Vendor);
                lock (m_Applications)
                {
                    application = m_Applications.FirstOrDefault(a=>a.Name==appName);
                    if (application==null)
                    {
                        application = new Application(appName);
                        m_Applications.Add(application);
                    }
                }

                lock(application)
                    application.RegisterOrUpdateVersion(appInfo);
            }
        }

        public void StartApps(params string[] appsToStart)
        {
            Application[] apps;
            lock(m_Applications)
            {
                apps = m_Applications.Where(app => appsToStart.Contains(app.Name.Name)).ToArray();
            }

            foreach (var app in apps)
            {
                ApplicationParams applicationParams;
                Version version;
                lock (app)
                {
                    version = app.Versions.First().Version;
                    applicationParams = app.GetLoadParams(version);
                }
                    
                ApplicationInstance instance = m_InstanceFactory.Create(app.Name, "default", version, applicationParams, m_Context);
                lock (m_Instances)
                {
                    m_Instances.Add(instance);
                }
                instance.Start();
            }
        }

        public void StopApps(params string[] appsToStart)
        {
            lock (m_Instances)
            {
                foreach (var instance in m_Instances)
                {
                    instance.Stop();
                }
            }

            foreach (var instance in m_Instances)
            {
                instance.Dispose();
            }
        }

        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged
        {
            get {return new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>();}
        }


        
    }
}