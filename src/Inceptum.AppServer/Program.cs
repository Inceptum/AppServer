using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Castle.Facilities.Logging;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using Inceptum.Core.Utils;
using Inceptum.Messaging;
using Inceptum.Messaging.Castle;

namespace Inceptum.AppServer
{
    internal static class Program
    {
        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main()
        {
            if (!Environment.UserInteractive)
            {
                const string source = "InternetBank";
                const string log = "Application";

                if (!EventLog.SourceExists(source)) EventLog.CreateEventSource(source, log);
                var eLog = new EventLog {Source = source};
                eLog.WriteEntry(@"Starting the service in " + AppDomain.CurrentDomain.BaseDirectory,
                                EventLogEntryType.Information);

                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                var servicesToRun = new ServiceBase[] {new ServiceHostSvc(createHost)};
                ServiceBase.Run(servicesToRun);
                return;
            }
           
            using (createHost())
            {
                Console.ReadLine();
            }
        }

        private static IDisposable createHost()
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
                                                             .ConfigureTransports("server.transports", "{environment}","{machineName}"))
                //TODO: move to app.config
                .AddFacility<MessagingFacility>(f => f.JailStrategy = (environment=="dev")?JailStrategy.MachineName : JailStrategy.None)
                .Register(
                    Component.For<Host>().DependsOn(new {appsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"apps")}),
                    Component.For<HostManager>().DependsOnBundle("server.host", "", "{environment}", "{machineName}")
                );
            return container;
        }
    }
}