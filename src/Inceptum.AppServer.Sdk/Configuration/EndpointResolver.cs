using System;
using System.Collections.Generic;
using System.Configuration;
using Inceptum.Messaging.Contract;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Inceptum.AppServer.Configuration
{
    public class EndpointResolver : IEndpointProvider, ISubDependencyResolver
    {
        private readonly Dictionary<string, Endpoint> m_Endpoints;
        public EndpointResolver(Dictionary<string, Endpoint> endpoints)
        {
            m_Endpoints = endpoints;
            m_Endpoints = new Dictionary<string, Endpoint>(endpoints, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool Contains(string endpointName)
        {
            return m_Endpoints.ContainsKey(endpointName);
        }

        public Endpoint Get(string endpointName)
        {
            if (!Contains(endpointName))
                throw new ConfigurationErrorsException(string.Format("Endpoint with name '{0} not found", endpointName));

            return m_Endpoints[endpointName];
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
        {
            if (dependency.TargetItemType != typeof(Endpoint)) return false;

            var endpointName = getEndpointName(model, dependency);
            return this.Contains(endpointName);
        }

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
        {
            var endpointName = getEndpointName(model, dependency);
            return this.Get(endpointName);
        }

        private static string getEndpointName(ComponentModel model, DependencyModel dependency)
        {
            var endpointName = dependency.DependencyKey;
            if (model.ExtendedProperties.Contains("endpointNames"))
            {
                var endpointNames = (IDictionary<string, string>)model.ExtendedProperties["endpointNames"];
                if (endpointNames.ContainsKey(dependency.DependencyKey))
                {
                    endpointName = endpointNames[dependency.DependencyKey];
                }
            }
            return endpointName;
        }
    }
}