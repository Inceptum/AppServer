using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Resolvers;
using Inceptum.Core.Messaging;

namespace Inceptum.AppServer.Configuration
{
    internal class EndpointResolver : ISubDependencyResolver
    {
		private readonly Dictionary<string, Endpoint> m_Endpoints = new Dictionary<string, Endpoint>();

        public EndpointResolver()
        {
        }

		public EndpointResolver(Dictionary<string, Endpoint> endpoints)
        {
            if (endpoints == null) throw new ArgumentNullException("endpoints");
            m_Endpoints = endpoints;
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver,
                               ComponentModel model, DependencyModel dependency)
        {
			if(dependency.TargetItemType != typeof(Endpoint)) return false;

			var endpointName = getEndpointName(model, dependency);
        	var canResolve = m_Endpoints.Any(p => p.Key.ToLower() == endpointName.ToLower());
        	return  canResolve;
        }

    	public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
    	{
    		var endpointName = getEndpointName(model, dependency);
    		var endpoint = m_Endpoints.First(p => p.Key.ToLower() == endpointName.ToLower()).Value;
    		return endpoint;
    	}

    	private static string getEndpointName(ComponentModel model, DependencyModel dependency)
    	{
			var endpointName = dependency.DependencyKey;
			if (model.ExtendedProperties.Contains("endpointNames"))
			{
				var endpointNames = (IDictionary<string, string>)model.ExtendedProperties["endpointNames"];
				if(endpointNames.ContainsKey(dependency.DependencyKey))
				{
					endpointName = endpointNames[dependency.DependencyKey];
				}
			}
			return endpointName;
    	}
    }
}
