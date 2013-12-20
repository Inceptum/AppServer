using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using Inceptum.AppServer.Bootstrap;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Hosting;

namespace Inceptum.AppServer
{

    internal static class Program
    {

        /*
         -debug-wrap "x:\WORK\Finam\CODE\_OPENWRAP\ibank\Internet.Bank.DiasoftAdapter-1.1.0.5.wrap, x:\WORK\Finam\CODE\_OPENWRAP\ibank\Internet.Bank.CyberplatAdapter-1.1.0.1.wrap"
         */

        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main(params string[] args)
        {
           // jobObject.AddProcess(process.Handle);

            var setup = new AppServerSetup
                            {
                                Environment = ConfigurationManager.AppSettings["Environment"],
                                ConfSvcUrl = ConfigurationManager.AppSettings["confSvcUrl"]
                            };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-appstostart":
                        i++;
                        if (i < args.Length)
                            setup.AppsToStart = args[i].Split(',');
                        break;

                    case "-debug-folder": 
                        i++;
                        if (i < args.Length)
                            setup.DebugFolders.Add(args[i]);
                        break;
                    case "-withNativeDll":
                        i++;
                        if (i < args.Length)
                            setup.DebugNativeDlls.Add(args[i]);
                        break;

                    default:
                        Console.WriteLine("Unknown arg: " + args[i]);
                        return;
                }
            }
            if (!Environment.UserInteractive)
            {
                const string source = "Inceptum.AppServer";
                const string log = "Application";

                if (!EventLog.SourceExists(source)) EventLog.CreateEventSource(source, log);
                var eLog = new EventLog {Source = source};
                eLog.WriteEntry(@"Starting the service in " + AppDomain.CurrentDomain.BaseDirectory,
                                EventLogEntryType.Information);

                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                var servicesToRun = new ServiceBase[] {new ServiceHostSvc(() => createHost(setup))};
                ServiceBase.Run(servicesToRun);
                return;
            }

            using (var host = createHost(setup))
            {
                Console.ReadLine();
                
            }
        }



        private static IDisposable createHost(AppServerSetup setup = null)
        {
            return Bootstrapper.Start(setup);
        }
    }


   

}