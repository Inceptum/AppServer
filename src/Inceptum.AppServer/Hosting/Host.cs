﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Utils;
using Inceptum.AppServer.Windsor;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Hosting
{
    class Host : IHost, IDisposable
    {
        private ILogger Logger { get; set; }
        public string Name { get; set; }
        private readonly List<ApplicationInstance> m_Instances = new List<ApplicationInstance>();
        private InstanceConfig[] m_InstancesConfiguration = new InstanceConfig[0];

        private readonly IApplicationInstanceFactory m_InstanceFactory;
        private readonly AppServerContext m_Context;
        private readonly IEnumerable<IHostNotificationListener> m_Listeners;
        private readonly IManageableConfigurationProvider m_ServerConfigurationProvider;
        private readonly object m_SyncRoot = new object();
        private readonly ApplicationRepository m_ApplicationRepository;
        private readonly ServiceHostWrapper<IConfigurationProvider> m_ConfigurationProviderServiceHost;
        private readonly ServiceHostWrapper<ILogCache> m_LogCacheServiceHost;
        private readonly JobObject m_JobObject;
        private readonly PeriodicalBackgroundWorker m_InstanceChecker;
        private bool m_IsStopped = true;

        public Host(ILogCache logCache, IManageableConfigurationProvider serverConfigurationProvider, IConfigurationProvider applicationConfigurationProvider, IApplicationInstanceFactory instanceFactory, IEnumerable<IHostNotificationListener> listeners, ApplicationRepository applicationRepository, ILogger logger = null)
        {
            m_JobObject = new JobObject();
            m_ApplicationRepository = applicationRepository;
            m_ServerConfigurationProvider = serverConfigurationProvider;
            m_Listeners = listeners;
            m_InstanceFactory = instanceFactory;
            Logger = logger ?? NullLogger.Instance;

            Name = Environment.MachineName;
            try
            {
                var bundleString = serverConfigurationProvider.GetBundle("AppServer", "server.host", "{machineName}");
                var setup = JsonConvert.DeserializeObject<AppServerSetup>(bundleString);
                if (setup.Name != null)
                {
                    Name = setup.Name;
                }
                else
                {
                    Logger.WarnFormat("Failed to get server name from configuration , using default:{0}", Name);
                }
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to get server name from configuration , using default:{0}", Name);
            }

            m_Context = new AppServerContext
            {
                Name = Name,
                AppsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps"),
                BaseDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            m_InstanceChecker = new PeriodicalBackgroundWorker("InstanceChecker", 1000, checkInstances);

            m_ConfigurationProviderServiceHost = new ServiceHostWrapper<IConfigurationProvider>(Logger, applicationConfigurationProvider, "ConfigurationProvider");
            m_LogCacheServiceHost = new ServiceHostWrapper<ILogCache>(Logger, logCache, "LogCache");
        }

        private void checkInstances()
        {
            ApplicationInstance[] instances;
            lock (m_Instances)
            {
                instances = m_Instances.ToArray();
            }
            foreach (var instance in instances)
            {
                instance.VerifySate();
            }
        }

        public string MachineName
        {
            get { return Environment.MachineName; }
        }

        public Application[] Applications
        {
            get
            {
                return m_ApplicationRepository.Applications;
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
                            orderby Tuple.Create(cfg.Environment, cfg.Name)
                            select new ApplicationInstanceInfo
                                       {
                                           Name = cfg.Name,
                                           Id = cfg.Name,
                                           ApplicationId = cfg.ApplicationId,
                                           ApplicationVendor = cfg.ApplicationVendor,
                                           Environment = cfg.Environment,
                                           Status = instance.Status,
                                           Version = cfg.Version,
                                           AutoStart = cfg.AutoStart,
                                           StartOrder = cfg.StartOrder??int.MaxValue,
                                           ActualVersion = instance.ActualVersion,
                                           Commands = instance.Commands,
                                           User = cfg.User,
                                           LogLevel = cfg.LogLevel,
                                           DefaultConfiguration = cfg.DefaultConfiguration,
                                           MaxLogSize = cfg.MaxLogSize,
                                           LogLimitReachedAction = cfg.LogLimitReachedAction,
                                           IsDebuggable = m_ApplicationRepository.Applications.Where(a => a.Vendor == cfg.ApplicationVendor && a.Name == cfg.ApplicationId).Select(a => a.Debug).FirstOrDefault()
                                       }).ToArray();
                }
            }
        }

        public void Start()
        {
            m_IsStopped = false;

            RediscoverApps();

            Logger.Info("Reading instances config");
            updateInstances();
            IGrouping<int, InstanceConfig>[] startGroups;
            lock (m_SyncRoot)
            {
                startGroups = m_InstancesConfiguration
                    .Where(c => c.AutoStart)
                    .GroupBy(c => c.StartOrder ?? Int32.MaxValue)
                    .OrderBy(c => c.Key).ToArray();
            }

            var sw = Stopwatch.StartNew();
            Task.Factory.StartNew(() =>
            {
                foreach (var startGroup in startGroups)
                {
                    var groupTasks = startGroup
                        .AsParallel()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                        .Select(async instance =>
                        {
                            await startInstance(instance.Name, safe: true);
                            Logger.InfoFormat("Started instance with {0} start order {1}", instance.Name,
                                instance.StartOrder);
                        })
                        .ToArray();
                    Task.WaitAll(groupTasks);
                }
            }).ContinueWith(t =>
            {
                sw.Stop();
                Logger.InfoFormat("Started all instances, time = {0}", sw.Elapsed);
            });
        }

        public void RediscoverApps()
        {
            if (m_IsStopped)
                throw new ObjectDisposedException("Host is disposed");

            m_ApplicationRepository.RediscoverApps();
            notifyApplicationsChanged();
        }

        private void createInstance(InstanceConfig config)
        {
            var instance = m_InstanceFactory.Create(config.Name, m_Context);
            m_Instances.Add(instance);
            instance.Subscribe(status => notifyInstancesChanged(instance.Name + ":" + instance.Status));
        }

        private void validateInstanceConfig(ApplicationInstanceInfo config)
        {
            if (string.IsNullOrEmpty(config.Name))
                throw new ArgumentException("Instance name should be not empty string");
            if (string.IsNullOrEmpty(config.ApplicationId))
                throw new ArgumentException("Instance application is not provided");
            if (string.IsNullOrEmpty(config.ApplicationVendor))
                throw new ArgumentException("Instance application is not provided");
            if (string.IsNullOrEmpty(config.Environment))
                throw new ArgumentException("Instance environment is not provided");

            if (m_ApplicationRepository.Applications.All(x => x.Name != config.ApplicationId && x.Vendor == config.ApplicationVendor))
                throw new ArgumentException("Application '" + config.ApplicationVendor + "(c) " + config.ApplicationId + "' not found");

        }

        private void notifyInstancesChanged(string comment = null)
        {
            foreach (var listener in m_Listeners)
            {
                try
                {
                    listener.InstancesChanged(comment);
                }
                catch (Exception e)
                {
                    Logger.WarnFormat(e, "Failed to deliver instances changed notification");
                }
            }
        }

        private void notifyApplicationsChanged(string comment = null)
        {
            foreach (var listener in m_Listeners)
            {
                try
                {
                    listener.ApplicationsChanged(comment);
                }
                catch (Exception e)
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
                if (m_IsStopped)
                    throw new ObjectDisposedException("Host is disposed");

                validateInstanceConfig(config);
                if (m_InstancesConfiguration.Any(x => x.Name == config.Name))
                    throw new ConfigurationErrorsException(string.Format("Instance named '{0}' already exists", config.Name));
                var cfg = new InstanceConfig
                {
                    Name = config.Name,
                    Version = config.Version,
                    ApplicationId = config.ApplicationId,
                    ApplicationVendor = config.ApplicationVendor,
                    Environment = config.Environment,
                    AutoStart = config.AutoStart,
                    StartOrder = config.StartOrder,
                    User = config.User,
                    Password = string.IsNullOrEmpty(config.Password)
                                    ? null
                                    : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(config.Password ?? ""), new byte[0], DataProtectionScope.LocalMachine)),
                    LogLevel = config.LogLevel,
                    DefaultConfiguration = config.DefaultConfiguration,
                    MaxLogSize = config.MaxLogSize,
                    LogLimitReachedAction = config.LogLimitReachedAction
                };
                instances = JsonConvert.SerializeObject(m_InstancesConfiguration.Concat(new[] { cfg }).ToArray(), Formatting.Indented);
            }
            m_ServerConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);

            var application = m_ApplicationRepository.Applications.FirstOrDefault(a => a.Name == config.ApplicationId && a.Vendor == config.ApplicationVendor);
            if (application == null)
                throw new InvalidOperationException(string.Format("Application {0} not found", config.ApplicationId));

            updateInstances();
        }

        public void SetInstanceVersion(string name, Version version)
        {
            Logger.InfoFormat("Setting instance '{0}' version to {1}", name, version);
            try
            {
                string instances;
                lock (m_SyncRoot)
                {
                    if (m_IsStopped)
                        throw new ObjectDisposedException("Host is disposed");

                    var instanceConfig = m_InstancesConfiguration.FirstOrDefault(x => x.Name == name);
                    if (instanceConfig == null)
                        throw new ConfigurationErrorsException(string.Format("Instance named '{0}' not found", name));

                    instanceConfig.Version = version;

                    instances = JsonConvert.SerializeObject(m_InstancesConfiguration.Where(c => c.Name != instanceConfig.Name).Concat(new[] { instanceConfig }).ToArray(), Formatting.Indented);
                }
                m_ServerConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
                Logger.InfoFormat("Instance '{0}' version is set to {1}", name, version);

                updateInstances();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, " Instance {0} failed to change version to {1} ", name, version);
                throw;
            }
        }

        public void UpdateInstance(ApplicationInstanceInfo config)
        {
            string instances;
            lock (m_SyncRoot)
            {
                if (m_IsStopped)
                    throw new ObjectDisposedException("Host is disposed");

                //TODO: rename app and logs folders on rename
                validateInstanceConfig(config);
                if (m_InstancesConfiguration.All(x => x.Name != config.Id))
                    throw new ConfigurationErrorsException(string.Format("Instance named '{0}' not found", config.Name));
                if (config.Name != config.Id && m_InstancesConfiguration.Any(x => x.Name == config.Name))
                    throw new ConfigurationErrorsException(string.Format("Can not rename instance '{0}' to {1}. Instance with this name already exists", config.Id, config.Name));
                var originalPassword = m_InstancesConfiguration.First(c => c.Name == config.Id).Password;
                var cfg = new InstanceConfig
                {
                    Name = config.Name,
                    Version = config.Version,
                    ApplicationId = config.ApplicationId,
                    ApplicationVendor = config.ApplicationVendor,
                    Environment = config.Environment,
                    AutoStart = config.AutoStart,
                    StartOrder=config.StartOrder,
                    User = config.User,
                    Password = string.IsNullOrEmpty(config.Password)
                        ? originalPassword
                        : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(config.Password ?? ""), new byte[0], DataProtectionScope.LocalMachine)),
                    LogLevel = config.LogLevel,
                    DefaultConfiguration = config.DefaultConfiguration,
                    MaxLogSize = config.MaxLogSize,
                    LogLimitReachedAction = config.LogLimitReachedAction
                };

                instances = JsonConvert.SerializeObject(m_InstancesConfiguration.Where(c => c.Name != config.Id).Concat(new[] { cfg }).ToArray(), Formatting.Indented);
                var instance = m_Instances.First(i => i.Name == config.Id);
                instance.ChangeLogLevel(config.LogLevel.ToString());
                instance.Rename(cfg.Name);
            }

            m_ServerConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
            updateInstances();
        }

        public void DeleteInstance(string name)
        {
            try
            {
                if (m_IsStopped)
                    throw new ObjectDisposedException("Host is disposed");

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
                m_ServerConfigurationProvider.CreateOrUpdateBundle("AppServer", "instances", instances);
                updateInstances();
                instance.Dispose();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to delete instance {0}", name);
                throw;
            }
        }

        public string ExecuteCommand(string name, InstanceCommand command)
        {
            Logger.InfoFormat("Executing command {0} with instance {1} ", command.Name, name);
            try
            {
                Task<string> task;
                lock (m_SyncRoot)
                {
                    if (m_IsStopped)
                        throw new ObjectDisposedException("Host is disposed");

                    ApplicationInstance instance = m_Instances.FirstOrDefault(i => i.Name == name);
                    if (instance == null)
                        return null;
                    task = instance.ExecuteCommand(command);
                }
                var result = task.ConfigureAwait(false).GetAwaiter().GetResult();
                return string.IsNullOrWhiteSpace(result) ? string.Format("Command '{0}' has executed", command.Name) : result;
            }
            catch (AggregateException ae)
            {
                Logger.WarnFormat(ae.Flatten(), "Instance {0} failed to execute command {1}", name, command.Name);
                throw ae.Flatten();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Instance {0} failed to execute command {1}", name, command.Name);
                throw;
            }
        }

        public void Debug(string name)
        {
            try
            {
                Task task=null;
                lock (m_SyncRoot)
                {
                    if (m_IsStopped)
                        throw new ObjectDisposedException("Host is disposed");

                    ApplicationInstance instance = m_Instances.FirstOrDefault(i => i.Name == name);
                    if (instance == null)
                        return;
                    if(instance.Status==HostedAppStatus.Stopped)
                        task=startInstance(name, false, true);
                    else if(instance.Status==HostedAppStatus.Started)
                        task = instance.Debug();
                }
                if (task != null)
                    task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (AggregateException ae)
            {
                Logger.WarnFormat(ae.Flatten(), "Failed to lounch debugger for instance {0} ", name);
                throw ae.Flatten();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to lounch debugger for instance", name);
                throw;
            }
        }

        public Task StopInstance(string name)
        {
            Logger.InfoFormat("Stopping instance {0}  ", name);
            ApplicationInstance instance;
            lock (m_SyncRoot)
            {
                if (m_IsStopped)
                    throw new ObjectDisposedException("Host is disposed");
                instance = m_Instances.FirstOrDefault(i => i.Name == name);
            }

            if (instance == null)
                throw new ConfigurationErrorsException(string.Format("Instance '{0}' not found", name));

            return instance.Stop(false);
        }

        public void KillInstanceProcess(string name)
        {
            try
            {
                Logger.InfoFormat("Stopping instance {0}  ", name);
                ApplicationInstance instance;
                lock (m_SyncRoot)
                {
                    if (m_IsStopped)
                        throw new ObjectDisposedException("Host is disposed");
                    instance = m_Instances.FirstOrDefault(i => i.Name == name);
                }

                if (instance == null)
                    throw new ConfigurationErrorsException(string.Format("Instance '{0}' not found", name));

                instance.KillProcess();
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to kill instance {0} process", name);
                throw;
            }
        }

        public Task StartInstance(string name, bool doDebug)
        {
            return startInstance(name, false, doDebug);
        }

        private Task startInstance(string name, bool safe, bool doDebug = false)
        {
            Logger.InfoFormat(" Starting instance {0}  ", name);
            try
            {
                ApplicationInstance instance;
                lock (m_SyncRoot)
                {
                    if (m_IsStopped)
                        throw new ObjectDisposedException("Host is disposed");

                    instance = m_Instances.FirstOrDefault(i => i.Name == name);
                }
                if (instance == null)
                    throw new ConfigurationErrorsException(string.Format("Instance '{0}' not found", name));

                var config = m_InstancesConfiguration.FirstOrDefault(i => i.Name == name);
                if (config == null)
                    throw new InvalidOperationException(string.Format("Configuration of instance {0} not found", name));
                var application = m_ApplicationRepository.Applications.FirstOrDefault(a => a.Name == config.ApplicationId && a.Vendor == config.ApplicationVendor);
                if (application == null)
                    throw new InvalidOperationException(string.Format("Application {0} not found", config.ApplicationId));

                var version = config.Version ?? application.Versions.Select(v => v.Version).OrderByDescending(v => v).FirstOrDefault();

                instance.UpdateConfig(version, config.Environment, config.User, config.Password, config.LogLevel, config.DefaultConfiguration, config.MaxLogSize, config.LogLimitReachedAction);

                return instance.Start(application.Debug && doDebug, () => m_ApplicationRepository.Install(application, version, Path.Combine(m_Context.AppsDirectory, config.Name) + "\\"));
            }
            catch (Exception e)
            {
                Logger.WarnFormat(e, "Failed to start instance {0}", name);
                if (!safe)
                    throw;
                return Task.FromResult<object>(null);
            }
        }

        private void updateInstances()
        {
            Logger.InfoFormat("Updating instances");

            var bundle = m_ServerConfigurationProvider.GetBundle("AppServer", "instances");
            var configs = JsonConvert.DeserializeObject<InstanceConfig[]>(bundle).GroupBy(i => i.Name).Select(g => g.First());

            lock (m_SyncRoot)
            {
                m_InstancesConfiguration = configs.ToArray();
                foreach (var config in m_InstancesConfiguration.Where(config => m_Instances.All(i => i.Name != config.Name)))
                {
                    Logger.InfoFormat("Creating new instance '{0}'", config.Name);
                    createInstance(config);
                }
                notifyInstancesChanged();
            }
            Logger.InfoFormat("Instances are updated");
        }

        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged
        {
            //TODO: fake!!!
            get { return new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>(); }
        }

        public void Stop()
        {
            if (m_IsStopped)
                throw new ObjectDisposedException("Host not started");
            Logger.InfoFormat("Host is stopping");
            m_IsStopped = true;
            IEnumerable<Task> tasks;
            lock (m_SyncRoot)
            {
                Logger.InfoFormat("Stopping instances");
                tasks = m_Instances.Where(instance => instance.Status != HostedAppStatus.Stopped && instance.Status != HostedAppStatus.Stopping).Select(instance => instance.Stop(true));
            }

            Task.WaitAll(tasks.ToArray());
            foreach (var instance in m_Instances)
            {
                m_InstanceFactory.Release(instance);
            }
            m_Instances.Clear();
            Logger.InfoFormat("Host is stopped");
        }

        public void Dispose()
        {
            m_InstanceChecker.Dispose();
            m_JobObject.Dispose();
            m_ConfigurationProviderServiceHost.Dispose();
            m_LogCacheServiceHost.Dispose();
        }

        class AppServerSetup
        {
            public string Name { get; set; }
        }
    }
}