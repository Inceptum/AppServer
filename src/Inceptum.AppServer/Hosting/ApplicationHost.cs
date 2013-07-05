using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Inceptum.AppServer.Bootstrap;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Utils;
using Inceptum.AppServer.Windsor;
using Inceptum.Core.Utils;
using Inceptum.Messaging;
using NLog;
using NLog.Config;

namespace Inceptum.AppServer.Hosting
{
    internal class ApplicationHost<TApp> : MarshalByRefObject, IApplicationHost where TApp : IHostedApplication
    {
        private WindsorContainer m_Container;
        private IHostedApplication m_HostedApplication;

        internal WindsorContainer Container
        {
            get { return m_Container; }
        }


        public ApplicationHost(  )
        {
            
            Status=HostedAppStatus.Stopped;
        }

        #region IApplicationHost Members

        public HostedAppStatus Status { get; private set; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public InstanceCommandSpec[] Start(IConfigurationProvider configurationProvider, ILogCache logCache, AppServerContext context, string instanceName, string environment)
        {
            Status = HostedAppStatus.Starting;
            if (m_Container != null)
                throw new InvalidOperationException("Host is already started");
            try
            {
                AppDomainRenderer.Register();

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
                   Component.For<ILogCache>().Instance(logCache).Named("LogCache"),
                   Component.For<ManagementConsoleTarget>().DependsOn(new { source = instanceName })
                   );


                var logFolder = new[] { context.BaseDirectory, "logs", instanceName }.Aggregate(Path.Combine);
                GlobalDiagnosticsContext.Set("logfolder",logFolder);
                ConfigurationItemFactory.Default.Targets.RegisterDefinition("ManagementConsole", typeof(ManagementConsoleTarget));
                var createInstanceOriginal = ConfigurationItemFactory.Default.CreateInstance;
                ConfigurationItemFactory.Default.CreateInstance = type => m_Container.Kernel.HasComponent(type) ? m_Container.Resolve(type) : createInstanceOriginal(type);

                m_Container
                    .AddFacility<LoggingFacility>(f => f.LogUsing<GenericsAwareNLoggerFactory>().WithConfig(Path.Combine(context.BaseDirectory, "nlog.config")))
                    .Register(
                        Component.For<AppServerContext>().Instance(context),
                        Component.For<IConfigurationProvider>().Named("ConfigurationProvider")
                                 .ImplementedBy<InstanceAwareConfigurationProvideWrapper>()
                                 .DependsOn(new
                                 {
                                     configurationProvider,
                                     instanceParams = new
                                     {
                                         environment,
                                         appName,
                                         machineName = Environment.MachineName,
                                         instance = instanceName,
                                     }
                                 })
                    )
                    .Install(FromAssembly.Instance(typeof(TApp).Assembly, new PluginInstallerFactory()))
                    .Register(Component.For<IHostedApplication>().ImplementedBy<TApp>());
                m_HostedApplication = m_Container.Resolve<IHostedApplication>();
                var allowedTypes = new []{typeof(string),typeof(int),typeof(DateTime),typeof(decimal)};
                var commands =
                    m_HostedApplication.GetType()
                                     .GetMethods()
                                     .Where(m => m.Name != "Start" && m.Name != "ToString" && m.Name != "GetHashCode" && m.Name != "GetType")
                                     .Where(m => m.GetParameters().All(p => allowedTypes.Contains(p.ParameterType)))
                                     .Select(m => new InstanceCommandSpec( m.Name,m.GetParameters().Select(p => new InstanceCommandParam{Name = p.Name,Type = p.ParameterType.Name}).ToArray()));
                m_HostedApplication.Start();
                Status = HostedAppStatus.Started;
                return commands.ToArray();
            }
            catch (Exception e)
            {
                Status = HostedAppStatus.Stopped;

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            Status = HostedAppStatus.Stopping;
            if (m_Container == null)
                throw new InvalidOperationException("Host is not started");
            m_Container.Dispose();
            m_Container = null;
            Status = HostedAppStatus.Stopped;
            m_HostedApplication = null;
        }

        public string Execute(string command)
        {
            var methodInfo = m_HostedApplication.GetType().GetMethod(command);
            var result=methodInfo.Invoke(m_HostedApplication,
                              methodInfo.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray());
            return result == null ? null : result.ToString();
        }

        #endregion

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        /// <returns>
        /// An object of type <see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> used to control the lifetime policy for this instance. This is the current lifetime service object for this instance if one exists; otherwise, a new lifetime service object initialized to the value of the <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/> property.
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception><filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="RemotingConfiguration, Infrastructure"/></PermissionSet>
        public override object InitializeLifetimeService()
        {
            // prevents proxy from expiration
            return null;
        }

        private class InstanceAwareConfigurationProvideWrapper:IConfigurationProvider
        {
            private readonly IConfigurationProvider m_ConfigurationProvider;
            private readonly IDictionary<string, string> m_InstanceParams;
            private readonly Regex m_InstanceParamRegex;

            public InstanceAwareConfigurationProvideWrapper(IConfigurationProvider configurationProvider, object instanceParams)
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