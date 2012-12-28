using System;
using Castle.Core;
using SignalR.Hosting.Self;

namespace Inceptum.AppServer.Logging
{
    class SignalRhost:IDisposable,IStartable
    {
        private readonly string m_Url;
        private Server m_Server;
        
        public SignalRhost(int port)
        {
            m_Url= string.Format("http://*:{0}/sr/", port);
           
        }

        public void Start()
        {
            m_Server = new Server(m_Url);

            // Map the default hub url (/signalr)
            m_Server.MapHubs();
            m_Server.MapConnection<LogConnection>("/log");
            // Start the server
            m_Server.Start();
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            m_Server.Stop();
        }
    }
}