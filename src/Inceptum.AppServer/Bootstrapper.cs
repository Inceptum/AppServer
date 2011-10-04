using System;
using System.Configuration;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Management;
using Inceptum.Core.Utils;
using Inceptum.Messaging;
using Inceptum.Messaging.Castle;

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
            m_Host.LoadApps();
            m_Host.StartApps(m_AppsToStart);
        }

        public static IDisposable Start()
        {
            string environment = ConfigurationManager.AppSettings["Environment"];
            string confSvcUrl = ConfigurationManager.AppSettings["confSvcUrl"];
            string machineName = Environment.MachineName;
            AppDomainRenderer.Register();
            var container = new WindsorContainer();
            container
                .AddFacility<StartableFacility>()
                .AddFacility<LoggingFacility>(f => f.LogUsing(LoggerImplementation.NLog).WithConfig("nlog.config"))
                .Register(
                    Component.For<IConfigurationProvider>().ImplementedBy<LocalStorageConfigurationProvider>()
                        .DependsOn(
                            new {configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration")}))
                .AddFacility<ConfigurationFacility>(f => f.Configuration("AppServer")
                                                             .Params(new {environment, machineName})
                                                             .ConfigureTransports("server.transports", "{environment}", "{machineName}"))
                
                                                             //TODO: move to app.config
                .AddFacility<MessagingFacility>(f => f.JailStrategy = (environment == "dev") ? JailStrategy.MachineName : JailStrategy.None)
                .Register(
                    Component.For<IHost>().ImplementedBy<Host>().DependsOn(new { appsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps"), name = environment }),
                    Component.For<HbSender>(),
                    Component.For<Bootstrapper>().DependsOnBundle("server.host", "", "{environment}", "{machineName}")
                );
            

            //TODO: facility?
            var console = new ManagementConsole(container);
            container.Resolve<Bootstrapper>().start();
            return Disposable.Create(() =>
                                         {
                                             console.Dispose();
                                             container.Dispose();
                                         }
                );
        }
    }
}