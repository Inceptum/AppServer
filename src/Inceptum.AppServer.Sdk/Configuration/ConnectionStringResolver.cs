using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Inceptum.AppServer.Configuration
{
	internal class ConnectionStringResolver : ISubDependencyResolver
	{
		private readonly Dictionary<string, ConnectionString> m_ConnectionStrings = new Dictionary<string, ConnectionString>();

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
			if (dependency.TargetItemType == typeof (ConnectionString))
				return m_ConnectionStrings.Any(p => p.Key.ToLower() == dependency.DependencyKey.ToLower());
			
			if (dependency.TargetItemType == typeof (IDictionary<string, ConnectionString>))
				return m_ConnectionStrings.Any();

			return false;
		}

		public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
		                      ComponentModel model, DependencyModel dependency)
		{
			if (dependency.TargetItemType == typeof (ConnectionString))
				return m_ConnectionStrings.First(p => p.Key.ToLower() == dependency.DependencyKey.ToLower()).Value;

			if (dependency.TargetItemType == typeof (IDictionary<string, ConnectionString>))
				return m_ConnectionStrings;

			return null;
		}
	}
}