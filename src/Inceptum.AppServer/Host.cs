using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.DynamicProxy;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    internal class Host : IDisposable, IHost
    {
        private static ILogger m_Logger = NullLogger.Instance;
        private readonly List<IApplicationBrowser> m_ApplicationBrowsers = new List<IApplicationBrowser>();
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly List<HostedAppInfo> m_Applications = new List<HostedAppInfo>();

        private readonly List<IApplicationHost> m_HostedApps = new  List<IApplicationHost>();

        public Subject<Tuple<HostedAppInfo, HostedAppStatus>[]> AppsStateChanged { get; private set; }

        public Host( IApplicationBrowser applicationBrowser, ILogger logger = null, IConfigurationProvider configurationProvider = null, string name = null)
        {
            if (applicationBrowser == null) throw new ArgumentNullException("applicationBrowser");
            Name = name ?? MachineName;
            m_ConfigurationProvider = configurationProvider;
            m_Logger = logger ?? NullLogger.Instance;
            m_ApplicationBrowsers.Add(applicationBrowser);
            AppsStateChanged=new Subject<Tuple<HostedAppInfo, HostedAppStatus>[]>();
        }

        #region IDisposable Members

        public void Dispose()
        {
            m_Logger.Info("Stopping service host.");
            StopApps(((IEnumerable<IApplicationHost>)m_HostedApps).Reverse().Select(host => host.AppInfo.Name).ToArray());
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
                                IApplicationHost appHost = CreateApplicationHost(appInfo);
                                m_HostedApps.Add(appHost);
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

         
            
            Task.WaitAll(m_HostedApps
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
                appHost.Start(MatshalableProxy.Generate(m_ConfigurationProvider), new AppServerContext {Name = Name});
                sw.Stop();
                m_Logger.InfoFormat("Starting application '{0}' complete in {1}ms", appHost.AppInfo, sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                sw.Stop();
                m_HostedApps.Remove(appHost);
                m_Logger.ErrorFormat(e, "Failed to start application '{0}'", appHost.AppInfo);
            }
            AppsStateChanged.OnNext(HostedApps);
        }


        public void StopApps(params string[] apps)
        {
            var appsToStop = apps.Where(a => m_HostedApps.Any(h => h.AppInfo.Name == a)).Select(app => m_HostedApps.FirstOrDefault(a => a.AppInfo.Name == app));


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
                //TODO: Thread saftyness
                m_HostedApps.Remove(appHost);
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
                return m_HostedApps.Select(appHost => Tuple.Create(appHost.AppInfo, appHost.Status)).ToArray();
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