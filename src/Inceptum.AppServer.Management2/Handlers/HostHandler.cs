using Inceptum.AppServer.Management2.Resources;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Management2.Handlers
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
                           MachineName = m_Host.MachineName,
                           Mode = "Standalone"
                       };
        } 
    }
}