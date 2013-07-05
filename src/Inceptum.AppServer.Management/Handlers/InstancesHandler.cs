using System.Linq;
using System.Threading;
using Inceptum.AppServer.Model;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Handlers
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

        public OperationResult Get(string instance)
        {
            ApplicationInstanceInfo instanceInfo = m_Host.Instances.FirstOrDefault(i => i.Name == instance);
            if (instanceInfo == null)
                return new OperationResult.NotFound();
            return new OperationResult.OK(instanceInfo);
        }

        public ApplicationInstanceInfo[] GetByApplication(string application)
        {
            return m_Host.Instances.Where(i => i.ApplicationId == application).ToArray();
        }

        public void Post(ApplicationInstanceInfo instanceConfig)
        {
            m_Host.AddInstance(instanceConfig);
        }


        public void Put(string instance, ApplicationInstanceInfo instanceConfig)
        {
            m_Host.UpdateInstance(instanceConfig);
        }

        public void Delete(string instance)
        {
            m_Host.DeleteInstance(instance);
        }

        [HttpOperation(HttpMethod.POST, ForUriName = "start")]
        public void Start(string instance)
        {
            m_Host.StartInstance(instance);
        }

        [HttpOperation(HttpMethod.POST, ForUriName = "stop")]
        public void Stop(string instance)
        {
            m_Host.StopInstance(instance);
        }
    
        [HttpOperation(HttpMethod.POST, ForUriName = "command")]
        public OperationResult Command(string instance, string command)
        {
            return new OperationResult.OK(new {message = m_Host.ExecuteCommand(instance, command)});
        }


        [HttpOperation(HttpMethod.POST, ForUriName = "restart")]
        public void Restart(string instance)
        {
            //TODO: refactore this ugly crap
            while (m_Host.Instances.FirstOrDefault(i => i.Name == instance).Status == HostedAppStatus.Starting || m_Host.Instances.FirstOrDefault(i => i.Name == instance).Status == HostedAppStatus.Stopping)
            {
                Thread.Sleep(300);
            }

            if (m_Host.Instances.FirstOrDefault(i => i.Name == instance).Status == HostedAppStatus.Started)
            {
                m_Host.StopInstance(instance);
                while (m_Host.Instances.FirstOrDefault(i => i.Name == instance).Status == HostedAppStatus.Starting || m_Host.Instances.FirstOrDefault(i => i.Name == instance).Status == HostedAppStatus.Stopping)
                {
                    Thread.Sleep(300);
                }
            }
            m_Host.StartInstance(instance);
        }
    }
}