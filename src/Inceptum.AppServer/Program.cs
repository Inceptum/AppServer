using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using Inceptum.AppServer.Bootstrap;

namespace Inceptum.AppServer
{
    internal static class Program
    {
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main(params string[] args)
        {
            var debugFolders =new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-debug-folder": 
                        i++;
                        if (i < args.Length)
                            debugFolders.Add(args[i]);
                        break;
                    default:
                        Console.WriteLine("Unknown arg: " + args[i]);
                        return;
                }
            }

            if (!Environment.UserInteractive)
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                var servicesToRun = new ServiceBase[] { new ServiceHostSvc(() => createHost(debugFolders)) };
                ServiceBase.Run(servicesToRun);
                return;
            }

            using (createHost(debugFolders))
            {
                Console.ReadLine();
                
            }
        }

        private static IDisposable createHost(IEnumerable<string> debugFolders = null)
        {
            return Bootstrapper.Start(debugFolders);
        }
    }
}