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
        private readonly List<HostedAppInfo> m_Applications = new List<HostedAppInfo>();

        private readonly Dictionary<IApplicationHost, HostedAppInfo> m_HostedApps = new Dictionary<IApplicationHost, HostedAppInfo>();

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
            get { return m_Applications.Select(a=>new AppInfo(a.Name,a.Version)).ToArray(); }
        }


        public void RediscoverApps()
        {
            m_Logger.InfoFormat("Discovering applications");

            IEnumerable<HostedAppInfo> hostedAppInfos = m_ApplicationBrowsers.SelectMany(b => b.GetAvailabelApps())
                .GroupBy(x => x.Name)
                .Select(x => x.OrderByDescending(y => y.Version).First())
                .Where(x => x != null);
            lock (m_Applications)
            {
                m_Applications.Clear();
                m_Applications.AddRange(hostedAppInfos);
            }
            m_Logger.InfoFormat("Discovered applications:{0}{1}", Environment.NewLine,
                                string.Join(Environment.NewLine, hostedAppInfos.Select(x => x.ToString())));

        }

        public void StartApps(params string[] appsToStart)
        {
            AppDomain.CurrentDomain.UnhandledException += processUnhandledException;
            IEnumerable<HostedAppInfo> apps;
            lock (m_Applications)
            {
                apps = m_Applications.ToArray();
            }

            m_Logger.InfoFormat("Loading applications: {0}",string.Join(", ", appsToStart));
            foreach (var app in appsToStart)
            {
                {
                        var appInfo=apps.FirstOrDefault(a=>a.Name==app);
                        if (appInfo != null)
                        {
                            try
                            {
                                IApplicationHost appHost = CreateApplicationHost(appInfo);
                                m_HostedApps.Add(appHost, appInfo);
                            }
                            catch (Exception e)
                            {
                                m_Logger.ErrorFormat(e, "Failed to load application '{0}'", appInfo);
                            }
                        }
                        else
                        {
                            m_Logger.ErrorFormat("Application '{0}' not found", app);
                        }
                }
            }

            m_Logger.InfoFormat("Starting loaded applications");

            foreach (var app in m_HostedApps.ToArray())
            {
                m_Logger.InfoFormat("Starting application '{0}'", app.Value.Name);
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    app.Key.Start(MatshalableProxy.Generate(m_ConfigurationProvider), new AppServerContext { Name = Name });
                    sw.Stop();
                    m_Logger.InfoFormat("Starting application '{0}' complete in {1}ms", app.Value.Name, sw.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    sw.Stop();
                    m_HostedApps.Remove(app.Key);
                    m_Logger.ErrorFormat(e, "Failed to start application '{0}'", app.Value.Name);
                }
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

    public class MatshalableProxy:MarshalByRefObject
        {
        

        public override object InitializeLifetimeService()
        {
            // prevents proxy from expiration
            return null;
        }

            public static T Generate<T>(T instance)
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
                    var generatorOptions = new ProxyGenerationOptions { BaseTypeForInterfaceProxy = typeof(MatshalableProxy) };
                    var proxy = (T)generator.CreateInterfaceProxyWithTarget(t, instance, generatorOptions);
                    return proxy;

                }
                catch (Castle.MicroKernel.ComponentNotFoundException)
                {
                    return default(T);
                }
            }

        }

}