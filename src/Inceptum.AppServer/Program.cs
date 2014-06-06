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
        /// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main(params string[] args)
        {
           // jobObject.AddProcess(process.Handle);
 
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