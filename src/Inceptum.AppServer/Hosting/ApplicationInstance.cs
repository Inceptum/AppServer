using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Hosting
{

    //TODO: merge with ApplicationHostProxy
    public class ApplicationInstance:IDisposable
    {
        public string Name { get; set; }
        public ApplicationName Application { get; private set; }
        public Version Version { get; private set; }
        public ILogger Logger { get; set; }
        private IApplicationHost m_ApplicationHost;
        public HostedAppStatus Status { get; private set; }

        readonly ManualResetEvent m_Started=new ManualResetEvent(false);
        private Task m_CurrentTask;
        private bool m_IsDisposing;
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly AppServerContext m_Context;
        private readonly ApplicationParams m_ApplicationParams;
        private readonly object m_SyncRoot = new object();
        private AppDomain m_AppDomain;


        public ApplicationInstance(ApplicationName application, string name, Version version, ApplicationParams applicationParams, AppServerContext context, IConfigurationProvider configurationProvider, ILogger logger)
        {
            Application = application;
            m_ApplicationParams = applicationParams;
            Name = name;
            Version = version;
            Logger = logger;
            m_ConfigurationProvider = configurationProvider;
            m_Context = context;
        }

        public void Start()
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if (Status == HostedAppStatus.Starting || Status==HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is "+Status);
                if (Status == HostedAppStatus.Started)
                    throw new InvalidOperationException("Instance already started");
                Status = HostedAppStatus.Starting;

                Logger.InfoFormat("Starting application {0} v{1} instance '{2}'", Application, Version, Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                                                          {
                                                              Logger.InfoFormat("Application {0} v{1} instance '{2}' started", Application, Version, Name);
                                                              createHost();
                                                              //TODO: may be it is better to move wrapping with MarshalableProxy to castle
                                                              m_ApplicationHost.Start(MarshalableProxy.Generate(m_ConfigurationProvider), m_Context);
                                                              try
                                                              {
                                                                  lock (m_SyncRoot)
                                                                  {
                                                                      Status = HostedAppStatus.Started;
                                                                  }
                                                                  m_Started.Set();
                                                              }catch(Exception e)
                                                              {
                                                                  Logger.ErrorFormat(e, "Application {0} v{1} instance '{2}' failed to start", Application, Version, Name);
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
            string path = Path.GetFullPath(new[] { m_Context.AppsDirectory, Application.Name }.Aggregate(Path.Combine));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var domain = AppDomain.CreateDomain(Application.Name, null, new AppDomainSetup
            {
                ApplicationBase = path, 
                PrivateBinPathProbe = null,
                DisallowApplicationBaseProbing = true,
                ConfigurationFile = m_ApplicationParams.ConfigFile
            });
            m_AppDomain = domain;
            m_AppDomain.Load(typeof(HostedAppInfo).Assembly.GetName());
            var appDomainInitializer = (AppDomainInitializer)m_AppDomain.CreateInstanceFromAndUnwrap(typeof(AppDomainInitializer).Assembly.Location, typeof(AppDomainInitializer).FullName, false, BindingFlags.Default, null, null, null, null);



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
                if (Status == HostedAppStatus.Started)
                    throw new InvalidOperationException("Instance already started");
                Status = HostedAppStatus.Stopping;
                Logger.InfoFormat("Stopping application {0} v{1} instance '{2}'", Application, Version, Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                                                          {
                                                              try
                                                              {
                                                                  m_ApplicationHost.Stop();
                                                                  AppDomain.Unload(m_AppDomain);
                                                                  Logger.InfoFormat("Application {0} v{1} instance '{2}' stopped", Application, Version, Name);
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                  Logger.ErrorFormat(e, "Application {0} v{1} instance '{2}' failed to stop", Application, Version,Name);
                                                              }
                                                              lock (m_SyncRoot)
                                                              {
                                                                  Status = HostedAppStatus.Stopped;
                                                              }
                                                          });
           
            }
        }

        public void Dispose()
        {
            lock (m_SyncRoot)
            {
                m_IsDisposing = true;
            }
            if (m_CurrentTask != null)
                m_CurrentTask.Wait();

            if(Status==HostedAppStatus.Started)
                m_ApplicationHost.Stop();
        }


        
    }
}