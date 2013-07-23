using System.Collections.Generic;

namespace Inceptum.AppServer.Configuration
{
    public interface IConnectionStringProvider
    {
        bool ContainsAny();
        bool Contains(string connectionStringName);
        ConnectionString Get(string connectionStringName);
        IDictionary<string, ConnectionString> GetAll();
    }
}