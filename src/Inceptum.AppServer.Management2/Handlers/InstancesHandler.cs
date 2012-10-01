using System.Linq;
using Inceptum.AppServer.Model;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.Handlers
{
    public class InstancesHandler
    {
          private readonly IHost m_Host;

          public InstancesHandler(IHost host)
        {
            m_Host = host;
        }


        public ApplicationInstanceInfo[] Get()
        {
            return m_Host.Instances;
            
        }

/*
        [HttpOperation(HttpMethod.PUT)]
*/
        public void Post(ApplicationInstanceInfo instanceConfig)
        {
            m_Host.AddInstance(instanceConfig);
        }
         

        public void Put(string instance, ApplicationInstanceInfo instanceConfig)
        {
            m_Host.UpdateInstance(instanceConfig);
        }
         

        [HttpOperation(HttpMethod.POST, ForUriName = "start")]
        public void Start(string instance)
        {
            m_Host.StartInstance(instance);
        }       
        
        public void Delete(string instance)
        {
            m_Host.DeleteInstance(instance);
        }

       /* [HttpOperation(HttpMethod.OPTIONS, ForUriName = "instances")]
        public void Options(string instance)
        {
        }
*/

        [HttpOperation(HttpMethod.POST, ForUriName = "stop")]
        public void Stop(string instance)
        {
            m_Host.StopInstance(instance);
        }          
        
        public ApplicationInstanceInfo[] Get(string application)
        {
            return m_Host.Instances.Where(i=>i.ApplicationId==application).ToArray();
            
        }

/*
        public OperationResult Get(string application)
        {
            Application app = m_Host.Instances.FirstOrDefault(a => a.Name.Name == name);
            if (app == null)
                return new OperationResult.NotFound() {Description = "Application not found"};
            return new OperationResult.OK { ResponseResource = app };
            
        }
*/
    }
}