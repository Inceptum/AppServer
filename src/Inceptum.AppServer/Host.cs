using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    internal class Host : IDisposable, IHost
    {
        private static ILogger m_Logger = NullLogger.Instance;
        private readonly List<IApplicationBrowser> m_ApplicationBrowsers = new List<IApplicationBrowser>();
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly List<HostedAppInfo> m_Applications = new List<HostedAppInfo>();
        private readonly List<IApplicationHost> m_ApplicationHosts = new  List<IApplicationHost>();

        public Host( IApplicationBrowser applicationBrowser, ILogger logger = null, IConfigurationProvider configurationProvider = null, string name = null)
        {
            if (applicationBrowser == null) throw new ArgumentNullException("applicationBrowser");
            Name = name ?? MachineName;
            m_ConfigurationProvider = configurationProvider;
            m_Logger = logger ?? NullLogger.Instance;
            m_ApplicationBrowsers.Add(applicationBrowser);
            AppsStateChanged=new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>();
        }

        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged { get; private set; }

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
            //TODO: scenario where apps are rediscavered after app server start is nottested. Looks like clearing m_Applications is not enough.  
            //Get apps and take the latest version of each
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
                                string.Join(Environment.NewLine, m_Applications.Select(x => x.ToString())));

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
                                var appHost = CreateApplicationHost(appInfo);
                                lock (m_ApplicationHosts)
                                {
                                    m_ApplicationHosts.Add(appHost);
                                }
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


            IApplicationHost[] hosts;
            lock (m_ApplicationHosts)
            {
                hosts = m_ApplicationHosts.ToArray();
            }

            Task.WaitAll(hosts
                            .Select(appHost => Task.Factory.StartNew(
                                                                        () => startHost(appHost))
                                                                    )
                            .ToArray());
        }

        private void startHost(IApplicationHost appHost)
        {
            m_Logger.InfoFormat("Starting application '{0}'", appHost.AppInfo);
            var sw = Stopwatch.StartNew();
            try
            {
                appHost.Start(MarshalableProxy.Generate(m_ConfigurationProvider), new AppServerContext {Name = Name});
                sw.Stop();
                m_Logger.InfoFormat("Starting application '{0}' complete in {1}ms", appHost.AppInfo, sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                sw.Stop();
                lock (m_ApplicationHosts)
                {
                    m_ApplicationHosts.Remove(appHost);
                }
                m_Logger.ErrorFormat(e, "Failed to start application '{0}'", appHost.AppInfo);
            }
            AppsStateChanged.OnNext(HostedApps);
        }


        public void StopApps(params string[] apps)
        {
            IEnumerable<IApplicationHost> appsToStop;
            lock (m_ApplicationHosts)
            {
                appsToStop = apps.Where(a => m_ApplicationHosts.Any(h => h.AppInfo.Name == a)).Select(app => m_ApplicationHosts.FirstOrDefault(a => a.AppInfo.Name == app)).ToArray();
            }

            Task.WaitAll(appsToStop
                            .Select(appHost => Task.Factory.StartNew(
                                                                        () => stopHost(appHost))
                                                                    )
                            .ToArray());
        }

        private void stopHost(IApplicationHost appHost)
        {
            var hostedAppInfo = appHost.AppInfo;
            m_Logger.InfoFormat("Stopping application {0}", hostedAppInfo.Name);
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                appHost.Stop();
                
                lock (m_ApplicationHosts)
                {
                    m_ApplicationHosts.Remove(appHost);
                }
                sw.Stop();
                m_Logger.InfoFormat("Stopping application '{0}' complete in {1}ms", hostedAppInfo.Name, sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                sw.Stop();
                m_Logger.ErrorFormat(e, "Application {0} failed to stop", hostedAppInfo.Name);
            }
            AppsStateChanged.OnNext(HostedApps);
        }

        public Tuple<HostedAppInfo,HostedAppStatus>[] HostedApps
        {
            get
            {
                lock (m_ApplicationHosts)
                {
                    return m_ApplicationHosts.Select(appHost => Tuple.Create(appHost.AppInfo, appHost.Status)).ToArray();
                }
            }
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

        #region IDisposable Members

        public void Dispose()
        {
            m_Logger.Info("Stopping service host.");
            string[] apps;
            lock (m_ApplicationHosts)
            {
                apps = ((IEnumerable<IApplicationHost>)m_ApplicationHosts).Reverse().Select(host => host.AppInfo.Name).ToArray();
            }
            StopApps(apps);
            AppDomain.CurrentDomain.UnhandledException -= processUnhandledException;
            m_Logger.Info("Service host is stopped.");
        }

        #endregion
    }
}