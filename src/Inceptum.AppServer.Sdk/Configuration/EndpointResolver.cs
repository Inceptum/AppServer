using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Inceptum.AppServer.Configuration
{
    internal class EndpointResolver : ISubDependencyResolver
    {
      
        private readonly Dictionary<string, SonicEndpoint> m_Endpoints=new Dictionary<string, SonicEndpoint>();

        public EndpointResolver()
        {
        }

        public EndpointResolver(Dictionary<string, SonicEndpoint> endpoints)
        {
            if (endpoints == null) throw new ArgumentNullException("endpoints");
            m_Endpoints = endpoints;
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver,
                               ComponentModel model, DependencyModel dependency)
        {
            return dependency.TargetItemType == typeof(SonicEndpoint) && m_Endpoints.Any(p => p.Key.ToLower() == dependency.DependencyKey.ToLower());
        }

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
        {
            return m_Endpoints.First(p => p.Key.ToLower() == dependency.DependencyKey.ToLower()).Value;
        }
    }
}