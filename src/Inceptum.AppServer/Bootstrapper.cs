using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.AppDiscovery.Openwrap;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Providers;
using Inceptum.AppServer.Management;
using Inceptum.Core.Utils;
using Inceptum.Messaging;
using Inceptum.Messaging.Castle;
using NLog;
using NLog.Config;

namespace Inceptum.AppServer
{

    class AppServerSetup
    {
        public string ConfSvcUrl { get; set; }
        public string[] AppsToStart { get; set; }
        public string Environment { get; set; }
        public string Repository { get; set; }
        public bool SendHb { get; set; }

        public string[] DebugWraps { get; set; }
    }

    public class Bootstrapper
    {
        private readonly IHost m_Host;
        private readonly string[] m_AppsToStart;

        public Bootstrapper(IHost host,string [] appsToStart)
        {
            m_AppsToStart = appsToStart;
            m_Host = host;
        }

        private void start()
        {
            m_Host.RediscoverApps();
            m_Host.StartApps(m_AppsToStart);
        }

        internal static IDisposable Start(AppServerSetup setup)
        {
            AppDomainRenderer.Register();
                string machineName = Environment.MachineName;
            WindsorContainer container;
            var nlogConf = Path.Combine(Path.GetDirectoryName(typeof(Bootstrapper).Assembly.Location),"nlog.config");
            using (var logFactory = new LogFactory(new XmlLoggingConfiguration(nlogConf)))
            {
                var log = logFactory.GetLogger(typeof (Bootstrapper).Name);
                log.Info("Initializing...");
                log.Info("Creating container");
                container = new WindsorContainer();
                log.Info("Registering components");
            }

            container
                .AddFacility<StartableFacility>()
                .AddFacility<LoggingFacility>(f => f.LogUsing(LoggerImplementation.NLog).WithConfig(nlogConf))
                //Configuration local/remote
                .Register(
                    setup.ConfSvcUrl != null
                        ? Component.For<IConfigurationProvider>().ImplementedBy<LegacyRemoteConfigurationProvider>()
                              .DependsOn(new {serviceUrl = setup.ConfSvcUrl, path = "."})
                        : Component.For<IConfigurationProvider>().ImplementedBy<LocalStorageConfigurationProvider>()
                              .DependsOn(new {configFolder = Path.Combine(Environment.CurrentDirectory, Path.Combine("Configuration"))}))
                .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer")
                                                             .Params(new {environment = setup.Environment, machineName})
                                                             .ConfigureTransports(new Dictionary<string, JailStrategy> {{"Environment", JailStrategy.Custom(() => setup.Environment)}},
                                                                                  "server.transports", "{environment}", "{machineName}"))
                //messaging
                .AddFacility<MessagingFacility>(f => { })
                .Register(
                    Component.For<IHost>().ImplementedBy<Host>().DependsOn(new {name = setup.Environment}),
                    //Applications to be started
                    setup.AppsToStart == null
                        ? Component.For<Bootstrapper>().DependsOnBundle("server.host", "", "{environment}", "{machineName}")
                        : Component.For<Bootstrapper>().DependsOn(new {appsToStart = setup.AppsToStart}),
                    Component.For<ManagementConsole>().DependsOn(new { container }).DependsOnBundle("server.host", "ManagementConsole", "{environment}", "{machineName}"),
                    Component.For<IApplicationBrowser>().ImplementedBy<OpenWrapApplicationBrowser>().DependsOn(
                        new
                            {
                                repository = setup.Repository ?? "Repository",
                                debugWraps = setup.DebugWraps ?? new string[0]
                            })
                );
            
            //HearBeats
            if(setup.SendHb)
                container.Register(Component.For<HbSender>());
            
            var logger = container.Resolve<ILoggerFactory>().Create(typeof (Bootstrapper));
            logger.Info("Starting application host");
            container.Resolve<Bootstrapper>().start();
            logger.Info("Initialization complete");
            return container;
        
        }
    }
}