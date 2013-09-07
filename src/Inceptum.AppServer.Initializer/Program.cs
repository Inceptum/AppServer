using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Initializer
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetWindowText(IntPtr hWnd, string windowName);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleTitle(string text);

        public static void Main(params string[] args)
        {
#if !DEBUG
            if (args.Length == 3 && args[2] == "-debug")
#endif
                Debugger.Launch();
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", args[1]);

            var name = args[0];
            AppDomain.CurrentDomain.SetData("AppServer.Application",name);
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

            SetConsoleTitle("AppServer - " + name); 
            SetWindowText(handle, "AppServer - " + name);

            var applicationHost = new ApplicationHost(name);
            applicationHost.Run();
        }

         
    }


  
}