using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Tests.Configuration
{
    internal class ConnectionStringsTest1
    {
        public ConnectionString ConnectionString { get; private set; }

        public ConnectionStringsTest1(ConnectionString cs1)
        {
            ConnectionString = cs1;
        }
    }
}