using Inceptum.AppServer.Management.Resources;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Management.Handlers
{
    public class HostHandler
    {
        private readonly IHost m_Host;
        public HostHandler(IHost host)
        {
            m_Host = host;
        }
        public HostInfo Get()
        {
            return new HostInfo
                       {
                           Name=m_Host.Name,
                           MachineName = m_Host.MachineName,
                           Mode = "Standalone",
                           Version = typeof(HostHandler).Assembly.GetName().Version.ToString()
                       };
        } 
    }
}