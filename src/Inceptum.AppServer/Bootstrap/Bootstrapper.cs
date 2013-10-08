using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.AppDiscovery.Nuget;
using Inceptum.AppServer.AppDiscovery.Openwrap;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Providers;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Management;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Windsor;
using Inceptum.Messaging;
using Inceptum.Messaging.Castle;
using Microsoft.AspNet.SignalR;

namespace Inceptum.AppServer.Bootstrap
{
    public static class Bootstrapper
    {
        internal static IDisposable Start(AppServerSetup setup)
        {
            IWindsorContainer container = new WindsorContainer();
            container.Install(new LoggingInstaller());
            var logger = container.Resolve<ILoggerFactory>().Create(typeof(Bootstrapper));

            logger.Info("Initializing...");
            logger.Info("Creating container");
            try
            {
                container
                    .AddFacility<StartableFacility>()
                    .AddFacility<TypedFactoryFacility>();
                container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
                container.Kernel.Resolver.AddSubResolver(new ConventionBasedResolver(container.Kernel));
            }
            catch (Exception e)
            {
                logger.FatalFormat(e, "Failed to create container\r\n");
                throw;
            }

            logger.Info("Registering components");
            try
            {
                container.Register(
                    Component.For<IConfigurationProvider, IManageableConfigurationProvider>().ImplementedBy<LocalStorageConfigurationProvider>().Named("localStorageConfigurationProvider")
                                  .DependsOn(new { configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration") }));



                //If remote configuration source is provided in app.config use it by default
                if (setup.ConfSvcUrl != null)
                    container.Register(Component.For<IConfigurationProvider>().ImplementedBy<CachingRemoteConfigurationProvider>()
                        .DependsOn(new { serviceUrl = setup.ConfSvcUrl, path = "." })
                        .Named("remoteConfigurationProvider"));


                //SignalR and Castle integraion
                GlobalHost.DependencyResolver = new WindsorToSignalRAdapter(container.Kernel);
                //Configuration local/remote
                container
                    .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer")
                                                                 .Params(new { environment = setup.Environment, machineName = Environment.MachineName })
                                                                 .ConfigureTransports(new Dictionary<string, JailStrategy> { { "Environment", JailStrategy.Custom(() => setup.Environment) } },
                                                                                      "server.transports", "{environment}", "{machineName}"));

                //messaging
                container
                    .AddFacility<MessagingFacility>(f => { })
                    //Management
                    .Register(                        
                       // Component.For<IDependencyResolver>().Instance(new WindsorToSignalRAdapter(container.Kernel)),
                        Component.For<SignalRhost>().DependsOnBundle("server.host", "ManagementConsole", "{environment}", "{machineName}"),
                        Component.For<ManagementConsole>().DependsOn(new { container }).DependsOnBundle("server.host", "ManagementConsole", "{environment}", "{machineName}"),
                        Component.For<IHostNotificationListener>().ImplementedBy<UiNotifier>()
                        )
                    //App hostoing
                    .Register(
                        Component.For<IApplicationInstanceFactory>().AsFactory(),
                        Component.For<ApplicationInstance>().LifestyleTransient(),
                        Component.For<ApplicationRepositary>(),
                        Component.For<IHost>().ImplementedBy<Host>().DependsOn(new { name = setup.Environment }),
                        Component.For<IApplicationBrowser>().ImplementedBy<NugetApplicationBrowser>().DependsOnBundle("server.host", "nuget", "{environment}", "{machineName}")/*,                        
                        Component.For<IApplicationBrowser>().ImplementedBy<OpenWrapApplicationBrowser>().DependsOn(
                            new
                            {
                                repository = setup.Repository ?? "Repository",
                                debugWraps = setup.DebugWraps ?? new string[0]
                            })*/
                            );

                if (setup.DebugFolders.Any())
                    container.Register(Component.For<IApplicationBrowser>().ImplementedBy<FolderApplicationBrowser>().DependsOn(new { folders = setup.DebugFolders.ToArray(), nativeDlls = setup.DebugNativeDlls.ToArray()}));

                //HeartBeats
                if (setup.SendHb)
                    container.Register(Component.For<HbSender>().DependsOn(new { environment = setup.Environment, hbInterval = setup.HbInterval }));
            }
            catch (Exception e)
            {
                logger.FatalFormat(e, "Failed to start");
                throw;
            }
            
             
            logger.Info("Starting application host");
            var sw = Stopwatch.StartNew();
            container.Resolve<IHost>().Start();
            logger.InfoFormat("Initialization complete in {0}ms",sw.ElapsedMilliseconds);
#if DEBUG            
    //        container.Resolve<UiNotificationHub>();
#endif

            return container;
        
        }
    } 
}