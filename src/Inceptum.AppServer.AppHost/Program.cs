using System;
using System.Diagnostics;
using System.IO;
using Inceptum.AppServer.Hosting;

namespace Inceptum.AppServer.AppHost
{
    public static class Program
    {
        public static void Main(params string[] args)
        {
            if (args.Length == 2 && args[1] == "-debug")
            {
                Debugger.Launch();
            }
            var name = args[0];

            var appConfigPath = Path.GetFullPath("app.config");
            if (File.Exists(appConfigPath))
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", appConfigPath);
            }

            AppDomain.CurrentDomain.SetData("APPBASE", Path.GetFullPath("./content"));
            AppDomain.CurrentDomain.SetData("AppServer.Application", name);

            var handle = Process.GetCurrentProcess().MainWindowHandle;
            
            string title = string.Format("{0} - AppServer", name);
            
            WndUtils.SetConsoleTitle(title);
            WndUtils.SetWindowText(handle, title);

            var applicationHost = new ApplicationHost(name);
            applicationHost.Run();
        }
    }
}