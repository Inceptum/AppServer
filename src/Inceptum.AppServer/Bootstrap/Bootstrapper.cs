using System;
using System.Collections.Generic;
using System.Configuration;
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
using Inceptum.AppServer.Windsor;
using Microsoft.AspNet.SignalR;

namespace Inceptum.AppServer.Bootstrap
{
    public static class Bootstrapper
    {
        internal static IDisposable Start(IEnumerable<string> debugFolders = null)
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
                var confSvcUrl = ConfigurationManager.AppSettings["confSvcUrl"];
                //If remote configuration source is provided in app.config use it by default
                if (confSvcUrl != null)
                    container.Register(Component.For<IConfigurationProvider, IManageableConfigurationProvider>().ImplementedBy<CachingRemoteConfigurationProvider>()
                        .DependsOn(new { serviceUrl = confSvcUrl, path = "." })
                        .IsDefault());


                //SignalR and Castle integraion
                GlobalHost.DependencyResolver = new WindsorToSignalRAdapter(container.Kernel);
                //Configuration local/remote
                container
                    .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer").Params(new { machineName = Environment.MachineName }))

                    //Management
                    .Register(                        
                        Component.For<SignalRhost>(),
                        Component.For<ManagementConsole>().DependsOn(new { container }),
                        Component.For<IHostNotificationListener>().ImplementedBy<UiNotifier>()
                        )
                    //App hostoing
                    .Register(
                        Component.For<IApplicationInstanceFactory>().AsFactory(),
                        Component.For<ApplicationInstance>().LifestyleTransient(),
                        Component.For<ApplicationRepository>(),
                        Component.For<IHost>().ImplementedBy<Host>())
                    //Nuget
                    .Register(
                        Component.For<IApplicationRepository>().ImplementedBy<NugetApplicationRepository>()
                        );

                var folders = (debugFolders??new string[0]).ToArray();
                if (folders.Length>0)
                {
                    container.Register(Component.For<IApplicationRepository>().ImplementedBy<FolderApplicationRepository>().DependsOn(new{folders}));
                }
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
                Disposable.Create(host.Stop),
                container
            };

        }
    } 
}