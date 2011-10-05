using System;
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
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Management;
using Inceptum.Core.Utils;
using Inceptum.Messaging;
using Inceptum.Messaging.Castle;
using NLog;
using NLog.Config;

namespace Inceptum.AppServer
{
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

        public static IDisposable Start()
        {
            AppDomainRenderer.Register();
                string environment = ConfigurationManager.AppSettings["Environment"];
                string confSvcUrl = ConfigurationManager.AppSettings["confSvcUrl"];
                string machineName = Environment.MachineName;
            WindsorContainer container;
            using(var logFactory = new LogFactory(new XmlLoggingConfiguration("nlog.config")))
            {
                var log = logFactory.GetLogger(typeof (Bootstrapper).Name);
                log.Info("Initializing...");
                log.Info("Creating container");
                container = new WindsorContainer();
                log.Info("Registering components");
            }
            container
                .AddFacility<StartableFacility>()
                .AddFacility<LoggingFacility>(f => f.LogUsing(LoggerImplementation.NLog).WithConfig("nlog.config"))
                .Register(
                    Component.For<IConfigurationProvider>().ImplementedBy<LocalStorageConfigurationProvider>()
                        .DependsOn(
                            new { configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration") }))
                .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer")
                                                             .Params(new { environment, machineName })
                                                             .ConfigureTransports("server.transports", "{environment}", "{machineName}"))

                                                             //TODO: move to app.config
                .AddFacility<MessagingFacility>(f => f.JailStrategy = (environment == "dev") ? JailStrategy.MachineName : JailStrategy.None)
                .Register(
                    Component.For<IHost>().ImplementedBy<Host>().DependsOn(new {name = environment }),
                // Component.For<HbSender>(),
                    Component.For<Bootstrapper>().DependsOnBundle("server.host", "", "{environment}", "{machineName}"),
                    //Component.For<IApplicationBrowser>().ImplementedBy<FolderApplicationBrowser>().DependsOn(new { folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps") }),
                    Component.For<IApplicationBrowser>().ImplementedBy<OpenWrapApplicationBrowser>().DependsOn(new { remoteRepository = "FakeRemoteRepo", localRepository = "LocalRepository" }),
                    Component.For<ManagementConsole>().DependsOn(new { container })
                );
            
            var logger = container.Resolve<ILoggerFactory>().Create(typeof (Bootstrapper));
            logger.Info("Starting application host");
            container.Resolve<Bootstrapper>().start();
            logger.Info("Initialization complete");
            return container;
        
        }
    }
}