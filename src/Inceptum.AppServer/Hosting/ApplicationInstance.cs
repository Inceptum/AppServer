using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using System.Xml.Schema;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Services.Logging.NLogIntegration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
     [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,IncludeExceptionDetailInFaults = true)]
    public class ApplicationInstance : IDisposable, IObservable<HostedAppStatus>, IApplicationInstance
    {
        private readonly AppServerContext m_Context;
        private readonly Subject<HostedAppStatus> m_StatusSubject = new Subject<HostedAppStatus>();
        private readonly object m_SyncRoot = new object();
        private IApplicationHost m_ApplicationHost;
        private Task m_CurrentTask;
        private bool m_IsDisposing;
        private HostedAppStatus m_Status;
        private Version m_ActualVersion;
        private ServiceHost m_ServiceHost;
        private readonly object m_ServiceHostLock=new object();
        private readonly JobObject m_JobObject;
        private Process m_Process;
        private ChannelFactory<IApplicationHost> m_AppHostFactory;
        private string m_User;
        private string m_Password;
        private LoggerLevel m_LogLevel;

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

 

         public ApplicationInstance(string name, AppServerContext context,
                                    ILogger logger, JobObject jobObject)
        {
            m_JobObject = jobObject;
            Name = name;
            Logger = logger;
            m_Context = context;
            IsMisconfigured = true;
            resetIpcHost();
        }

         private void resetIpcHost()
        {
            lock (m_ServiceHostLock)
            {
                if (m_ServiceHost != null)
                {
                    m_ServiceHost.Close();
                    m_ServiceHost = null;
                }
                var serviceHost = new ServiceHost(this);
                serviceHost.AddServiceEndpoint(typeof(IApplicationInstance), new NetNamedPipeBinding { ReceiveTimeout = TimeSpan.MaxValue, SendTimeout = TimeSpan.MaxValue }, new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id + "/instances/" + Name));
                serviceHost.Faulted += (o, args) =>
                {
                    Logger.DebugFormat("Creating Host.");
                    resetIpcHost();
                };
                serviceHost.Open();
                m_ServiceHost = serviceHost;
            }
        }



         public void UpdateConfig(Version actualVersion, string environment, string user, string password, LoggerLevel logLevel)
        {
             m_LogLevel = logLevel;
             m_ActualVersion = actualVersion;
            m_Password = password;
            m_User = user;
            Environment = environment;
            
        }




        public void Start(bool debug,Action beforeStart)
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

                Logger.InfoFormat("Starting instance '{0}'", Name);
                m_CurrentTask = Task.Factory.StartNew(() =>
                {
                    
                                                              try
                                                              {
                                                                  if (beforeStart != null)
                                                                      beforeStart();
/*
                                                                  if (IsMisconfigured)
                                                                      throw new ConfigurationErrorsException("Instance is misconfigured");
*/

                                                                  createHost(debug);
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                  Commands=new InstanceCommand[0];
                                                                  Logger.ErrorFormat(e, "Instance '{0}' failed to start", Name);
                                                                  Stop(true);
                                                              }
                                                          });
            }
        }

        internal InstanceCommand[] Commands { get; private set; }

      

        public void Stop(bool abort)
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
                            if (m_ApplicationHost != null && !m_Process.HasExited)
                                m_ApplicationHost.Stop();
                            if(m_AppHostFactory!=null)
                                m_AppHostFactory.Close();
                        }
                        catch (Exception e)
                        {
                            Logger.ErrorFormat(e, "Instance '{0}' application failed while stopping", Name);
                            if (!m_Process.HasExited)
                                m_Process.Kill();
                        }
                        finally
                        {
                            Logger.InfoFormat("Instance '{0}' stopped", Name);
                            if (m_Process!=null && !m_Process.HasExited)
                                m_Process.WaitForExit();
                                
                            m_Process = null;
                            m_ApplicationHost = null;
                            m_AppHostFactory = null;
                        }
                        lock (m_SyncRoot)
                        {
                            Status = HostedAppStatus.Stopped;
                        }
                    });
            }
        }

        private void createHost(bool debug)
        {
            string path = Path.GetFullPath(new[] { m_Context.AppsDirectory, Name }.Aggregate(Path.Combine));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var args = Name;
/*

            foreach (var configFile in m_ApplicationParams.ConfigFiles)
            {
                copyConfig(path, configFile, Path.GetFileName(configFile));
            }

            if (m_ApplicationParams.Debug)
                args += " -debug";
*/

            var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "";
            var procSetup = new ProcessStartInfo
            {
                FileName = Path.Combine(directoryName, "AppHost", "AppHost.exe"),
                Arguments = args,
                WorkingDirectory = path,
                

            };

            if (!string.IsNullOrWhiteSpace(m_User))
            {
                procSetup.UserName = m_User;
                var decryptedBytes = ProtectedData.Unprotect(Convert.FromBase64String(m_Password), new byte[0], DataProtectionScope.LocalMachine);
                var pass = new SecureString();
                foreach (var c in Encoding.UTF8.GetString(decryptedBytes))
                {
                    pass.AppendChar(c);
                }
                procSetup.Password = pass;
                procSetup.UseShellExecute = false;
            }
 

#if !DEBUG
            if (!debug)
            {
                procSetup.CreateNoWindow = true;
            }
#endif
 

            m_Process = Process.Start(procSetup);

            m_JobObject.AddProcess(m_Process);
        }

         private void copyConfig(string path, string providedConfig, string configFileName)
         {
             string configPath = Path.Combine(path, configFileName);
             string defaultConfigPath = Path.Combine(path, configFileName+".default");
             if (providedConfig != null && File.Exists(providedConfig))
             {
                 if (!File.Exists(configPath))
                     File.Copy(providedConfig, configPath);
                 File.Copy(providedConfig, defaultConfigPath, true);
             }
         }

         public void ReportFailure(string error)
         {
             Logger.ErrorFormat("Instance '{0}' crashed: {1}", Name,error);
             Stop(true);
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

        public void RegisterApplicationHost(string uri, InstanceCommand[] instanceCommands)
        {
            m_AppHostFactory = new ChannelFactory<IApplicationHost>(new NetNamedPipeBinding(), new EndpointAddress(uri));
            IApplicationHost applicationHost = m_AppHostFactory.CreateChannel();
            Commands = instanceCommands;
            m_ApplicationHost = applicationHost;
            lock (m_SyncRoot)
            {
                Status = HostedAppStatus.Started;
            }
            Logger.InfoFormat("Instance {0} started", Name);
        }

        public InstanceParams GetInstanceParams()
         {
         /*    ApplicationParams applicationParams;
             lock (m_SyncRoot)
             {
                 applicationParams = null;//m_ApplicationParams.Clone();
             }




             var assembliesToLoad = new Dictionary<string, string>()
                {
                    {typeof (AppInfo).Assembly.GetName().FullName, typeof (AppInfo).Assembly.Location},
                    //AppServer is loaded not from package, so it dependencies used in appDomain plugin should be provided aswell
                    {typeof (LoggingFacility).Assembly.GetName().FullName, typeof (LoggingFacility).Assembly.Location},
                    {typeof (ILogger).Assembly.GetName().FullName, typeof (ILogger).Assembly.Location},
                    {typeof (WindsorContainer).Assembly.GetName().FullName, typeof (WindsorContainer).Assembly.Location},
                    {typeof (NLogFactory).Assembly.GetName().FullName, typeof (NLogFactory).Assembly.Location}
                };
             foreach (var assm in applicationParams.AssembliesToLoad)
            {
                if(!assembliesToLoad.ContainsKey(assm.Key))
                    assembliesToLoad.Add(assm.Key,assm.Value);
            }
*/
            return new InstanceParams
            {
               /* ApplicationParams = new ApplicationParams
                                        (
                                            applicationParams.AppType,
                                            applicationParams.ConfigFiles.ToArray(),
                                            applicationParams.NativeDllToLoad.ToArray(),
                                            assembliesToLoad
                                        ),*/

                AppServerContext = m_Context,
                Environment = Environment,
                LogLevel = m_LogLevel.ToString() ,
                AssembliesToLoad =   new Dictionary<string, string>()
                {
                    {typeof (AppInfo).Assembly.GetName().FullName, typeof (AppInfo).Assembly.Location},
                    //AppServer is loaded not from package, so it dependencies used in appDomain plugin should be provided aswell
                    {typeof (LoggingFacility).Assembly.GetName().FullName, typeof (LoggingFacility).Assembly.Location},
                    {typeof (ILogger).Assembly.GetName().FullName, typeof (ILogger).Assembly.Location},
                    {typeof (WindsorContainer).Assembly.GetName().FullName, typeof (WindsorContainer).Assembly.Location},
                    {typeof (NLogFactory).Assembly.GetName().FullName, typeof (NLogFactory).Assembly.Location}
                }
            }; 
         }

         public void VerifySate()
         {
             lock (m_SyncRoot)
             {
                 if ((Status != HostedAppStatus.Started && Status != HostedAppStatus.Starting) || m_Process == null || !m_Process.HasExited) return;
                 Logger.ErrorFormat("Instance '{0}' process has unexpectedly stopped", Name);
                 Stop(true);
             }
         }
    }



}
