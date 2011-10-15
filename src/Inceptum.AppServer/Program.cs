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
                                //ConfSvcUrl = ConfigurationManager.AppSettings["confSvcUrl"],
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
                            var wrap = args[i];
                            if (Path.GetExtension(wrap).ToLower() != ".wrap")
                            {
                                Console.WriteLine("-debug should be followed with .wrap file");
                                Environment.Exit(1);
                            }
                            if (!File.Exists(wrap))
                            {
                                Console.WriteLine(wrap + " could not be found");
                                Environment.Exit(1);
                            }


                             /*   var process = Process.Start(new ProcessStartInfo("o", "build-wrap -quiet -incremental -debug")
                                {
                                    WorkingDirectory = Path.GetDirectoryName(wrapdesc),
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    RedirectStandardError = true,
                                    RedirectStandardOutput = true
                                });
                                process.WaitForExit();
                            
                            if(process.ExitCode!=0)
                                Console.WriteLine("Failed to build debuged wrap");
                            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(wrapdesc));
                            var wrap = directoryInfo.GetFiles("*.wrap").OrderByDescending(f=>f.CreationTime).First().FullName;
                              
                            if(Directory.Exists("DebugRepo"))
                                Directory.Delete("DebugRepo",true);
                            Directory.CreateDirectory("DebugRepo");
                            File.Copy(wrap, Path.Combine("DebugRepo",Path.GetFileName(wrap)));
                            Console.WriteLine(wrap);
                            setup.DebugWraps = Path.GetFullPath("DebugRepo");*/
                            setup.DebugWraps = new []{Path.GetFullPath(wrap)};
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