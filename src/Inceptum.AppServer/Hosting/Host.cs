using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
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
        private readonly List<ApplicationInstance> m_Instances = new List<ApplicationInstance>();
        private InstanceConfig[] m_InstancesConfiguration =new InstanceConfig[0];

        private readonly IApplicationInstanceFactory m_InstanceFactory;
        private readonly AppServerContext m_Context;
        private readonly IEnumerable<IHostNotificationListener> m_Listeners;
        private readonly IManageableConfigurationProvider m_ConfigurationProvider;
        private readonly object m_SyncRoot = new object();
        private readonly ApplicationRepositary m_ApplicationRepositary;

        public Host(IManageableConfigurationProvider configurationProvider, IApplicationInstanceFactory instanceFactory, IEnumerable<IHostNotificationListener> listeners, ApplicationRepositary applicationRepositary, ILogger logger = null, string name = null)
        {
            m_ApplicationRepositary = applicationRepositary;
            m_ConfigurationProvider = configurationProvider;
            m_Listeners = listeners;
            m_InstanceFactory = instanceFactory;
            Logger = logger;
            Name = name??Environment.MachineName;
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
                return m_ApplicationRepositary.Applications;
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
                                               Environment = cfg.Environment,
                                               Status = instance.Status,
                                               Version = cfg.Version,
                                               AutoStart = cfg.AutoStart,
                                               ActualVersion=instance.ActualVersion,
                                               Commands = instance.Commands
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
                tasks = m_InstancesConfiguration
                        .Where(c => c.AutoStart)
                        .Select(c => c.Name)
                        .Select(instance => Task.Factory.StartNew(() =>startInstance(instance,true)));
                Logger.InfoFormat("Starting instances");                
            }
            Task.WaitAll(tasks.ToArray());
        }

        public void RediscoverApps()
        {
            m_ApplicationRepositary.RediscoverApps();
            notefyApplicationsChanged();
        }


        private ApplicationInstance createInstance(InstanceConfig config)
        {
            var instance = m_InstanceFactory.Create(config.Name, config.Environment, m_Context);
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
            if (string.IsNullOrEmpty(config.Environment))
                throw new ArgumentException("Instance environment is not provided");

            if (m_ApplicationRepositary.Applications.All(x => x.Name != config.ApplicationId))
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

        private void notefyApplicationsChanged(string comment = null)
        {
            foreach (var listener in m_Listeners)
            {
                try
                {
                    listener.ApplicationsChanged(comment);
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
                    Environment = config.Environment,
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
                    Environment =  config.Environment,
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

        public string ExecuteCommand(string name, string command)
        {
            try
            {
                ApplicationInstance instance;
                lock (m_SyncRoot)
                {
                    instance = m_Instances.FirstOrDefault(i => i.Name == name);
                    if (instance == null)
                        return null;
                    return instance.ExecuteCommand(command);
                }
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
            startInstance(name,false);
        }

        private void startInstance(string name,bool safe)
        {
            try
            {
                ApplicationInstance instance;
                lock (m_SyncRoot)
                {
                    instance = m_Instances.FirstOrDefault(i => i.Name == name);

                    if (instance == null)
                        throw new ConfigurationErrorsException(string.Format("Instance '{0}' not found", name));

                    var config = m_InstancesConfiguration.FirstOrDefault(i => i.Name == name);
                    if (config == null)
                        throw new InvalidOperationException(string.Format("Configuration of instance {0} not found", name));
                    var application = m_ApplicationRepositary.Applications.FirstOrDefault(a => a.Name == config.ApplicationId);
                    if (application == null)
                        throw new InvalidOperationException(string.Format("Application {0} not found", config.ApplicationId));

                    var version = config.Version ?? application.Versions.Select(v => v.Version).OrderByDescending(v => v).FirstOrDefault();

                    m_ApplicationRepositary.EnsureLoadParams(config.ApplicationId, version);
                    instance.UpdateApplicationParams(application.GetLoadParams(version), version);
                }

                instance.Start();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to start instance {0}", name);
                if(!safe)
                    throw;
            }
        }

        private  void updateInstances()
        {
            var bundle = m_ConfigurationProvider.GetBundle("AppServer", "instances");
            var configs = JsonConvert.DeserializeObject<InstanceConfig[]>(bundle).GroupBy(i=>i.Name).Select(g=>g.First());

            lock (m_SyncRoot)
            {
                m_InstancesConfiguration = configs.ToArray();
                foreach (var config in m_InstancesConfiguration.Where(config => m_Instances.All(i => i.Name != config.Name)))
                {
                    createInstance(config);
                }
               /* var instanceParams = from config in m_InstancesConfiguration
                                     join app in m_ApplicationRepositary.Applications on config.ApplicationId equals app.Name into matchedApp
                                     from application in matchedApp.DefaultIfEmpty()
                                     where application!=null
                                     let version = config.Version ?? application.Versions.Select(v => v.Version).OrderByDescending(v => v).FirstOrDefault()
                                     join instance in m_Instances on config.Name equals instance.Name into matchedInstance
                                     from instance in matchedInstance.DefaultIfEmpty()
                                     select new { config.Name, application, version, instance = instance ?? createInstance(config) };

                foreach (var instanceParam in instanceParams)
                {
                    m_ApplicationRepositary.EnsureLoadParams(instanceParam.application.Name,instanceParam.version);
                    instanceParam.instance.UpdateApplicationParams(instanceParam.application!=null?instanceParam.application.GetLoadParams(instanceParam.version):null, instanceParam.version);
                }*/
                notefyInstancesChanged();
            }
        }


        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged
        {
            //TODO: fake!!!
            get {return new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>();}
        }


        public void Dispose()
        {
            Logger.InfoFormat("Host is disposed");
        }
    }
}