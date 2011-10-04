using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    public class Host : IDisposable, IHost
    {
        private static ILogger m_Logger = NullLogger.Instance;
        private readonly List<IApplicationBrowser> m_ApplicationBrowsers = new List<IApplicationBrowser>();
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly List<HostedAppInfo> m_DiscoveredApps = new List<HostedAppInfo>();

        private readonly Dictionary<IApplicationHost, HostedAppInfo> m_HostedApps =
            new Dictionary<IApplicationHost, HostedAppInfo>();

        public Host(string appsFolder = null, ILogger logger = null, IConfigurationProvider configurationProvider = null, string name = null)
        {
            Name = name ?? MachineName;
            m_ConfigurationProvider = configurationProvider;
            if (appsFolder == null) throw new ArgumentNullException("appsFolder");
            m_Logger = logger ?? NullLogger.Instance;
            if (!Directory.Exists(appsFolder))
                Directory.CreateDirectory(appsFolder);
            m_ApplicationBrowsers.Add(new FolderApplicationBrowser(appsFolder)
                                          {
                                              Logger = m_Logger.CreateChildLogger(typeof (FolderApplicationBrowser).Name)
                                          });
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

        public virtual HostedAppInfo[] DiscoveredApps
        {
            get { return m_DiscoveredApps.ToArray(); }
        }


        public void LoadApps()
        {
            IEnumerable<HostedAppInfo> hostedAppInfos = m_ApplicationBrowsers.SelectMany(b => b.GetAvailabelApps());
            lock (m_DiscoveredApps)
            {
                foreach (HostedAppInfo appInfo in hostedAppInfos.Where(a => !m_DiscoveredApps.Contains(a)))
                {
                    m_DiscoveredApps.Add(appInfo);
                    m_Logger.InfoFormat("Discovered application {0}", appInfo.Name);
                }
            }
        }

        public void StartApps(params string[] appsToStart)
        {
            m_Logger.Info("Starting service host.");
            AppDomain.CurrentDomain.UnhandledException += processUnhandledException;

            foreach (HostedAppInfo appInfo in DiscoveredApps.Where(a => appsToStart == null || appsToStart.Length == 0 || appsToStart.Contains(a.Name)))
            {
                try
                {
                    IApplicationHost app = CreateApplicationHost(appInfo);
                    m_HostedApps.Add(app, appInfo);
                    m_Logger.InfoFormat("Loaded application {0}", appInfo.Name);
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
                    app.Key.Start(m_ConfigurationProvider);
                    sw.Stop();
                    m_Logger.InfoFormat("Starting application '{0}' complete in {1}ms", app.Value.Name, sw.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    sw.Stop();
                    m_Logger.ErrorFormat(e, "Failed to start application '{0}'", app.Value.Name);
                }
            }

            m_Logger.Info("Service host is started.");
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