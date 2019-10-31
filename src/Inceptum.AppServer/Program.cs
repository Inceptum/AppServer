using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Inceptum.AppServer.Bootstrap;

namespace Inceptum.AppServer
{
    internal static class Program
    {
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main(params string[] args)
        {
            List<string> debugFolders;
            bool forceConsole;
            if (!parseCommandLineArgs(args, out debugFolders, out forceConsole))
            {
                return;
            }

            var host = Bootstrapper.Start(debugFolders);
            if (Environment.UserInteractive || forceConsole)
            {
                var mre = new ManualResetEvent(false);
                Console.Title = getProductNameAndVersion();
                using (host)
                {
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        if (eventArgs.SpecialKey == ConsoleSpecialKey.ControlBreak
                            || eventArgs.SpecialKey == ConsoleSpecialKey.ControlC)
                        {
                            mre.Set();
                            eventArgs.Cancel = true;
                        }
                    };

                    Task.Run(() => {
                        Console.ReadLine();
                        mre.Set();
                    });

                    mre.WaitOne();
                }
            }
            else
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                var servicesToRun = new ServiceBase[] {new ServiceHostSvc(() => host)};
                ServiceBase.Run(servicesToRun);
            }
        }

        private static bool parseCommandLineArgs(string[] args, out List<string> debugFolders, out bool forceConsole)
        {
            debugFolders = new List<string>();
            forceConsole = false;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-debug-folder":
                        i++;
                        if (i < args.Length)
                        {
                            debugFolders.Add(args[i]);
                        }
                        break;
                    case "-force-console":
                        i++;
                        forceConsole = true;
                        break;
                    default:
                        Console.WriteLine("Unknown arg: " + args[i]);
                        return false;
                }
            }

            return true;
        }

        private static string getProductNameAndVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            string productVersion = versionInfo.FileVersion;
            if (productVersion == "0.0.0.0")
            {
#if DEBUG
                productVersion = "DEBUG";
#endif
#if RELEASE
                productVersion = "RELEASE";
#endif
            }
            else
            {
                productVersion = "v" + productVersion;
            }

            return string.Format("AppServer, {0}", productVersion);
        }
    }
}