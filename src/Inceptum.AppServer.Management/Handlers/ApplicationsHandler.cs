using System.Linq;
using Inceptum.AppServer.Model;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Handlers
{
    public class ApplicationsHandler
    {
        private readonly IHost m_Host;

        public ApplicationsHandler(IHost host)
        {
            m_Host = host;
        }


        public Application[] Get()
        {
            return m_Host.Applications;
            
        }

        public OperationResult Get(string application)
        {
            Application app = m_Host.Applications.FirstOrDefault(a => a.Name == application);
            if (app == null)
                return new OperationResult.NotFound() {Description = "Application not found"};
            return new OperationResult.OK { ResponseResource = app };
        }

        [HttpOperation(HttpMethod.POST, ForUriName = "rediscover")]
        public OperationResult Rediscover()
        {
            m_Host.RediscoverApps();
            return new OperationResult.OK { ResponseResource = new Application[0]};
        }

    }
}