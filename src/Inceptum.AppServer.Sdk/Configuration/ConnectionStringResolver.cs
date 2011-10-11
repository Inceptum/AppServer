using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Inceptum.AppServer.Configuration
{
    internal class ConnectionStringResolver : ISubDependencyResolver
    {
        private readonly Dictionary<string, ConnectionString> m_ConnectionStrings=new Dictionary<string, ConnectionString>();

        public ConnectionStringResolver()
        {
        }

        public ConnectionStringResolver(Dictionary<string, string> connectionStrings)
        {
            foreach (var str in connectionStrings)
            {
                m_ConnectionStrings.Add(str.Key, str.Value);
            }



        }


        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver,
                               ComponentModel model, DependencyModel dependency)
        {
            return dependency.TargetItemType == typeof(ConnectionString) && m_ConnectionStrings.Any(p => p.Key.ToLower() == dependency.DependencyKey.ToLower());
        }

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
        {
            return m_ConnectionStrings.First(p => p.Key.ToLower() == dependency.DependencyKey.ToLower()).Value;
        }
    }
}