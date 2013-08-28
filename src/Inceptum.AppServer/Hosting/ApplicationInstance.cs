using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Initializer;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;
using AppDomainInitializer = Inceptum.AppServer.Initializer.AppDomainInitializer;

namespace Inceptum.AppServer.Hosting
{
     [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ApplicationInstance : IDisposable, IObservable<HostedAppStatus>, IApplicationInstance
    {
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly AppServerContext m_Context;
        private readonly ILogCache m_LogCache;
        private readonly Subject<HostedAppStatus> m_StatusSubject = new Subject<HostedAppStatus>();
        private readonly object m_SyncRoot = new object();
        private AppDomain m_AppDomain;
        private IApplicationHost m_ApplicationHost;
        private Task m_CurrentTask;
        private bool m_IsDisposing;
        private HostedAppStatus m_Status;
        private ApplicationParams m_ApplicationParams;
        private Version m_ActualVersion;
        private ServiceHost m_ServiceHost;

        public string Name { get; set; }
        public string Environment { get; set; }
        public ILogger Logger { get; set; }
        public bool HasToBeRecreated { get; set; }
        public bool IsMisconfigured { get; set; }

        public HostedAppStatus Status
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_Status;
                }
            }
            private set
            {
                lock (m_SyncRoot)
                {
                    if (m_Status == value)
                        return;
                    m_Status = value;
                }
                Logger.DebugFormat("Instance '{0}' status changed to {1}",Name,value);
                m_StatusSubject.OnNext(value);
            }
        }

        public Version ActualVersion
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_ActualVersion;
                }
            }
        }


        public ApplicationInstance(string name, string environment, AppServerContext context, IConfigurationProvider configurationProvider,
                                   ILogCache logCache, ILogger logger)
        {
            m_LogCache = logCache;
            Name = name;
            Environment = environment;
            Logger = logger;
            m_ConfigurationProvider = configurationProvider;
            m_Context = context;
            IsMisconfigured = true;
        }

        public void UpdateApplicationParams(ApplicationParams applicationParams, Version actualVersion)
        {
            m_ActualVersion = actualVersion;
            lock (m_SyncRoot)
            {
                if(m_ApplicationParams==applicationParams)
                    return;
                m_ApplicationParams = applicationParams;
                IsMisconfigured = applicationParams == null;
                HasToBeRecreated = !IsMisconfigured && Status==HostedAppStatus.Starting || Status==HostedAppStatus.Started;
            }
        }

        public void Start()
        {
            lock (m_SyncRoot)
            {
                if (IsMisconfigured)
                    throw new ConfigurationErrorsException("Instance is misconfigured");
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if (Status == HostedAppStatus.Starting || Status == HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is " + Status);
                if (Status == HostedAppStatus.Started)
                    throw new InvalidOperationException("Instance already started");
                Status = HostedAppStatus.Starting;

                Logger.InfoFormat("Starting instance '{0}'", Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                                                          {
                                                              try
                                                              {              
                                                                  createHost();
                                                                  //TODO: may be it is better to move wrapping with MarshalableProxy to castle
                                                                  Commands=m_ApplicationHost.Start(
                                                                      MarshalableProxy.Generate(m_ConfigurationProvider),
                                                                      MarshalableProxy.Generate(m_LogCache),
                                                                      m_Context, Name, Environment);

                                                                  lock (m_SyncRoot)
                                                                  {
                                                                      Status = HostedAppStatus.Started;
                                                                  }
                                                                  Logger.InfoFormat("Instance {0} started", Name);
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                  Commands=new InstanceCommand[0];
                                                                  Logger.ErrorFormat(e, "Instance '{0}' failed to start", Name);
                                                                  lock (m_SyncRoot)
                                                                  {
                                                                      Status = HostedAppStatus.Stopped;
                                                                  }
                                                              }
                                                          });
            }
        }

        internal InstanceCommand[] Commands { get; private set; }

        public void Stop()
        {
            stop(false);
        }

        private void stop(bool abort)
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if ((Status == HostedAppStatus.Starting && !abort)|| Status == HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is " + Status);
                if (Status == HostedAppStatus.Stopped)
                    throw new InvalidOperationException("Instance is not started");
                Commands = new InstanceCommand[0];

                Status = HostedAppStatus.Stopping;
                Logger.InfoFormat("Stopping instance '{0}'", Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            m_ApplicationHost.Stop();
                        }
                        catch (Exception e)
                        {
                            Logger.ErrorFormat(e, "Instance '{0}' application failed while stopping", Name);
                        }
                        finally
                        {
                            Logger.InfoFormat("Instance '{0}' stopped", Name);
                            AppDomain.Unload(m_AppDomain);
                        }
                        lock (m_SyncRoot)
                        {
                            Status = HostedAppStatus.Stopped;
                        }
                    });
            }
        }

        private void createHost()
        {
            m_ServiceHost = new ServiceHost(this);
            m_ServiceHost.AddServiceEndpoint(typeof(IApplicationInstance), new NetNamedPipeBinding(), new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id + "/instances/" + Name));
            //m_ConfigurationProviderServiceHost.Faulted += new EventHandler(this.IpcHost_Faulted);
            m_ServiceHost.Open();

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "Inceptum.AppServer.Initializer.exe",
                Arguments = Name,
                UseShellExecute = false,
                CreateNoWindow = true
            });


            string path = Path.GetFullPath(new[] {m_Context.AppsDirectory, Name}.Aggregate(Path.Combine));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);


            ApplicationParams applicationParams;
            lock(m_SyncRoot)
            {
                applicationParams = m_ApplicationParams.Clone();
            }


            m_AppDomain =  AppDomain.CreateDomain(Name, null, new AppDomainSetup
                                                                               {
                                                                                   ApplicationBase = path,
                                                                                   PrivateBinPathProbe = null,
                                                                                   DisallowApplicationBaseProbing = true,
                                                                                   ConfigurationFile = applicationParams.ConfigFile
                                                                               });

            var appDomainInitializer = (AppDomainInitializer)m_AppDomain.CreateInstanceFromAndUnwrap(typeof(AppDomainInitializer).Assembly.Location, typeof(AppDomainInitializer).FullName, false, BindingFlags.Default, null, null, null, null);

            var assembliesToLoad = new Dictionary<AssemblyName, string>(applicationParams.AssembliesToLoad)
                {
                    {typeof (AppInfo).Assembly.GetName(), typeof (AppInfo).Assembly.Location},
                    {typeof (ApplicationInstance).Assembly.GetName(),typeof (ApplicationInstance).Assembly.Location},
                     //AppServer is loaded not from package, so it dependencies used in appDOmain plugin should be provided aswell
                    {typeof (LoggingFacility).Assembly.GetName(),typeof (LoggingFacility).Assembly.Location}
                };
            appDomainInitializer.Initialize(path, assembliesToLoad, applicationParams.NativeDllToLoad.ToArray(),new AppDomainCrashHandler( err =>
            {
                Logger.FatalFormat("Instance '{0}' crashed:{1}", Name, err);
                // stop(true);
            }));
            m_ApplicationHost = (IApplicationHost) appDomainInitializer.CreateInstance(typeof(ApplicationHost<>).AssemblyQualifiedName,applicationParams.AppType);
        }

        #region IObservable<HostedAppStatus> Members

        public IDisposable Subscribe(IObserver<HostedAppStatus> observer)
        {
            return m_StatusSubject.Subscribe(observer);
        }

        #endregion
 

        #region IDisposable Members

        public void Dispose()
        {
            lock (m_SyncRoot)
            {
                m_IsDisposing = true;
            }

            Action finishDispose = () =>
            {
                //TODO: exception handling
                if (Status == HostedAppStatus.Started)
                    m_ApplicationHost.Stop();
                m_StatusSubject.Dispose();
            };

            if (m_CurrentTask != null)
            {
                m_CurrentTask.ContinueWith(task => finishDispose());
            }
            else
            {
                Task.Factory.StartNew(finishDispose);
            }
        }

        #endregion

        public void Rename(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            lock (m_SyncRoot)
            {
                Name = name;
            }
        }

        public string ExecuteCommand(InstanceCommand command)
        {
            var cmd = Commands.FirstOrDefault(c => c.Name == command.Name);
            if(cmd==null)
                throw new InvalidOperationException(string.Format("Command '{0}' not found",command));
           return m_ApplicationHost.Execute(command);
        }

        public void RegisterApplicationHost()
        {
            var operationContext = OperationContext.Current;
            var operationContextType = operationContext.GetType();
            var getCallbackChannelMethod = operationContextType.GetMethod("GetCallbackChannel");

            getCallbackChannelMethod = getCallbackChannelMethod.MakeGenericMethod(typeof(IApplicationInitializer));
            var initializer = (IApplicationInitializer)getCallbackChannelMethod.Invoke(operationContext, null);
            Task.Factory.StartNew(() =>
            {
                ApplicationParams applicationParams;
                lock (m_SyncRoot)
                {
                    applicationParams = m_ApplicationParams.Clone();
                }


                var assembliesToLoad = new Dictionary<AssemblyName, string>(applicationParams.AssembliesToLoad)
                {
                    {typeof (AppInfo).Assembly.GetName(), typeof (AppInfo).Assembly.Location},
                    {typeof (ApplicationInstance).Assembly.GetName(), typeof (ApplicationInstance).Assembly.Location},
                    //AppServer is loaded not from package, so it dependencies used in appDOmain plugin should be provided aswell
                    {typeof (LoggingFacility).Assembly.GetName(), typeof (LoggingFacility).Assembly.Location}
                };
                initializer.Initialize(assembliesToLoad.Select(p=>Tuple.Create(p.Key.ToString(),p.Value)).ToArray(), applicationParams.NativeDllToLoad.ToArray());
            });



            //           m_ApplicationHost = (IApplicationHost)appDomainInitializer.CreateInstance(typeof(ApplicationHost<>).AssemblyQualifiedName, applicationParams.AppType);


        }
    }
}