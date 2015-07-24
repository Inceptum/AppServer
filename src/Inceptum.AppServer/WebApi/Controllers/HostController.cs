using System.Web.Http;
using System.Web.Http.Description;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.WebApi.Controllers
{
    /// <summary>
    /// Host
    /// </summary>
    /// <remarks>
    /// Host endpoint provides information about application host
    /// </remarks>
    public class HostController : ApiController
    {
        private readonly IHost m_Host;

        /// <summary></summary>
        /// <param name="host"></param>
        public HostController(IHost host)
        {
            m_Host = host;
        }

        /// <summary>
        /// Host information
        /// </summary>        
        [HttpGet]
        [ResponseType(typeof (HostInfo))]
        public IHttpActionResult Index()
        {
            return Ok(new HostInfo
            {
                Name = m_Host.Name,
                MachineName = m_Host.MachineName,
                Mode = "Standalone",
                Version = GetType().Assembly.GetName().Version.ToString()
            });
        }
    }
}