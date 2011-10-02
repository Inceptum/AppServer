using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
 
    public class Host : IDisposable
    {
        private static ILogger m_Logger = NullLogger.Instance;
        private readonly List<IApplicationBrowser> m_ApplicationBrowsers = new List<IApplicationBrowser>();
        private readonly List<HostedAppInfo> m_DiscoveredApps = new List<HostedAppInfo>();

        private readonly Dictionary<IApplicationHost, HostedAppInfo> m_HostedApps =
            new Dictionary<IApplicationHost, HostedAppInfo>();

        private IConfigurationProvider m_ConfigurationProvider;

        public Host(string appsFolder=null,ILogger logger = null, IConfigurationProvider configurationProvider=null)
        {
            m_ConfigurationProvider = configurationProvider;
            if (appsFolder == null) throw new ArgumentNullException("appsFolder");
            m_Logger = logger ?? NullLogger.Instance;
            if (!Directory.Exists(appsFolder))
                Directory.CreateDirectory(appsFolder);
            m_ApplicationBrowsers.Add(new FolderApplicationBrowser(appsFolder)
            {
                Logger = m_Logger.CreateChildLogger(typeof(FolderApplicationBrowser).Name)
            });
        }

        public virtual HostedAppInfo[] DiscoveredApps
        {
            get { return m_DiscoveredApps.ToArray(); }
        }

        public HostedAppInfo[] HostedApps
        {
            get { return m_HostedApps.Values.ToArray(); }
        }

        private static void processUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            m_Logger.Error("Unhandled exception.", (Exception) args.ExceptionObject);
        }


        public void LoadApps()
        {
            var hostedAppInfos = m_ApplicationBrowsers.SelectMany(b => b.GetAvailabelApps());
            lock (m_DiscoveredApps)
            {
                foreach (var appInfo in hostedAppInfos.Where(a=>!m_DiscoveredApps.Contains(a)))
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

            foreach (HostedAppInfo appInfo in DiscoveredApps.Where(a =>appsToStart==null||appsToStart.Length==0|| appsToStart.Contains(a.Name)))
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
                    m_Logger.InfoFormat("Starting application '{0}' complete in {1}ms", app.Value.Name,sw.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    sw.Stop();
                    m_Logger.ErrorFormat(e, "Failed to start application '{0}'", app.Value.Name);
                }
            }

            m_Logger.Info("Service host is started.");
        }

        /// <summary>
        /// Extracted for testing purposes. 
        /// </summary>
        internal virtual IApplicationHost CreateApplicationHost(HostedAppInfo appInfo)
        {
            return ApplicationHost.Create(appInfo);
        }

        #region IDisposable Members

        public void Dispose()
        {
            m_Logger.Info("Stopping service host.");
            foreach (var app in m_HostedApps.Reverse())
            {
                m_Logger.InfoFormat("Stopping application {0}", app.Value.Name);
                try
                {
                    app.Key.Stop();
                }
                catch (Exception)
                {
                    m_Logger.ErrorFormat("Application {0} failed to stop", app.Value.Name);
                }
            }
            AppDomain.CurrentDomain.UnhandledException -= processUnhandledException;
            m_Logger.Info("Service host is stopped.");
        }

        #endregion
    }
}