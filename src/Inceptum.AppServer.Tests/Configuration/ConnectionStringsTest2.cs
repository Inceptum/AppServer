using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Tests.Configuration
{
    internal class ConnectionStringsTest2
    {
        public ConnectionString ConnectionString1 { get; private set; }
        public ConnectionString ConnectionString2 { get; private set; }

        public ConnectionStringsTest2(ConnectionString cs1, ConnectionString cs2)
        {
            ConnectionString1 = cs1;
            ConnectionString2 = cs2;
        }
    }
}