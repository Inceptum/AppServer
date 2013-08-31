using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Windsor;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Inceptum.AppServer.Hosting
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,IncludeExceptionDetailInFaults = true)]
    internal class ApplicationHost<TApp> : IApplicationHost where TApp : IHostedApplication
    {
        private WindsorContainer m_Container;
        private IHostedApplication m_HostedApplication;
        private readonly ILogCache m_LogCache;
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly string m_Environment;
        private readonly string m_InstanceName;
        private readonly AppServerContext m_Context;

        public ApplicationHost(ILogCache logCache, IConfigurationProvider configurationProvider, string environment, string instanceName, AppServerContext context)
        {
            m_LogCache = logCache;
            m_ConfigurationProvider = configurationProvider;
            m_Environment = environment;
            m_InstanceName = instanceName;
            m_Context = context;
        }


        #region IApplicationHost2 Members

        [MethodImpl(MethodImplOptions.Synchronized)]
        public InstanceCommand[] Start()
        {
            if (m_Container != null)
                throw new InvalidOperationException("Host is already started");
            try
            {

                m_Container = new WindsorContainer();
                //castle config
                var appName = AppDomain.CurrentDomain.FriendlyName;
                var configurationFile = string.Format("castle.{0}.config", appName);
                if (File.Exists(configurationFile))
                {
                    m_Container
                        .Install(Castle.Windsor.Installer.Configuration.FromXmlFile(configurationFile));
                }

                m_Container.Register(
                   Component.For<ILogCache>().Instance(m_LogCache).Named("LogCache"),
                   Component.For<ManagementConsoleTarget>().DependsOn(new { source = m_InstanceName })
                   );


                var logFolder = new[] { m_Context.BaseDirectory, "logs", m_InstanceName }.Aggregate(Path.Combine);
                GlobalDiagnosticsContext.Set("logfolder",logFolder);
                ConfigurationItemFactory.Default.Targets.RegisterDefinition("ManagementConsole", typeof(ManagementConsoleTarget));
                var createInstanceOriginal = ConfigurationItemFactory.Default.CreateInstance;
                ConfigurationItemFactory.Default.CreateInstance = type => m_Container.Kernel.HasComponent(type) ? m_Container.Resolve(type) : createInstanceOriginal(type);

                m_Container
                    .AddFacility<LoggingFacility>(f => f.LogUsing(new GenericsAwareNLoggerFactory(
                        null,//Path.Combine(m_Context.BaseDirectory, "nlog.config"),
                        updateLoggingConfig)))
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
                    )
                    .Install(FromAssembly.Instance(typeof(TApp).Assembly, new PluginInstallerFactory()))
                    .Register(Component.For<IHostedApplication>().ImplementedBy<TApp>());


                m_HostedApplication = m_Container.Resolve<IHostedApplication>();
                var allowedTypes = new []{typeof(string),typeof(int),typeof(DateTime),typeof(decimal),typeof(bool)};
                var commands =
                    m_HostedApplication.GetType()
                                     .GetMethods()
                                     .Where(m => m.Name != "Start" && m.Name != "ToString" && m.Name != "GetHashCode" && m.Name != "GetType")
                                     .Where(m => m.GetParameters().All(p => allowedTypes.Contains(p.ParameterType)))
                                     .Select(m => new InstanceCommand( m.Name,m.GetParameters().Select(p => new InstanceCommandParam{Name = p.Name,Type = p.ParameterType.Name}).ToArray()));
                m_HostedApplication.Start();
                return commands.ToArray();
            }
            catch (Exception e)
            {
                try
                {
                    if (m_Container != null)
                    {
                        m_Container.Dispose();
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

        private void updateLoggingConfig(LoggingConfiguration config)
        {
            Target logFile = new AsyncTargetWrapper(new FileTarget
            {
                FileName = Path.Combine(m_Context.BaseDirectory, "logs", m_InstanceName, "${shortdate}.log"),
                Layout = "${longdate} ${uppercase:inner=${pad:padCharacter= :padding=-5:inner=${level}}} [${threadid}][${threadname}] [${logger:shortName=true}] ${message} ${exception:format=tostring}"
            });
            config.AddTarget("logFile", logFile);
            var rule = new LoggingRule("*", LogLevel.Debug, logFile);
            config.LoggingRules.Add(rule);


            Target console = new AsyncTargetWrapper(new ConsoleTarget
            {
                Layout = "${longdate} ${uppercase:inner=${pad:padCharacter= :padding=-5:inner=${level}}} [${threadid}][${threadname}] [${logger:shortName=true}] ${message} ${exception:format=tostring}"
            });
            config.AddTarget("console", console);
            rule = new LoggingRule("*", LogLevel.Debug, console);
            config.LoggingRules.Add(rule);

            Target managementConsole = new ManagementConsoleTarget(m_LogCache, m_InstanceName)
            {
                Layout = @"${date:format=HH\:mm\:ss.fff} ${level} [${threadid}][${threadname}] [" + m_InstanceName + ".${logger:shortName=true}] ${message} ${exception:format=tostring}"
            };
            config.AddTarget("managementConsole", managementConsole);
            rule = new LoggingRule("*", LogLevel.Debug, managementConsole);
            config.LoggingRules.Add(rule);
        }




        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            if (m_Container == null)
                throw new InvalidOperationException("Host is not started");
            m_Container.Dispose();
            m_Container = null;
            m_HostedApplication = null;
        }

        public string Execute(InstanceCommand command)
        {
            var methodInfo = m_HostedApplication.GetType().GetMethod(command.Name);
            var result=methodInfo.Invoke(m_HostedApplication,
                              methodInfo.GetParameters().Select(p=>parseCommandParameterValue(p,command)).ToArray());
            return result == null ? null : result.ToString();
        }

        private object parseCommandParameterValue(ParameterInfo parameter,InstanceCommand command)
        {
            var instanceCommandParam = command.Parameters.FirstOrDefault(p => p.Name == parameter.Name);
            if (instanceCommandParam == null || instanceCommandParam.Value==null)
                return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;

            return Convert.ChangeType(instanceCommandParam.Value, parameter.ParameterType);
        }

        #endregion

        

        private class InstanceAwareConfigurationProviderWrapper:IConfigurationProvider
        {
            private readonly IConfigurationProvider m_ConfigurationProvider;
            private readonly IDictionary<string, string> m_InstanceParams;
            private readonly Regex m_InstanceParamRegex;

            public InstanceAwareConfigurationProviderWrapper(IConfigurationProvider configurationProvider, object instanceParams)
            {
                if (configurationProvider == null) throw new ArgumentNullException("configurationProvider");
                if (instanceParams == null) throw new ArgumentNullException("instanceParams");
                m_ConfigurationProvider = configurationProvider;
                m_InstanceParams = extractParamValues(instanceParams);
                m_InstanceParamRegex = createRegex(m_InstanceParams);
            }

            //TODO[MT]: methods (replaceParams, createRegex and extractParamValues) should be moved to some internal "Utils" namespace (same logic is used inside ConfigurationFacility)
            private static Regex createRegex(IDictionary<string, string> paramsDictionary)
            {
                var r = string.Format("^{0}$", string.Join("|", paramsDictionary.Keys.Select(x => "\\{" + x + "\\}")));
                return new Regex(r, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }

            private static Dictionary<string, string> extractParamValues(object values)
            {
                var vals = values
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanRead)
                    .Select(property => new { key = property.Name, value = property.GetValue(values, null) });


                return vals.ToDictionary(o => o.key, o => (o.value ?? "").ToString());
            }

            private string replaceParams(string paramName, bool strict = true)
            {
                if (!strict && (m_InstanceParamRegex == null || !m_InstanceParamRegex.IsMatch(paramName)))
                    return paramName;
                return m_InstanceParams.Aggregate(paramName,
                                          (current, param) =>
                                          current.Replace(string.Format("{{{0}}}", param.Key), param.Value));
            }

            public string GetBundle(string configuration, string bundleName, params string[] parameters)
            {
                var extraParams = parameters.Select(x => replaceParams(x)).ToArray();
                bundleName = replaceParams(bundleName, false);

                return m_ConfigurationProvider.GetBundle(configuration, bundleName, extraParams);
            }
        }
    }
}