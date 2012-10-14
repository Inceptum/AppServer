using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.AppDiscovery.Openwrap;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Providers;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Management2;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Windsor;
using Inceptum.Core.Utils;
using Inceptum.Messaging;
using Inceptum.Messaging.Castle;
using NLog;
using NLog.Config;
using SignalR;

namespace Inceptum.AppServer
{

    class AppServerSetup
    {
        public string ConfSvcUrl { get; set; }
        public string[] AppsToStart { get; set; }
        public string Environment { get; set; }
        public string Repository { get; set; }
        public bool SendHb { get; set; }
        public int HbInterval { get; set; }
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
/*
            m_Host.RediscoverApps();
            m_Host.StartApps(m_AppsToStart);
*/
            m_Host.Start();
        }

        internal static IDisposable Start(AppServerSetup setup)
        {
            
            AppDomainRenderer.Register();
                string machineName = Environment.MachineName;
                
            IWindsorContainer container = new WindsorContainer();

            ILogger logger;
            var logFolder = new[] { AppDomain.CurrentDomain.BaseDirectory, "logs", "server" }.Aggregate(Path.Combine);
            GlobalDiagnosticsContext.Set("logfolder", logFolder);
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("ManagementConsole", typeof(ManagementConsoleTarget));
            container.Register(
                Component.For<ILogCache>().ImplementedBy<LogCache>().Forward<LogCache>().DependsOn(new { capacity = 2000 }),
                Component.For<LogConnection>(),
                Component.For<ManagementConsoleTarget>().DependsOn(new {source="Server"}),
                Component.For<UiNotificationHub>().Forward<IHostNotificationListener>()
                );

            GlobalHost.DependencyResolver = new WindsorToSignalRAdapter(container);
            //TODO: stop!!!
            SignalRhost.Start();


            var createInstanceOriginal = ConfigurationItemFactory.Default.CreateInstance;
            ConfigurationItemFactory.Default.CreateInstance = type => container.Kernel.HasComponent(type) ? container.Resolve(type) : createInstanceOriginal(type);

            var nlogConf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config");
            var loggingConfiguration = new XmlLoggingConfiguration(nlogConf);

            using (var logFactory = new LogFactory(loggingConfiguration))
            {
                Thread.Sleep(1000);
                var log = logFactory.GetLogger(typeof (Bootstrapper).Name);
                log.Info("Initializing...");
                log.Info("Creating container");
                try
                {
                    container
                        .AddFacility<StartableFacility>()
                        .AddFacility<TypedFactoryFacility>()
                        .AddFacility<LoggingFacility>(f => f.LogUsing<GenericsAwareNLoggerFactory>().WithConfig(nlogConf));

                    container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
                    container.Kernel.Resolver.AddSubResolver(new ConventionBasedResolver(container.Kernel));

                    logger = container.Resolve<ILoggerFactory>().Create(typeof (Bootstrapper));
                }catch(Exception e)
                {
                    log.FatalException("Failed to start\r\n",e);
                    throw;
                }
                log.Info("Registering components");
                
            }

            try
            {
                container.Register(
                    Component.For<IConfigurationProvider, IManageableConfigurationProvider>().ImplementedBy<LocalStorageConfigurationProvider>().Named("localStorageConfigurationProvider")
                                  .DependsOn(new { configFolder = Path.Combine(Environment.CurrentDirectory, Path.Combine("Configuration")) }));

                //If remote configuration source is provided in app.config use it by default
                if (setup.ConfSvcUrl != null)
                    container.Register(Component.For<IConfigurationProvider>().ImplementedBy<CachingRemoteConfigurationProvider>()
                        .DependsOn(new { serviceUrl = setup.ConfSvcUrl, path = "." })
                        .IsDefault());

                //Configuration local/remote
                container
                    .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer")
                                                                 .Params(new { environment = setup.Environment, machineName })
                                                                 .ConfigureTransports(new Dictionary<string, JailStrategy> { { "Environment", JailStrategy.Custom(() => setup.Environment) } },
                                                                                      "server.transports", "{environment}", "{machineName}"))
                    //messaging
                    .AddFacility<MessagingFacility>(f => { })
                    .Register(
                        Component.For<IApplicationInstanceFactory>().AsFactory(),
                        Component.For<ApplicationInstance>().LifestyleTransient(),
                        //Component.For<IHost>().ImplementedBy<Host>().DependsOn(new { name = setup.Environment }),
                        Component.For<IHost>().ImplementedBy<Host>().DependsOn(new { name = setup.Environment }),
                    //Applications to be started
                        setup.AppsToStart == null
                            ? Component.For<Bootstrapper>().DependsOnBundle("server.host", "", "{environment}", "{machineName}")
                            : Component.For<Bootstrapper>().DependsOn(new { appsToStart = setup.AppsToStart }),

                        Component.For<ManagementConsole>().DependsOn(new { container }).DependsOnBundle("server.host", "ManagementConsole", "{environment}", "{machineName}"),
                        Component.For<IApplicationBrowser>().ImplementedBy<OpenWrapApplicationBrowser>().DependsOn(
                            new
                            {
                                repository = setup.Repository ?? "Repository",
                                debugWraps = setup.DebugWraps ?? new string[0]
                            }));

                //HearBeats
                if (setup.SendHb)
                    container.Register(Component.For<HbSender>().DependsOn(new { environment = setup.Environment, hbInterval = setup.HbInterval }));

                UiNotificationHub hub = container.Resolve<UiNotificationHub>();
                Task.Factory.StartNew(() =>
                                          {
                                              int i=0;
                                              while (true)
                                              {
/*                                                  if (i % 11 == 0)
                                                    logger.FatalFormat("message {0}", i);
                                                  else if (i % 7 == 0)
                                                    logger.ErrorFormat("message {0}", i);
                                                  else if (i % 5== 0)
                                                    logger.WarnFormat("message {0}", i);
                                                  else if (i % 3 == 0)
                                                    logger.InfoFormat("message {0}", i);
                                                  else
                                                    logger.DebugFormat("message {0}", i);*/
                                                //  hub.InstancesChanged();
                                                  //hub.HeartBeat();
                                                  Thread.Sleep(1000);
                                                  i++;
                                              }
                                          });  
            }
            catch (Exception e)
            {
                logger.FatalFormat(e, "Failed to start");
                throw;
            }
            
             
            logger.Info("Starting application host");
            var sw = Stopwatch.StartNew();
            container.Resolve<Bootstrapper>().start();
            logger.InfoFormat("Initialization complete in {0}ms",sw.ElapsedMilliseconds);
            return container;
        
        }
    }
}