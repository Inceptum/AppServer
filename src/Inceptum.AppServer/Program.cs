using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using OpenWrap.Build;
using OpenWrap.Commands;
using OpenWrap.Commands.Wrap;
using OpenWrap.IO.Packaging;
using OpenWrap.PackageModel;
using OpenWrap.Services;

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
             
            var setup = new AppServerSetup
                            {
                                SendHb = false,
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

                    case "-repository":
                        i++;
                        if (i < args.Length)
                            setup.Repository = args[i];
                        break;
                    case "-debug-wrap":
                        i++;
                        if (i < args.Length)
                        {
                            var wraps = args[i].Split(',').Select(w => w.Trim());
                            foreach (var wrap in wraps)
                            {
                                if (Path.GetExtension(wrap).ToLower() != ".wrap")
                                {
                                    Console.WriteLine("-debug-wrap should be followed with comma separated list pf .wrap files");
                                    Environment.Exit(1);
                                }
                                if (!File.Exists(wrap))
                                {
                                    Console.WriteLine(wrap + " could not be found");
                                    Environment.Exit(1);
                                }


                            }
                            setup.DebugWraps =wraps.Select(Path.GetFullPath).ToArray();
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown arg: " + args[i]);
                        return;
                }
            }
            if (!Environment.UserInteractive)
            {
                const string source = "InternetBank";
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

            using (createHost(setup))
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