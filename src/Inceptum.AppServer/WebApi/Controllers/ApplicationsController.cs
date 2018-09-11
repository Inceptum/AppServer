using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.WebApi.Controllers
{
    /// <summary>
    /// Applications
    /// </summary>
    public class ApplicationsController : ApiController
    {
        private readonly IHost m_Host;

        public ApplicationsController(IHost host)
        {
            m_Host = host;
        }


        /// <summary>
        /// List all applications.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(Application[]))]
        public IHttpActionResult Index()
        {
            return Ok(m_Host.Applications);
        }

        /// <summary>
        /// Retrieve an application.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <param name="application">The application.</param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(Application))]
        public IHttpActionResult Get(string vendor, string application)
        {
            Application app = m_Host.Applications.FirstOrDefault(a => a.Name == application && a.Vendor == vendor);
            if (app == null)
                return NotFound();
            return Ok(app);
        }

        /// <summary>
        /// Rediscover applications.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Rediscover()
        {
            m_Host.RediscoverApps();
            return Ok();
        }
    }
}