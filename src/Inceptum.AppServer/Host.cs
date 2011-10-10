using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Castle.DynamicProxy;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    public class Host : IDisposable, IHost
    {
        private static ILogger m_Logger = NullLogger.Instance;
        private readonly List<IApplicationBrowser> m_ApplicationBrowsers = new List<IApplicationBrowser>();
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly List<AppInfo> m_DiscoveredApps = new List<AppInfo>();

        private readonly Dictionary<IApplicationHost, HostedAppInfo> m_HostedApps =
            new Dictionary<IApplicationHost, HostedAppInfo>();

        public Host(IApplicationBrowser applicationBrowser, ILogger logger = null, IConfigurationProvider configurationProvider = null, string name = null)
        {
            if (applicationBrowser == null) throw new ArgumentNullException("applicationBrowser");
            Name = name ?? MachineName;
            m_ConfigurationProvider = configurationProvider;
            m_Logger = logger ?? NullLogger.Instance;
            m_ApplicationBrowsers.Add(applicationBrowser);
        }

        #region IDisposable Members

        public void Dispose()
        {
            m_Logger.Info("Stopping service host.");
            StopApps(m_HostedApps.Reverse().Select(a=>a.Value.Name).ToArray());
            AppDomain.CurrentDomain.UnhandledException -= processUnhandledException;
            m_Logger.Info("Service host is stopped.");
        }

        #endregion

        #region IHost Members

        public string Name { get; private set; }

        public string MachineName
        {
            get { return Environment.MachineName; }
        }

        public virtual AppInfo[] DiscoveredApps
        {
            get { return m_DiscoveredApps.ToArray(); }
        }


        public void RediscoverApps()
        {
            m_Logger.InfoFormat("Discovering applications");

            IEnumerable<AppInfo> hostedAppInfos = m_ApplicationBrowsers.SelectMany(b => b.GetAvailabelApps());
            lock (m_DiscoveredApps)
            {
                foreach (AppInfo appInfo in hostedAppInfos.Where(a => !m_DiscoveredApps.Contains(a)))
                {
                    m_DiscoveredApps.Add(appInfo);
                    m_Logger.InfoFormat("Discovered application {0}", appInfo.Name);
                }
            }
        }

        public void StartApps(params string[] appsToStart)
        {
            AppDomain.CurrentDomain.UnhandledException += processUnhandledException;

            foreach (AppInfo appInfo in DiscoveredApps.Where(a => appsToStart == null || appsToStart.Length == 0 || appsToStart.Contains(a.Name)))
            {
                try
                {
                    //TODO: hack!!!
                    var browser = m_ApplicationBrowsers.First();
                    var appLoadParams = browser.GetAppLoadParams(appInfo);
                    if (appLoadParams != null)
                    {
                        IApplicationHost app = CreateApplicationHost(appLoadParams);
                        m_HostedApps.Add(app, appLoadParams);
                        m_Logger.InfoFormat("Loaded application {0}", appInfo.Name);
                    }
                }
                catch (Exception e)
                {
                    m_Logger.ErrorFormat(e, "Failed to load application '{0}'", appInfo.Name);
                }
            }

            foreach (var app in m_HostedApps)
            {
                m_Logger.InfoFormat("Starting application '{0}'", app.Value.Name);
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    app.Key.Start(getMarshalableProxy(m_ConfigurationProvider),new AppServerContext{Name=Name});
                    sw.Stop();
                    m_Logger.InfoFormat("Starting application '{0}' complete in {1}ms", app.Value.Name, sw.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    sw.Stop();
                    m_Logger.ErrorFormat(e, "Failed to start application '{0}'", app.Value.Name);
                }
            }

        }

        private T getMarshalableProxy<T>(T instance)
        {
            Type t = typeof(T);
            if (!t.IsInterface)
            {
                throw new ArgumentException("Type must be an interface");
            }
            try
            {
                //T instance = container.Resolve<T>();
                if (typeof(MarshalByRefObject).IsAssignableFrom(instance.GetType()))
                {
                    return instance;
                }

                var generator = new ProxyGenerator();
                var generatorOptions = new ProxyGenerationOptions { BaseTypeForInterfaceProxy = typeof(MarshalByRefObject) };
                return (T)generator.CreateInterfaceProxyWithTarget(t, instance, generatorOptions);

            }
            catch (Castle.MicroKernel.ComponentNotFoundException)
            {
                return default(T);
            }
        }

        public void StopApps(params string[] apps)
        {
            var appsToStop = apps.Where(a => m_HostedApps.Any(h => h.Value.Name == a)).Select(app => m_HostedApps.FirstOrDefault(a => a.Value.Name == app));

            foreach (var app in appsToStop)
            {
                m_Logger.InfoFormat("Stopping application {0}", app.Value.Name);
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    app.Key.Stop();
                    //TODO: Thread saftyness
                    m_HostedApps.Remove(app.Key);
                    sw.Stop();
                    m_Logger.InfoFormat("Stopping application '{0}' complete in {1}ms", app.Value.Name, sw.ElapsedMilliseconds);

                }
                catch (Exception e)
                {
                    sw.Stop();
                    m_Logger.ErrorFormat(e, "Application {0} failed to stop", app.Value.Name);
                }
            }
        }

        public HostedAppInfo[] HostedApps
        {
            get { return m_HostedApps.Values.ToArray(); }
        }

        #endregion

        private static void processUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            m_Logger.Error("Unhandled exception.", (Exception) args.ExceptionObject);
        }

        /// <summary>
        /// Extracted for testing purposes. 
        /// </summary>
        internal virtual IApplicationHost CreateApplicationHost(HostedAppInfo appInfo)
        {
            return ApplicationHost.Create(appInfo);
        }
    }
}