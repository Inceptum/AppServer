using System.Linq;
using System.Text;
using Castle.Core.Logging;
using Castle.MicroKernel;
using Castle.MicroKernel.Handlers;
using Castle.Windsor.Diagnostics;

namespace Inceptum.AppServer.AppHost.Container
{
    public class MisconfiguredComponentsLogger
    {
        private readonly ILogger m_Logger;

        public MisconfiguredComponentsLogger(ILogger logger)
        {
            m_Logger = logger;
        }

        public void Log(IKernel kernel)
        {
            var diagnostic = new PotentiallyMisconfiguredComponentsDiagnostic(kernel);
            IHandler[] handlers = diagnostic.Inspect();
            if (handlers != null && handlers.Any())
            {
                var builder = new StringBuilder();
                builder.AppendFormat("Misconfigured components ({0})\r\n", handlers.Count());
                foreach (IHandler handler in handlers)
                {
                    var info = (IExposeDependencyInfo)handler;
                    var inspector = new DependencyInspector(builder);
                    info.ObtainDependencyDetails(inspector);
                }
                m_Logger.Debug(builder.ToString());
            }
        }
    }
}