using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Inceptum.AppServer.Configuration
{
    internal class ConnectionStringResolver : IConnectionStringProvider, ISubDependencyResolver
    {
        private readonly Dictionary<string, ConnectionString> m_ConnectionStrings;

        public ConnectionStringResolver(Dictionary<string, string> connectionStrings)
        {
            m_ConnectionStrings = new Dictionary<string, ConnectionString>(connectionStrings.ToDictionary(kvp => kvp.Key, kvp => (ConnectionString) kvp.Value));
        }

        public bool ContainsAny()
        {
            return m_ConnectionStrings.Any();
        }

        public bool Contains(string connectionStringName)
        {
            return m_ConnectionStrings.ContainsKey(connectionStringName);
        }

        public ConnectionString Get(string connectionStringName)
        {
            if (!Contains(connectionStringName))
                throw new ConfigurationErrorsException(string.Format("Connection string with name '{0}' not found", connectionStringName));

            return m_ConnectionStrings[connectionStringName];
        }

        public IDictionary<string, ConnectionString> GetAll()
        {
            //copy
            return new Dictionary<string, ConnectionString>(m_ConnectionStrings);
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver,
                               ComponentModel model, DependencyModel dependency)
        {
            if (dependency.TargetItemType == typeof (ConnectionString))
                return this.Contains(getConnectionStringName(model, dependency));

            if (dependency.TargetItemType == typeof (IDictionary<string, ConnectionString>))
                return this.ContainsAny();

            return false;
        }

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver,
                              ComponentModel model, DependencyModel dependency)
        {
            if (dependency.TargetItemType == typeof (ConnectionString))
                return this.Get(getConnectionStringName(model, dependency));

            if (dependency.TargetItemType == typeof (IDictionary<string, ConnectionString>))
                return this.GetAll();

            return null;
        }

        private static string getConnectionStringName(ComponentModel model, DependencyModel dependency)
        {
            var connectionStringName = dependency.DependencyKey;
            if (model.ExtendedProperties.Contains("connectionStrings"))
            {
                var connectionStringsNames = (IDictionary<string, string>)model.ExtendedProperties["connectionStrings"];
                if (connectionStringsNames.ContainsKey(dependency.DependencyKey))
                {
                    connectionStringName = connectionStringsNames[dependency.DependencyKey];
                }
            }
            return connectionStringName;
        }
    }
}