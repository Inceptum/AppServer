using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
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
			return dependency.TargetItemType == typeof(Endpoint) && m_Endpoints.Any(p => p.Key.ToLower() == dependency.DependencyKey.ToLower());
        }

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
        {
            return m_Endpoints.First(p => p.Key.ToLower() == dependency.DependencyKey.ToLower()).Value;
        }
    }
}
