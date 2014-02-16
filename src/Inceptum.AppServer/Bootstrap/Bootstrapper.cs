using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.AppDiscovery.NuGet;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Providers;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Management;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Raven;
using Inceptum.AppServer.Windsor;
using Microsoft.AspNet.SignalR;
using Raven.Client;

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
                container.Register(Component.For<JobObject>());
                container.Register(
                    Component.For<IConfigurationProvider, IManageableConfigurationProvider>().ImplementedBy<LocalStorageConfigurationProvider>().Named("localStorageConfigurationProvider")
                                  .DependsOn(new { configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration") }));

                //If remote configuration source is provided in app.config use it by default
                if (setup.ConfSvcUrl != null)
                    container.Register(Component.For<IConfigurationProvider>().ImplementedBy<CachingRemoteConfigurationProvider>()
                        .DependsOn(new { serviceUrl = setup.ConfSvcUrl, path = "." })
                        .IsDefault());


                //SignalR and Castle integraion
                GlobalHost.DependencyResolver = new WindsorToSignalRAdapter(container.Kernel);
                //Configuration local/remote
                container
                    .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer")
                                                                 .Params(new { environment = setup.Environment, machineName = Environment.MachineName })
                                                        )

                    //Management
                    .Register(                        
                        Component.For<SignalRhost>(),
                        Component.For<ManagementConsole>().DependsOn(new { container }),
                        Component.For<IHostNotificationListener>().ImplementedBy<UiNotifier>()
                        )
                    //Raven
                    .Register(
                        Component.For<RavenBootstrapper>()/*.DependsOn(new { indexLookupAssemblies = new[] { typeof(MessageViewModelListIndex).Assembly } })*/,
                        Component.For<IDocumentStore>().UsingFactoryMethod(k => k.Resolve<RavenBootstrapper>().Store)
                        )
                    //App hostoing
                    .Register(
                        Component.For<IApplicationInstanceFactory>().AsFactory(),
                        Component.For<ApplicationInstance>().LifestyleTransient(),
                        Component.For<ApplicationRepository>(),
                        Component.For<IHost>().ImplementedBy<Host>().DependsOn(new { name = setup.Environment }));
                container.Register(
                    Component.For<IApplicationRepository>().ImplementedBy<NugetApplicationRepository>()
                    
                    );

                if (setup.DebugFolders.Any())
                    container.Register(Component.For<IApplicationRepository>().ImplementedBy<FolderApplicationRepository>().DependsOn(
                        new
                        {
                            folders = setup.DebugFolders.ToArray()
                        })); 

            }
            catch (Exception e)
            {
                logger.FatalFormat(e, "Failed to start");
                throw;
            }
            
             
            logger.Info("Starting application host");
            var sw = Stopwatch.StartNew();
            var host = container.Resolve<IHost>();
            host.Start();
            logger.InfoFormat("Initialization complete in {0}ms",sw.ElapsedMilliseconds);
            return new CompositeDisposable
            {
                System.Reactive.Disposables.Disposable.Create(host.Stop),
                container
            };

        }
    } 
}