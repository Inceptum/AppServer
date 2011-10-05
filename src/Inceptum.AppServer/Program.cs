using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.ServiceProcess;
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
            return Bootstrapper.Start();
         
        }
    }
}