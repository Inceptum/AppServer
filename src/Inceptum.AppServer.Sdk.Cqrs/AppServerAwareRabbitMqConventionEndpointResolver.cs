using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inceptum.Cqrs;

namespace Inceptum.AppServer.Sdk.Cqrs
{
    public class GatesRabbitMqConventionEndpointResolver : AppServerAwareRabbitMqConventionEndpointResolver
    {
        public GatesRabbitMqConventionEndpointResolver(string transport, string serializationFormat, InstanceContext instanceContext) 
            : base(transport, serializationFormat, instanceContext, "requests", "responses")
        {
        }
    }

    public class AppServerAwareRabbitMqConventionEndpointResolver : RabbitMqConventionEndpointResolver
    {
        public AppServerAwareRabbitMqConventionEndpointResolver(string transport, string serializationFormat, InstanceContext instanceContext, string commandsKeyword = null, string eventsKeyword = null)
            : base(transport, serializationFormat, instanceContext.Name.Replace('.', '-') + "." + instanceContext.AppServerName, instanceContext.Environment,commandsKeyword,eventsKeyword)
        {
        }
    }
}
