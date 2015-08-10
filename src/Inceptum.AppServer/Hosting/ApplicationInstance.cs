using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Services.Logging.NLogIntegration;
using Castle.Windsor;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.Utils;

namespace Inceptum.AppServer.Hosting
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class ApplicationInstance : IDisposable, IObservable<HostedAppStatus>, IApplicationInstance
    {
        private readonly AppServerContext m_Context;
        private readonly JobObject m_JobObject;
        private readonly object m_ServiceHostLock = new object();
        private readonly Subject<HostedAppStatus> m_StatusSubject = new Subject<HostedAppStatus>();
        private readonly object m_SyncRoot = new object();
        private Version m_ActualVersion;
        private ChannelFactory<IApplicationHost> m_AppHostFactory;
        private IApplicationHost m_ApplicationHost;
        private Task m_CurrentTask;
        private string m_DefaultConfiguration;
        private bool m_IsDisposing;
        private LoggerLevel m_LogLevel;
        private LogLimitReachedAction m_LogLimitReachedAction;
        private long m_MaxLogSize;
        private string m_Password;
        private Process m_Process;
        private ServiceHost m_ServiceHost;
        private HostedAppStatus m_Status;
        private string m_User;
        private readonly CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();
        private readonly AutoResetEvent m_HostRegisteredEvent = new AutoResetEvent(false);
        public ApplicationInstance(string name, AppServerContext context, ILogger logger, JobObject jobObject)
        {
            var completionSource = new TaskCompletionSource<object>();
            completionSource.SetResult(null);
            m_CurrentTask = completionSource.Task;

            m_JobObject = jobObject;
            Name = name;
            Logger = logger;
            m_Context = context;
            resetIpcHost();
        }

        public string Name { get; set; }

        public string UrlSafeInstanceName
        {
            get
            {
                return WebUtility.UrlEncode(Name);
            }
        }
        private string Environment { get; set; }
        private ILogger Logger { get; set; }

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
                Logger.DebugFormat("Instance '{0}' status changed to {1}", Name, value);
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

        internal InstanceCommand[] Commands { get; private set; }

        public void ReportFailure(string error)
        {
            Logger.ErrorFormat("Instance '{0}' crashed: {1}", Name, error);
            lock (m_SyncRoot)
            {
                m_CurrentTask = doStop();
            }
        }

        public void RegisterApplicationHost(string uri, InstanceCommand[] instanceCommands)
        {
            m_AppHostFactory = new ChannelFactory<IApplicationHost>(WcfHelper.CreateUnlimitedQuotaNamedPipeLineBinding(), new EndpointAddress(uri));
            IApplicationHost applicationHost = m_AppHostFactory.CreateChannel();
            Commands = instanceCommands;
            m_ApplicationHost = applicationHost;

            EventHandler clientFault = null;
            clientFault = (sender, e) =>
            {
                ((ICommunicationObject)m_ApplicationHost).Faulted -= clientFault;
                m_ApplicationHost = m_AppHostFactory.CreateChannel();
                ((ICommunicationObject)m_ApplicationHost).Faulted += clientFault;

            };
            ((ICommunicationObject)m_ApplicationHost).Faulted += clientFault;

            lock (m_SyncRoot)
            {
                Status = HostedAppStatus.Started;
            }
            Logger.InfoFormat("Instance {0} started", Name);
            m_HostRegisteredEvent.Set();

        }


        public InstanceParams GetInstanceParams()
        {
            return new InstanceParams
            {
                AppServerContext = m_Context,
                Environment = Environment,
                LogLevel = m_LogLevel.ToString(),
                DefaultConfiguration = m_DefaultConfiguration,
                LogLimitReachedAction = m_LogLimitReachedAction,
                MaxLogSize = m_MaxLogSize,
                AssembliesToLoad = new Dictionary<string, string>
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

        #region IObservable<HostedAppStatus> Members

        public IDisposable Subscribe(IObserver<HostedAppStatus> observer)
        {
            return m_StatusSubject.Subscribe(observer);
        }

        #endregion

        #region IDisposable Members

        public async void Dispose()
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

            m_CancellationTokenSource.Cancel();
            await m_CurrentTask;
            finishDispose();
        }

        #endregion


        public void KillProcess()
        {
            lock (m_SyncRoot)
            {
                Logger.InfoFormat("Killing process of instance '{0}'", Name);
                if (m_Process != null && !m_Process.HasExited)
                {
                    m_Process.Kill();

                }
            }
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
                serviceHost.AddServiceEndpoint(typeof(IApplicationInstance),
                    new NetNamedPipeBinding { ReceiveTimeout = TimeSpan.MaxValue, SendTimeout = TimeSpan.MaxValue },
                    new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id + "/instances/" + UrlSafeInstanceName));
                serviceHost.Faulted += (o, args) =>
                {
                    Logger.DebugFormat("Creating Host.");
                    resetIpcHost();
                };
                serviceHost.Open();
                m_ServiceHost = serviceHost;
            }
        }


        public void UpdateConfig(Version actualVersion, string environment, string user, string password, LoggerLevel logLevel, string defaultConfiguration,
            long maxLogSize, LogLimitReachedAction logLimitReachedAction)
        {
            m_MaxLogSize = maxLogSize;
            m_LogLimitReachedAction = logLimitReachedAction;
            m_DefaultConfiguration = defaultConfiguration;
            m_LogLevel = logLevel;
            m_ActualVersion = actualVersion;
            m_Password = password;
            m_User = user;
            Environment = environment;
        }


        public Task Start(bool debug, Action beforeStart)
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

                Logger.InfoFormat("Scheduling starting instance '{0}'. Debug mode: {1}", Name, debug);
                m_CurrentTask = doStart(debug, beforeStart);
                return m_CurrentTask;
            }
        }





        private async Task doStart(bool debug, Action beforeStart)
        {
            await m_CurrentTask;
            await Task.Yield();
            if (m_CancellationTokenSource.IsCancellationRequested) return;
            
            Logger.InfoFormat("Starting instance '{0}'. Debug mode: {1}", Name, debug);            
            try
            {
                if (beforeStart != null)
                    beforeStart();

                createHost(debug);

                while (!m_HostRegisteredEvent.WaitOne(300) && m_Process != null && !m_Process.HasExited)
                {
                    Logger.DebugFormat("Waiting for instance '{0}' hosting process to start...", Name);
                }
            }
            catch (Exception e)
            {
                Commands = new InstanceCommand[0];
                Logger.ErrorFormat(e, "Instance '{0}' failed to start", Name);
                lock (m_SyncRoot)
                {
                    m_CurrentTask = doStop();
                }
            }


        }


        public Task Stop(bool abort)
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if ((Status == HostedAppStatus.Starting && !abort) || Status == HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is " + Status);
                if (Status == HostedAppStatus.Stopped)
                    throw new InvalidOperationException("Instance is not started");
                Commands = new InstanceCommand[0];

                Status = HostedAppStatus.Stopping;
                Logger.InfoFormat("Scheduling stopping instance '{0}'", Name);
                m_CurrentTask = doStop();
                return m_CurrentTask;
            }
        }

        private async Task doStop()
        {
            await m_CurrentTask;
            await Task.Yield();

            Logger.InfoFormat("Stopping instance '{0}'", Name);
            
            try
            {
                if (m_ApplicationHost != null && !m_Process.HasExited)
                    m_ApplicationHost.Stop();
                if (m_AppHostFactory != null)
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
                if (m_Process != null && !m_Process.HasExited)
                    m_Process.WaitForExit();

                m_Process = null;
                m_ApplicationHost = null;
                m_AppHostFactory = null;
            }
            lock (m_SyncRoot)
            {
                Status = HostedAppStatus.Stopped;
                Logger.InfoFormat("Instance '{0}' stopped", Name);
            }
        }

        private void createHost(bool debug)
        {
            string path = Path.GetFullPath(Path.Combine(m_Context.AppsDirectory, Name));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string args = "\""+Name+"\"";
            /*

                        foreach (var configFile in m_ApplicationParams.ConfigFiles)
                        {
                            copyConfig(path, configFile, Path.GetFileName(configFile));
                        }
            */
            if (debug)
                args += " -debug";

            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "";
            var procSetup = new ProcessStartInfo
            {
                FileName = Path.Combine(directoryName, "AppHost", "AppHost.exe"),
                Arguments = args,
                WorkingDirectory = path,
            };

            if (!string.IsNullOrWhiteSpace(m_User))
            {
                procSetup.UserName = m_User;
                byte[] decryptedBytes = ProtectedData.Unprotect(Convert.FromBase64String(m_Password), new byte[0], DataProtectionScope.LocalMachine);
                var pass = new SecureString();
                foreach (char c in Encoding.UTF8.GetString(decryptedBytes))
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

            m_HostRegisteredEvent.Reset();
            m_Process = Process.Start(procSetup);
            m_JobObject.AddProcess(m_Process);
        }

        private void copyConfig(string path, string providedConfig, string configFileName)
        {
            string configPath = Path.Combine(path, configFileName);
            string defaultConfigPath = Path.Combine(path, configFileName + ".default");
            if (providedConfig != null && File.Exists(providedConfig))
            {
                if (!File.Exists(configPath))
                    File.Copy(providedConfig, configPath);
                File.Copy(providedConfig, defaultConfigPath, true);
            }
        }

        public void Rename(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            lock (m_SyncRoot)
            {
                Name = name;
            }
        }

        public Task<string> ExecuteCommand(InstanceCommand command)
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    throw new ObjectDisposedException("Instance is being disposed");
                if (Status == HostedAppStatus.Stopped || Status == HostedAppStatus.Stopping)
                    throw new InvalidOperationException("Instance is " + Status);

                Logger.InfoFormat("Scheduling command '{0}' execution with  instance '{1}'", command.Name, Name);

                var executeTask = doExecute(command, m_CurrentTask);
                m_CurrentTask = safeTask(executeTask);

                return executeTask;
            }
        }

        private async Task safeTask(Task task)
        {
            try
            {
                await task;
            }            
            catch (Exception)
            {                      
            }
        }

        private async Task<string> doExecute(InstanceCommand command, Task currentTask)
        {
            await Task.Yield();
            await currentTask;
            m_CancellationTokenSource.Token.ThrowIfCancellationRequested();
            Logger.InfoFormat("Executing command '{0}' with  instance '{1}'", command.Name, Name);

            InstanceCommand cmd = Commands.FirstOrDefault(c => c.Name == command.Name);
            if (cmd == null)
                throw new InvalidOperationException(string.Format("Command '{0}' not found", command.Name));
            return m_ApplicationHost.Execute(command);
        }

        public Task<object> ChangeLogLevel(string logLevel)
        {
            lock (m_SyncRoot)
            {
                if (m_IsDisposing)
                    return Task.FromResult<object>(null);
                if (Status == HostedAppStatus.Stopped || Status == HostedAppStatus.Stopping)
                    return Task.FromResult<object>(null);

                Logger.InfoFormat("Scheduling log level change to '{0}' for instance '{1}'", logLevel, Name);

                var changeLogLevelTask = doChangeLogLevel(logLevel, m_CurrentTask);
                m_CurrentTask = safeTask(changeLogLevelTask);

                return changeLogLevelTask;
            }
        }
        private async Task<object> doChangeLogLevel(string logLevel, Task currentTask)
        {
            await Task.Yield();
            await currentTask;
            m_CancellationTokenSource.Token.ThrowIfCancellationRequested();
            Logger.InfoFormat("Changing log level to '{0}' for instance '{1}'", logLevel, Name);

         
            m_ApplicationHost.ChangeLogLevel(logLevel);
            return null;
        }

        public void VerifySate()
        {
            lock (m_SyncRoot)
            {
                if ((Status != HostedAppStatus.Started && Status != HostedAppStatus.Starting) || m_Process == null || !m_Process.HasExited)
                    return;
                Logger.ErrorFormat("Instance '{0}' process has unexpectedly stopped", Name);
                lock (m_SyncRoot)
                {
                    m_CurrentTask = doStop();
                }
            }
        }
    }
}