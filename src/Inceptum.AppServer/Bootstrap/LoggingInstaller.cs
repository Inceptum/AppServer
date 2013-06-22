using System;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Notification;
using Inceptum.AppServer.Utils;
using Inceptum.Core.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using NLog;
using NLog.Config;

namespace Inceptum.AppServer.Bootstrap
{
    public class LoggingInstaller:IWindsorInstaller

    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            //nlog app domain resolver e.g. ${app_domain} processing in nlog.config
            AppDomainRenderer.Register();
            var logFolder = new[] { AppDomain.CurrentDomain.BaseDirectory, "logs", "server" }.Aggregate(Path.Combine);
            //logfolder nlog variable
            GlobalDiagnosticsContext.Set("logfolder", logFolder);
            //Configuring management console target (forwards log to ui via SignalR)
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("ManagementConsole", typeof(ManagementConsoleTarget));
            container.Register(
                Component.For<ILogCache>().ImplementedBy<LogCache>().Forward<LogCache>().DependsOn(new { capacity = 2000 }),
                Component.For<LogConnection>(),
                Component.For<ManagementConsoleTarget>().DependsOn(new { source = "Server" }),
                Component.For<UiNotificationHub>().Forward<IHub>()//.Forward<IHostNotificationListener>()
                );
            //NLog has to resolve components from container
            var createInstanceOriginal = ConfigurationItemFactory.Default.CreateInstance;
            ConfigurationItemFactory.Default.CreateInstance = type => container.Kernel.HasComponent(type) ? container.Resolve(type) : createInstanceOriginal(type);
            var nlogConf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config");
            container.AddFacility<LoggingFacility>(f => f.LogUsing<GenericsAwareNLoggerFactory>().WithConfig(nlogConf));
        }
    }
}