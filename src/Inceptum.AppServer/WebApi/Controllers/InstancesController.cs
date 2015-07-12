using System;
using System.Linq;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.ModelBinding;
using System.Web.Http.ValueProviders;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.WebApi.Controllers
{
    /// <summary>
    /// Instances
    /// </summary>
    public class InstancesController : ApiController
    {
        private readonly IHost m_Host;

        public InstancesController(IHost host)
        {
            m_Host = host;
        }


        /// <summary>
        /// List all instances.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(ApplicationInstanceInfo[]))]
        public IHttpActionResult Index()
        {
            return Ok(m_Host.Instances);
        }


        /// <summary>
        /// List all instances of application.
        /// </summary>
        /// <param name="vendor">The vendor.</param>
        /// <param name="application">The application.</param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(ApplicationInstanceInfo[]))]
        public IHttpActionResult GetByApplication(string vendor, string application)
        {
             return Ok(m_Host.Instances.Where(i => i.ApplicationId == application && i.ApplicationVendor == vendor).ToArray());
        }


        /// <summary>
        /// Retrieve instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        [ResponseType(typeof(ApplicationInstanceInfo))]
        public IHttpActionResult Get(string id)
        {
            ApplicationInstanceInfo instanceInfo = m_Host.Instances.FirstOrDefault(i => i.Name == id);
            if (instanceInfo == null)
                return NotFound();
            return Ok(instanceInfo);
        }

        /// <summary>
        /// Create application instance.
        /// </summary>
        /// <param name="instanceConfig">The instance configuration.</param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(ApplicationInstanceInfo))]
        public IHttpActionResult Create(ApplicationInstanceInfo instanceConfig)
        {
            m_Host.AddInstance(instanceConfig);
            ApplicationInstanceInfo instanceInfo = m_Host.Instances.First(i => i.Name == instanceConfig.Name);
            return Ok(instanceInfo);
        }


        /// <summary>
        /// Update application instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="instanceConfig">The instance configuration.</param>
        /// <returns></returns>
        [HttpPut]
        [ResponseType(typeof(ApplicationInstanceInfo))]
        public IHttpActionResult Update(string id, ApplicationInstanceInfo instanceConfig)
        {
            //TODO verify id==instanceConfig.Id
            m_Host.UpdateInstance(instanceConfig);
            ApplicationInstanceInfo instanceInfo = m_Host.Instances.First(i => i.Name == instanceConfig.Name);
            return Ok(instanceInfo);
        }

        /// <summary>
        /// Delete application instance.
        /// </summary>
        /// <param name="id">The instance.</param>
        /// <returns></returns>
        [HttpDelete]
        public IHttpActionResult Delete(string id)
        {
            m_Host.DeleteInstance(id);
            return Ok();
        }



        /// <summary>
        /// Start instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Start(string id)
        {
            m_Host.StartInstance(id, false);
            return Ok();
        }


        /// <summary>
        /// Debug instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Debug(string id)
        {
            m_Host.StartInstance(id, true);
            return Ok();
        }


        /// <summary>
        /// Kill instance process.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Kill(string id)
        {
            m_Host.KillInstanceProcess(id);
            return Ok();
        }

        /// <summary>
        /// Stop instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Stop(string id)
        {
            m_Host.StopInstance(id);
            return Ok();
        }

        /// <summary>
        /// Restart instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Restart(string id)
        {
            //TODO: refactore this ugly crap
            while (m_Host.Instances.FirstOrDefault(i => i.Name == id).Status == HostedAppStatus.Starting || m_Host.Instances.FirstOrDefault(i => i.Name == id).Status == HostedAppStatus.Stopping)
            {
                Thread.Sleep(300);
            }

            if (m_Host.Instances.FirstOrDefault(i => i.Name == id).Status == HostedAppStatus.Started)
            {
                m_Host.StopInstance(id).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            m_Host.StartInstance(id, false).ConfigureAwait(false).GetAwaiter().GetResult();
            return Ok();
        }

        /// <summary>
        /// Send command to instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [HttpPost]
        [ResponseType(typeof(CommandResult))]
        public IHttpActionResult Command(string id, InstanceCommand command)
        {
            return Ok(new CommandResult { Message = m_Host.ExecuteCommand(id, command) });
        }

        
          
        
        /// <summary>
        /// Update instance version.
        /// </summary>
        /// <param name="id">The instance.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        [HttpPut]
        public IHttpActionResult UpdateVersion(string id, /*[ModelBinder(typeof(VersionModelBinder))] Version*/string  version)
        /*public void Version(string instance, [JsonConverter(typeof(StringVersionJsonConverter))]Version version)*/
        {
            //TODO: ensure that version parses as desired
            m_Host.SetInstanceVersion(id, Version.Parse(version));
            return Ok();
        }


    }

    public class VersionModelBinder : IModelBinder
    {
        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(Version))
            {
                return false;
            }
            ValueProviderResult val = bindingContext.ValueProvider.GetValue(
            bindingContext.ModelName);
            if (val == null)
            {
                return false;
            }
            var value = val.RawValue as string;
            if (value == null)
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Wrong value type");
                return false;
            }


            Version version;
            if (Version.TryParse(value, out version))
            {
                bindingContext.Model = version;
                return true;
            }

            bindingContext.ModelState.AddModelError(
          bindingContext.ModelName, "Cannot convert value to Vresion");
            return false;
        }
    }

    public class CommandResult
    {
        public string Message { get; set; }
    }

}