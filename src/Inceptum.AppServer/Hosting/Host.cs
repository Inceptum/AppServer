using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Windsor;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Hosting
{
    class Host:IHost,IDisposable
    {
        private ILogger Logger { get; set; }
        public string Name { get; set; }
        private List<Application> m_Applications = new List<Application>();
        private readonly List<ApplicationInstance> m_Instances = new List<ApplicationInstance>();
        private InstanceConfig[] m_InstancesConfiguration =new InstanceConfig[0];

        private readonly IApplicationBrowser m_ApplicationBrowser;
        private readonly IApplicationInstanceFactory m_InstanceFactory;
        private readonly AppServerContext m_Context;
        private readonly IEnumerable<IHostNotificationListener> m_Listeners;
        private readonly IManageableConfigurationProvider m_ConfigurationProvider;
        private readonly object m_SyncRoot = new object();

        public Host(IManageableConfigurationProvider configurationProvider,IApplicationBrowser applicationBrowser, IApplicationInstanceFactory instanceFactory, IEnumerable<IHostNotificationListener> listeners, ILogger logger = null, string name = null)
        {
            m_ConfigurationProvider = configurationProvider;
            m_Listeners = listeners;
            m_InstanceFactory = instanceFactory;
            Logger = logger;
            Name = name??Environment.MachineName;
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
                lock (m_SyncRoot)
                {
                    return m_Applications.ToArray();
                }
            }
        }

        public ApplicationInstanceInfo[] Instances
        {
            get
            {
                lock (m_SyncRoot)
                    {
                        return (from cfg in m_InstancesConfiguration
                                join instance in m_Instances on cfg.Name equals instance.Name into t
                                from instance in t
                                select new ApplicationInstanceInfo
                                           {
                                               Name = cfg.Name,
                                               Id = cfg.Name,
                                               ApplicationId = cfg.ApplicationId,
                                               Status = instance.Status,
                                               Version = cfg.Version,
                                               AutoStart = cfg.AutoStart,
                                               ActualVersion=instance.ActualVersion
                                           }).ToArray();
                    }
            }
        }


        public void Start()
        {
            RediscoverApps();
            Logger.InfoFormat("Reading instances config");
            updateInstances();
            IEnumerable<Task> tasks;
            lock (m_SyncRoot)
            {
                tasks = m_InstancesConfiguration.Where(c => c.AutoStart).Select(c => c.Name).Select(instance => Task.Factory.StartNew(() => StartInstance(instance)));
/*
                foreach (var instance in m_InstancesConfiguration.Where(c => c.AutoStart).Select(c => c.Name))
                {
                    StartInstance(instance);
                }
*/
                Logger.InfoFormat("Starting instances");                
            }
            Task.WaitAll(tasks.ToArray());
        }

        public void RediscoverApps()
        {
            Logger.InfoFormat("Applications discovery");

            var appInfos = m_ApplicationBrowser.GetAvailabelApps();
            var applications = new List<Application>(
                                                    from info in appInfos
                                                    group info by new { vendor = info.Vendor, name = info.Name }
                                                        into app
                                                        select new Application(app.Key.name, app.Key.vendor, app)
                                                    );

            lock(m_SyncRoot)
            {
                m_Applications = applications;
            }

        }


        private  void updateInstances()
        {
            var bundle = m_ConfigurationProvider.GetBundle("AppServer", "instances");
            var configs = JsonConvert.DeserializeObject<InstanceConfig[]>(bundle).GroupBy(i=>i.Name).Select(g=>g.First());

            lock (m_SyncRoot)
            {
                m_InstancesConfiguration = configs.ToArray();
                var instanceParams = from config in m_InstancesConfiguration
                                     join app in m_Applications on config.ApplicationId equals app.Name into matchedApp
                                     from application in matchedApp.DefaultIfEmpty()
                                     let version = config.Version ?? application.Versions.Select(v => v.Version).OrderByDescending(v => v).FirstOrDefault()
                                     join instance in m_Instances on config.Name equals instance.Name into matchedInstance
                                     from instance in matchedInstance.DefaultIfEmpty()
                                     select new { config.Name, application, version, instance = instance ?? createInstance(config) };

                foreach (var instanceParam in instanceParams)
                {
                    //TODO: it crashes if app configured for instance is not found
                    instanceParam.instance.UpdateApplicationParams(instanceParam.application.GetLoadParams(instanceParam.version), instanceParam.version);
                }
                notefyInstancesChanged();
            }
        }

        private ApplicationInstance createInstance(InstanceConfig config)
        {
            var instance = m_InstanceFactory.Create(config.Name, m_Context);
            m_Instances.Add(instance);
            instance.Subscribe(status => notefyInstancesChanged(instance.Name + ":" + instance.Status));
            return instance;
        }

        private void validateInstanceConfig(ApplicationInstanceInfo config)
        {
            if (string.IsNullOrEmpty(config.Name))
                throw new ArgumentException("Instance name should be not empty string");
            if (string.IsNullOrEmpty(config.ApplicationId))
                throw new ArgumentException("Instance application is not provided");
            if (m_Applications.All(x => x.Name != config.ApplicationId))
                throw new ArgumentException("Application '" + config.ApplicationId + "' not found");

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

        public void AddInstance(ApplicationInstanceInfo config)
        {
            string instances;
            lock (m_SyncRoot)
            {
                validateInstanceConfig(config);
                if (m_InstancesConfiguration.Any(x => x.Name == config.Name))
                    throw new ConfigurationErrorsException(string.Format("Instance named '{0}' already exists", config.Name));
                var cfg = new InstanceConfig
                {
                    Name = config.Name,
                    Version = config.Version,
                    ApplicationId = config.ApplicationId,
                    AutoStart = config.AutoStart
                };
                instances = JsonConvert.SerializeObject(m_InstancesConfiguration.Concat(new[] { cfg }).ToArray(), Formatting.Indented);
            }
            m_ConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
            updateInstances();
        }
        
        public void UpdateInstance(ApplicationInstanceInfo config)
        {
            string instances;
            lock (m_SyncRoot)
            {
                //TODO: rename app and logs folders on rename
                validateInstanceConfig(config);
                if (m_InstancesConfiguration.All(x => x.Name != config.Id))
                    throw new ConfigurationErrorsException(string.Format("Instance named '{0}' not found", config.Name));
                if (config.Name!=config.Id && m_InstancesConfiguration.Any(x => x.Name == config.Name))
                    throw new ConfigurationErrorsException(string.Format("Can not rename instance '{0}' to {1}. Instance with this name already exists", config.Id, config.Name));
                var cfg = new InstanceConfig
                {
                    Name = config.Name,
                    Version = config.Version,
                    ApplicationId = config.ApplicationId,
                    AutoStart = config.AutoStart
                };

                instances = JsonConvert.SerializeObject(m_InstancesConfiguration.Where(c => c.Name != config.Id).Concat(new[] { cfg }).ToArray(), Formatting.Indented);
                m_Instances.First(i => i.Name == config.Id).Rename(cfg.Name);
            }

            m_ConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
            updateInstances();
        }

        public void DeleteInstance(string name)
        {
            try
            {
                ApplicationInstance instance;
                lock (m_SyncRoot)
                {
                    instance = m_Instances.FirstOrDefault(i => i.Name == name);
                    if (instance == null)
                        return;
                    m_Instances.Remove(instance);
                    m_InstanceFactory.Release(instance);
                }
                //TODO: cleanup instance folder.
                var instances = JsonConvert.SerializeObject(m_InstancesConfiguration.Where(i => i.Name != name).ToArray(), Formatting.Indented);
                m_ConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
                updateInstances();
                instance.Dispose();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to delete instance {0}", name);
                throw;
            }
        }


        public void StopInstance(string name)
        {
            try
            {
                ApplicationInstance instance;
                lock (m_SyncRoot)
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
                lock (m_SyncRoot)
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


        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged
        {
            get {return new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>();}
        }


        public void Dispose()
        {
            Logger.InfoFormat("Host is disposed");
        }
    }
}