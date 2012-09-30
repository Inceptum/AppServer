using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Castle.Core.Logging;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Windsor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Hosting
{
    class Host:IHost,IDisposable
    {
        private ILogger Logger { get; set; }
        public string Name { get; set; }
        readonly List<Application> m_Applications=new List<Application>();
        readonly List<ApplicationInstance> m_Instances = new List<ApplicationInstance>();
        private readonly IApplicationBrowser m_ApplicationBrowser;
        private readonly IApplicationInstanceFactory m_InstanceFactory;
        private readonly AppServerContext m_Context;
        private readonly IEnumerable<IHostNotificationListener> m_Listeners;
        private IManageableConfigurationProvider m_ConfigurationProvider;

        public Host(IManageableConfigurationProvider configurationProvider,IApplicationBrowser applicationBrowser, IApplicationInstanceFactory instanceFactory, IEnumerable<IHostNotificationListener> listeners, ILogger logger = null, string name = null)
        {
            m_ConfigurationProvider = configurationProvider;
            m_Listeners = listeners;
            m_InstanceFactory = instanceFactory;
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

        public Application[] Applications
        {
            get {
                lock (m_Applications)
                {
                    return m_Applications.ToArray();
                }
            }
        }

        public ApplicationInstanceInfo[] Instances
        {
            get
            {
                return m_Instances.Select(i => new ApplicationInstanceInfo
                                            {
                                                Name = i.Name,
                                                ApplicationId = i.ApplicationId,
                                                Status = i.Status,
                                                Version = i.Version
                                            }).OrderByDescending(i=>i.Version).ToArray();
            }
        }


        public void Start()
        {
            RediscoverApps();
            updateInstancesConfiguration();
            foreach (var instance in m_Instances)
            {
                StartInstance(instance.Name);
            }
        }

        public void RediscoverApps()
        {
            var appInfos = m_ApplicationBrowser.GetAvailabelApps();
            foreach (var appInfo in appInfos)
            {
                Application application;
                lock (m_Applications)
                {
                    application = m_Applications.FirstOrDefault(a=>a.Name==appInfo.Name);
                    if (application==null)
                    {
                        application = new Application(appInfo.Name,appInfo.Vendor);
                        m_Applications.Add(application);
                    }
                }

                lock(application)
                    application.RegisterOrUpdateVersion(appInfo);
            }
        }

        private  void updateInstancesConfiguration()
        {
            var bundle = m_ConfigurationProvider.GetBundle("AppServer", "instances");
            var infos = JsonConvert.DeserializeObject<ApplicationInstanceInfo[]>(bundle);
            lock (m_Instances)
            {
                foreach (var info in infos)
                {
                    var instance = m_Instances.FirstOrDefault(i => i.Name == info.Name);
                    if(instance==null)
                    {
                        createInstance(info);
                    }
                }
            }
        }


        public void AddInstance(ApplicationInstanceInfo config)
        {
            var instances = JsonConvert.SerializeObject(Instances.Concat(new []{config}).ToArray(),Formatting.Indented);
            m_ConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
            updateInstancesConfiguration();
        }        
        
        public void DeleteInstance(string name)
        {
            lock (m_Instances)
            {
                var instance = m_Instances.FirstOrDefault(i => i.Name == name);
                if(instance==null)
                    return;
                StopInstance(name);
                m_Instances.Remove(instance);
                //TODO: cleanup instance folder
                var instances = JsonConvert.SerializeObject(Instances.Where(i=>i.Name!=name).ToArray(),Formatting.Indented);
                m_ConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
                updateInstancesConfiguration();
            }
        
        }

        public void createInstance(ApplicationInstanceInfo config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrWhiteSpace(config.Name)) throw new ArgumentException("Can not create instance with empty name", "config");
            lock(m_Instances)
            {
                if(m_Instances.Any(i => i.Name == config.Name))
                    throw new ConfigurationErrorsException(string.Format("Instance named '{0}' already exists",config.Name));
                ApplicationParams applicationParams;
                lock (m_Applications)
                {
                    var application = m_Applications.FirstOrDefault(a => a.Name == config.ApplicationId && a.Versions.Select(v => v.Version).Contains(config.Version));
                    if(application==null)
                        throw new ConfigurationErrorsException(string.Format("Can not create instance for application {0} v{1}. There is no such application or it does not have specified version",config.ApplicationId,config.Name));
                    applicationParams = application.GetLoadParams(config.Version);
                }

                var instance = m_InstanceFactory.Create(config.ApplicationId, config.Name, config.Version, applicationParams, m_Context);
                m_Instances.Add(instance);
                notefyInstancesChanged();
                //TODO: unsubscribe
                instance.Subscribe(status => notefyInstancesChanged(instance.Name + ":" + instance.Status));
            }
        }



        public void StopInstance(string name)
        {
            try
            {
                ApplicationInstance instance;
                lock (m_Instances)
                {
                    instance = m_Instances.FirstOrDefault(i => i.Name == name);
                }

                if (instance == null)
                    throw new ConfigurationErrorsException(string.Format("Instance '{0}' not found", name));
                
                instance.Stop();

            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to stop instance {0}",name);                
                throw;
            }
        }

        public void StartInstance(string name)
        {
            try
            {
                ApplicationInstance instance;
                lock (m_Instances)
                {
                    instance = m_Instances.FirstOrDefault(i => i.Name == name);
                }

                if (instance == null)
                    throw new ConfigurationErrorsException(string.Format("Instance '{0}' not found",name));

                instance.Start();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to start instance {0}",name);                
                throw;
            }
        }

        private void notefyInstancesChanged(string comment=null)
        {
            foreach (var listener in m_Listeners)
            {
                try
                {
                    listener.InstancesChanged(comment);
                }catch(Exception e)
                {
                    Logger.WarnFormat(e, "Failed to deliver instances changed notification");
                }
            }
        }

    
        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged
        {
            get {return new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>();}
        }


        public void Dispose()
        {
            Logger.InfoFormat("Host is disposed");
      /*      foreach (var instance in m_Instances)
            {
                m_InstanceFactory.Release(instance);
            }*/
        }
    }
}