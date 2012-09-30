using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
    public class ApplicationInstance : IDisposable, IObservable<HostedAppStatus>
    {
        private readonly ApplicationParams m_ApplicationParams;
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


        public ApplicationInstance(string applicationId, string name, Version version, ApplicationParams applicationParams, AppServerContext context, IConfigurationProvider configurationProvider,
                                   ILogCache logCache, ILogger logger)
        {
            m_LogCache = logCache;
            ApplicationId = applicationId;
            m_ApplicationParams = applicationParams;
            Name = name;
            Version = version;
            Logger = logger;
            m_ConfigurationProvider = configurationProvider;
            m_Context = context;
        }

        public string Name { get; set; }
        public string ApplicationId { get; private set; }
        public Version Version { get; private set; }
        public ILogger Logger { get; set; }

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
                Logger.DebugFormat("Status changed to {0}",value);
                m_StatusSubject.OnNext(value);
            }
        }

        #region IObservable<HostedAppStatus> Members

        public IDisposable Subscribe(IObserver<HostedAppStatus> observer)
        {
            return m_StatusSubject.Subscribe(observer);
        }

        #endregion

        public void Start()
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if (Status == HostedAppStatus.Starting || Status == HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is " + Status);
                if (Status == HostedAppStatus.Started)
                    throw new InvalidOperationException("Instance already started");
                Status = HostedAppStatus.Starting;

                Logger.InfoFormat("Starting application {0} v{1} instance '{2}'", ApplicationId, Version, Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                                                          {
                                                              try
                                                              {              
                                                                  Logger.InfoFormat("Application {0} v{1} instance '{2}' started", ApplicationId, Version, Name);
                                                                  createHost();
                                                                  //TODO: may be it is better to move wrapping with MarshalableProxy to castle
                                                                  m_ApplicationHost.Start(MarshalableProxy.Generate(m_ConfigurationProvider), MarshalableProxy.Generate(m_LogCache), m_Context,Name);

                                                                  lock (m_SyncRoot)
                                                                  {
                                                                      Status = HostedAppStatus.Started;
                                                                  }
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                  Logger.ErrorFormat(e, "Application {0} v{1} instance '{2}' failed to start", ApplicationId, Version, Name);
                                                                  lock (m_SyncRoot)
                                                                  {
                                                                      Status = HostedAppStatus.Stopped;
                                                                  }
                                                              }
                                                          });
            }
        }

        private void createHost()
        {
            //TODO: use folders for named instances
            string path = Path.GetFullPath(new[] {m_Context.AppsDirectory, Name}.Aggregate(Path.Combine));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            AppDomain domain = AppDomain.CreateDomain(ApplicationId, null, new AppDomainSetup
                                                                               {
                                                                                   ApplicationBase = path,
                                                                                   PrivateBinPathProbe = null,
                                                                                   DisallowApplicationBaseProbing = true,
                                                                                   ConfigurationFile = m_ApplicationParams.ConfigFile
                                                                               });
            m_AppDomain = domain;
            m_AppDomain.Load(typeof (HostedAppInfo).Assembly.GetName());
            var appDomainInitializer =
                (AppDomainInitializer)
                m_AppDomain.CreateInstanceFromAndUnwrap(typeof (AppDomainInitializer).Assembly.Location, typeof (AppDomainInitializer).FullName, false, BindingFlags.Default, null, null, null, null);


            appDomainInitializer.Initialize(path, m_ApplicationParams.AssembliesToLoad, m_ApplicationParams.NativeDllToLoad.ToArray());
            m_ApplicationHost = appDomainInitializer.CreateHost(m_ApplicationParams.AppType);
        }


        public void Stop()
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if (Status == HostedAppStatus.Starting || Status == HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is " + Status);
                if (Status == HostedAppStatus.Stopped)
                    throw new InvalidOperationException("Instance is not started started");
                Status = HostedAppStatus.Stopping;
                Logger.InfoFormat("Stopping application {0} v{1} instance '{2}'", ApplicationId, Version, Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                                                          {
                                                              try
                                                              {
                                                                  m_ApplicationHost.Stop();
                                                                  AppDomain.Unload(m_AppDomain);
                                                                  Logger.InfoFormat("Application {0} v{1} instance '{2}' stopped", ApplicationId, Version, Name);
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                  Logger.ErrorFormat(e, "Application {0} v{1} instance '{2}' failed to stop", ApplicationId, Version, Name);
                                                              }
                                                              lock (m_SyncRoot)
                                                              {
                                                                  Status = HostedAppStatus.Stopped;
                                                              }
                                                          });
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (m_SyncRoot)
            {
                m_IsDisposing = true;
            }
            if (m_CurrentTask != null)
                m_CurrentTask.Wait();

            if (Status == HostedAppStatus.Started)
                m_ApplicationHost.Stop();
        }

        #endregion
    }
}