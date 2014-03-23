using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inceptum.Cqrs;

namespace Inceptum.AppServer.Sdk.Cqrs
{
    public class AppServerAwareRabbitMqConventionEndpointResolver : RabbitMqConventionEndpointResolver
    {
        public AppServerAwareRabbitMqConventionEndpointResolver(string transport, string serializationFormat, InstanceContext instanceContext)
            : base(transport, serializationFormat, instanceContext.Name + "." + instanceContext.AppServerName, instanceContext.Environment)
        {
        }
    }
}
