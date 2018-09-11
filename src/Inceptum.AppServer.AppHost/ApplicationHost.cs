using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Inceptum.AppServer.AppHost.Configuration;
using Inceptum.AppServer.AppHost.Container;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.AppHost.Interop;
using Inceptum.AppServer.AppHost.Logging.Targets;
using Inceptum.AppServer.AppHost.Wcf;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Inceptum.AppServer.Hosting
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    internal class ApplicationHost : IApplicationHost 
    {
        private IDisposable m_Container;
        private IHostedApplication m_HostedApplication;
        private ILogCache m_LogCache;
        private IConfigurationProvider m_ConfigurationProvider;
        private string m_Environment;
        private readonly string m_InstanceName;
        private AppServerContext m_Context;
        private readonly ManualResetEvent m_StopEvent = new ManualResetEvent(false);
        private Dictionary<AssemblyName, Lazy<Assembly>> m_LoadedAssemblies;
        private IApplicationInstance m_Instance;
        private ServiceHost m_ServiceHost;
        private InstanceContext m_InstanceContext;
        private readonly string m_ServiceAddress;
        private readonly object m_ServiceHostLock = new object();
        private LoggingConfiguration m_LoggingConfig;

        public ApplicationHost(string instanceName)
        {
            m_InstanceName = instanceName;
            m_ServiceAddress = new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id + "/" + WebUtility.UrlEncode(m_InstanceName)).ToString();
        }

        /// <exception cref="InvalidOperationException"></exception>
        public void Run()
        {
            createServiceHost();

            var uri = "net.pipe://localhost/AppServer/" + WndUtils.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/instances/" + WebUtility.UrlEncode(m_InstanceName);
            var factory = new ChannelFactory<IApplicationInstance>(WcfHelper.CreateUnlimitedQuotaNamedPipeLineBinding(), new EndpointAddress(uri));
            m_Instance = factory.CreateChannel();
            var instanceParams = m_Instance.GetInstanceParams();

            AppDomain.CurrentDomain.UnhandledException += onUnhandledException;

            m_Environment = instanceParams.Environment;
            m_Context = instanceParams.AppServerContext;
            m_InstanceContext = new InstanceContext
            {
                Name = m_InstanceName,
                AppServerName = instanceParams.AppServerContext.Name,
                Environment = m_Environment,
                DefaultConfiguration = instanceParams.DefaultConfiguration
            };

            IEnumerable<AssemblyName> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName());

            var folder = Path.Combine(m_Context.AppsDirectory, m_InstanceName, "bin");
            var dlls = new[] { "*.dll", "*.exe" }
                .SelectMany(searchPattern => Directory.GetFiles(folder, searchPattern, SearchOption.AllDirectories))
                .Select(file => new { path = file, AssemblyDefinition = AssemblyDefinitionFactory.ReadAssemblySafe(file) }).ToArray();

            var injectedAssemblies = instanceParams.AssembliesToLoad;
            m_LoadedAssemblies = dlls.Where(d => d.AssemblyDefinition != null)
                                     .Where(asm => loadedAssemblies.All(a => a.Name != asm.AssemblyDefinition.Name.Name))
                                     .Where(asm => injectedAssemblies.All(a => a.Key != asm.AssemblyDefinition.Name.Name))
                                     .ToDictionary(asm => new AssemblyName(asm.AssemblyDefinition.FullName), asm => new Lazy<Assembly>(() => loadAssembly(asm.path)));
            foreach (var injectedAssembly in injectedAssemblies)
            {
                var path = injectedAssembly.Value;
                var assemblyName = injectedAssembly.Key;
                m_LoadedAssemblies.Add(new AssemblyName(assemblyName), new Lazy<Assembly>(() => loadAssembly(path)));
            }

            foreach (string dll in dlls.Where(d => d.AssemblyDefinition == null).Select(d => d.path))
            {
                if (WndUtils.LoadLibrary(dll) == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to load unmanaged dll " + Path.GetFileName(dll) + " from package");
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
            createCongigurationProviderProxy();
            createLogCacheProxy();


            var appAssemblies =
                dlls.Select(file => new { file, asm = file.AssemblyDefinition })
                    .Where(@t => @t.asm != null)
                    .Select(@t => new { @t, attribute = @t.asm.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(HostedApplicationAttribute).FullName) })
                    .Where(@t => @t.attribute != null)
                    .Select(@t => @t.@t.asm);

            var appTypes = appAssemblies.SelectMany(
                                    a => a.MainModule.Types.Where(t => t.Interfaces.Any(i => i.FullName == typeof(IHostedApplication).FullName))
                                        .Select(t => t.FullName + ", " + a.FullName)
                                    ).ToArray();

            if (appTypes.Length > 1)
                throw new InvalidOperationException(string.Format("Instance {0} bin folder contains several types implementing IHostedApplication: {1}", m_InstanceName, string.Join(",", appTypes)));

            if (appTypes.Length == 0)
                throw new InvalidOperationException(string.Format("Instance {0} bin folder does not contain type implementing IHostedApplication", m_InstanceName));

            string appType = appTypes[0];

            var instanceCommands = initContainer(Type.GetType(appType), instanceParams.LogLevel, instanceParams.MaxLogSize, instanceParams.LogLimitReachedAction);

            m_Instance.RegisterApplicationHost(m_ServiceAddress, instanceCommands);

            m_StopEvent.WaitOne();
            if (m_Container == null)
            {
                throw new InvalidOperationException("Host is not started");
            }

            if (m_ServiceHost != null)
            {
                try
                {
                    m_ServiceHost.Close();
                }
                catch
                {
                    //There is nothing to do with it
                }
            }

            m_Container.Dispose();
            if (m_ConfigurationProvider != null)
            {
                try
                {

                    ((ICommunicationObject)m_ConfigurationProvider).Close();
                }
                catch
                {
                    //There is nothing to do with it
                }
            }

            if (m_LogCache != null)
            {
                try
                {
                    ((ICommunicationObject)m_LogCache).Close();
                }
                catch
                {
                    //There is nothing to do with it
                }
            }

            m_Container = null;
            m_HostedApplication = null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            m_StopEvent.Set();
        }

        public string Execute(InstanceCommand command)
        {
            var methodInfo = m_HostedApplication.GetType().GetMethod(command.Name);
            var result = methodInfo.Invoke(m_HostedApplication, methodInfo.GetParameters().Select(p => parseCommandParameterValue(p, command)).ToArray());
            return result == null ? null : result.ToString();
        }

        public void ChangeLogLevel(string level)
        {
            var logLevel = mapLogLevel(level);
            foreach (var l in new[] { LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal })
            {
                foreach (var rule in m_LoggingConfig.LoggingRules)
                {
                    if (l < logLevel)
                        rule.DisableLoggingForLevel(l);
                    else
                        rule.EnableLoggingForLevel(l);
                }

            }
            LogManager.ReconfigExistingLoggers();
        }

        public void Debug()
        {
            Debugger.Launch();
        }

        private void resetServiceHost(object sender, EventArgs e)
        {
            lock (m_ServiceHostLock)
            {
                if (m_ServiceHost != null)
                {
                    m_ServiceHost.Faulted -= resetServiceHost;
                    m_ServiceHost.Close();
                    m_ServiceHost = null;
                }
                createServiceHost();
            }
        }
 
        private void createServiceHost()
        {
            lock (m_ServiceHostLock)
            {
                m_ServiceHost = new ServiceHost(this);


                var debug = m_ServiceHost.Description.Behaviors.Find<ServiceDebugBehavior>();

                // if not found - add behavior with setting turned on 
                if (debug == null)
                    m_ServiceHost.Description.Behaviors.Add(new ServiceDebugBehavior {IncludeExceptionDetailInFaults = true});
                else
                    debug.IncludeExceptionDetailInFaults = true;
                //TODO: need to do it in better way. String based type resolving is a bug source
                m_ServiceHost.AddServiceEndpoint(typeof (IApplicationHost), WcfHelper.CreateUnlimitedQuotaNamedPipeLineBinding(), m_ServiceAddress);


                m_ServiceHost.Open();
                m_ServiceHost.Faulted += resetServiceHost;
            }
        }

        private void onUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            m_Instance.ReportFailure(args.ExceptionObject.ToString());
        }

        private static Assembly loadAssembly(string path)
        {
            return Assembly.LoadFile(Path.GetFullPath(path));
        }

        private Assembly onAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var requestedAssemblyName = new AssemblyName(args.Name);

            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = loadedAssemblies.FirstOrDefault(a => a.FullName == args.Name || a.GetName().Name == requestedAssemblyName.Name || a.GetName().Name == args.Name);

            if (assembly != null)
            {
                return assembly;
            }

            assembly = m_LoadedAssemblies
                .Where(asm => asm.Key.Name == requestedAssemblyName.Name)
                .OrderByDescending(asm => asm.Key.Version)
                .Select(asm => asm.Value.Value).FirstOrDefault();

            return assembly;
        }

        private void createCongigurationProviderProxy()
        {
            var uri = "net.pipe://localhost/AppServer/" + WndUtils.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/ConfigurationProvider";
            var factory = new ChannelFactory<IConfigurationProvider>(WcfHelper.CreateUnlimitedQuotaNamedPipeLineBinding(),
                new EndpointAddress(uri));
            m_ConfigurationProvider = factory.CreateChannel();

            EventHandler clientFault = null;
            clientFault = (sender, e) =>
            {
                ((ICommunicationObject)m_ConfigurationProvider).Faulted -= clientFault;
                m_ConfigurationProvider = factory.CreateChannel();
                ((ICommunicationObject)m_ConfigurationProvider).Faulted += clientFault;

            };
            ((ICommunicationObject)m_ConfigurationProvider).Faulted += clientFault;
        }

        private void createLogCacheProxy()
        {
            var uri = "net.pipe://localhost/AppServer/" + WndUtils.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/LogCache";
            var factory = new ChannelFactory<ILogCache>(WcfHelper.CreateUnlimitedQuotaNamedPipeLineBinding(), new EndpointAddress(uri));
            m_LogCache= factory.CreateChannel();

            EventHandler clientFault = null;
            clientFault = (sender, e) =>
            {
                ((ICommunicationObject) m_LogCache).Faulted -= clientFault;
                m_LogCache = factory.CreateChannel();
                ((ICommunicationObject)m_LogCache).Faulted += clientFault;

            };
            ((ICommunicationObject)m_LogCache).Faulted += clientFault;
        }

        private object parseCommandParameterValue(ParameterInfo parameter,InstanceCommand command)
        {
            var instanceCommandParam = command.Parameters.FirstOrDefault(p => p.Name == parameter.Name);
            if (instanceCommandParam == null || instanceCommandParam.Value==null)
                return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;

            return Convert.ChangeType(instanceCommandParam.Value, parameter.ParameterType);
        }

        private InstanceCommand[] initContainer(Type appType, string logLevel, long maxLogSize, LogLimitReachedAction logLimitReachedAction)
        {
            if (m_Container != null)
                throw new InvalidOperationException("Host is already started");
            var container = new WindsorContainer();
            try
            {
                //castle config
                var appName = AppDomain.CurrentDomain.FriendlyName;
                var configurationFile = string.Format("castle.{0}.config", appName);
                if (File.Exists(configurationFile))
                {
                    container
                        .Install(Castle.Windsor.Installer.Configuration.FromXmlFile(configurationFile));
                }

                container.Register(
                   Component.For<InstanceContext>().Instance(m_InstanceContext),
                   Component.For<ILogCache>().Instance(m_LogCache).Named("LogCache"),
                   Component.For<ManagementConsoleTarget>().DependsOn(new { source = m_InstanceName })
                   );


                var logFolder = new[] { m_Context.BaseDirectory, "logs", m_InstanceName }.Aggregate(Path.Combine);
                var oversizedLogFolder = new[] { m_Context.BaseDirectory, "logs.oversized", m_InstanceName }.Aggregate(Path.Combine);
                GlobalDiagnosticsContext.Set("logfolder", logFolder);
                GlobalDiagnosticsContext.Set("oversizedLogFolder", oversizedLogFolder);
                ConfigurationItemFactory.Default.Targets.RegisterDefinition("ManagementConsole", typeof(ManagementConsoleTarget));
                var createInstanceOriginal = ConfigurationItemFactory.Default.CreateInstance;
                ConfigurationItemFactory.Default.CreateInstance = type => container.Kernel.HasComponent(type) ? container.Resolve(type) : createInstanceOriginal(type);

                string nlogConfigPath = Path.GetFullPath("nlog.config");
                if (!File.Exists(nlogConfigPath))
                    nlogConfigPath = null;
                container
                    .AddFacility<LoggingFacility>(f => f.LogUsing(new GenericsAwareNLoggerFactory(
                        nlogConfigPath,
                        config => updateLoggingConfig(config, logLevel, maxLogSize, logLimitReachedAction))));
                container
                    .Register(
                        Component.For<AppServerContext>().Instance(m_Context),
                        Component.For<IConfigurationProvider>().Named("ConfigurationProvider")
                                 .ImplementedBy<InstanceAwareConfigurationProviderWrapper>()
                                 .DependsOn(new
                                 {
                                     configurationProvider = m_ConfigurationProvider,
                                     instanceParams = new
                                     {
                                         environment = m_Environment,
                                         appName,
                                         machineName = Environment.MachineName,
                                         instance = m_InstanceName,
                                     }
                                 })
                    );
                container
                    .Install(FromAssembly.Instance(appType.Assembly, new PluginInstallerFactory()));
                container
                    .Register(Component.For<IHostedApplication>().ImplementedBy(appType));


                m_HostedApplication = container.Resolve<IHostedApplication>();
                var allowedTypes = new[] { typeof(string), typeof(int), typeof(DateTime), typeof(decimal), typeof(bool) };
                var commands =
                    m_HostedApplication.GetType()
                                     .GetMethods()
                                     .Where(m => m.Name != "Start" && m.Name != "ToString" && m.Name != "GetHashCode" && m.Name != "GetType")
                                     .Where(m => m.GetParameters().All(p => allowedTypes.Contains(p.ParameterType)))
                                     .Select(m => new InstanceCommand(m.Name, m.GetParameters().Select(p => new InstanceCommandParam { Name = p.Name, Type = p.ParameterType.Name }).ToArray()));
                m_HostedApplication.Start();
                container.Register(Component.For<MisconfiguredComponentsLogger>());
                container.Resolve<MisconfiguredComponentsLogger>().Log(container.Kernel);
                m_Container = container;
                return commands.ToArray();
            }
            catch (Exception e)
            {
                try
                {
                    if (container != null)
                    {
                        container.Dispose();
                        m_Container = null;
                    }
                }
                catch (Exception e1)
                {
                    throw new ApplicationException(
                        string.Format("Failed to start: {0}{1}Exception while disposing container: {2}", e,
                                      Environment.NewLine, e1));
                }
                string[] strings = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name + " " + a.GetName().Version).OrderBy(a => a).ToArray();
                throw new ApplicationException(string.Format("Failed to start: {0}", e));
            }
        }

        private void updateLoggingConfig(LoggingConfiguration config, string logLevel, long maxLogSize, LogLimitReachedAction logLimitReachedAction)
        {
            m_LoggingConfig = config;
            var minLogLevel = mapLogLevel(logLevel);

            var fileTarget = new FileTarget
            {
                Encoding = Encoding.UTF8,
                FileName = Path.Combine(m_Context.BaseDirectory, "logs", m_InstanceName, "${shortdate}.log"),
                ArchiveFileName = Path.Combine(m_Context.BaseDirectory, "logs.oversized", m_InstanceName, "${shortdate}.{#####}.log"),
                Layout = "${longdate} ${uppercase:inner=${pad:padCharacter= :padding=-5:inner=${level}}} [${threadid}][${threadname}] [${logger:shortName=true}] ${message} ${exception:format=tostring}"
            };
            if (maxLogSize > 0)
            {
                fileTarget.ArchiveAboveSize = maxLogSize;
                fileTarget.ArchiveEvery = FileArchivePeriod.Minute;
                fileTarget.ArchiveNumbering = ArchiveNumberingMode.Rolling;
                switch (logLimitReachedAction)
                {
                    case LogLimitReachedAction.LogToOversizedFolder:
                        fileTarget.MaxArchiveFiles = int.MaxValue;
                        break;
                    case LogLimitReachedAction.StopLogging:
                        fileTarget.MaxArchiveFiles = 0;
                        break;
                }

            }
            Target logFile = new AsyncTargetWrapper(fileTarget);
            config.AddTarget("logFile", logFile);
            var rule = new LoggingRule("*", minLogLevel, logFile);
            config.LoggingRules.Add(rule);


            var coloredConsoleTarget = new ColoredConsoleTarget
            {
                Layout = @"${date:format=HH\:mm\:ss.fff} ${uppercase:inner=${pad:padCharacter= :padding=-5:inner=${level}}} [${threadid}][${threadname}] [${logger:shortName=true}] ${message} ${exception:format=tostring}",
                UseDefaultRowHighlightingRules = false
            };
            coloredConsoleTarget.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Debug", ForegroundColor = ConsoleOutputColor.DarkGray });
            coloredConsoleTarget.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Info", ForegroundColor = ConsoleOutputColor.White });
            coloredConsoleTarget.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Warn", ForegroundColor = ConsoleOutputColor.Yellow });
            coloredConsoleTarget.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Error", ForegroundColor = ConsoleOutputColor.Red });
            coloredConsoleTarget.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Fatal", ForegroundColor = ConsoleOutputColor.Red, BackgroundColor = ConsoleOutputColor.White });

            Target console = new AsyncTargetWrapper(coloredConsoleTarget);
            config.AddTarget("console", console);
            rule = new LoggingRule("*", minLogLevel, console);
            config.LoggingRules.Add(rule);

            Target managementConsole = new ManagementConsoleTarget(m_LogCache, m_InstanceName)
            {
                Layout = @"${pad:padCharacter= :padding=-20:inner=${gdc:AppServer.Instance}}: ${date:format=HH\:mm\:ss.fff} ${uppercase:inner=${pad:padCharacter= :padding=-5:inner=${level}}} [${threadid}][${threadname}] [${logger:shortName=true}] ${message} ${exception:format=tostring}"
            };

            config.AddTarget("managementConsole", managementConsole);
            rule = new LoggingRule("*", minLogLevel, managementConsole);
            config.LoggingRules.Add(rule);

            GlobalDiagnosticsContext.Set("AppServer.Instance", m_InstanceName);
        }

        LogLevel mapLogLevel(string logLevel)
        {
            try
            {
                return LogLevel.FromString(logLevel);
            }
            catch
            {
                return LogLevel.Debug;
            }
        }
    }
}