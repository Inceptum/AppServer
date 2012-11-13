using System;
using Castle.MicroKernel.Facilities;
using Inceptum.AppServer.Logging;
using NLog.Config;
using SignalR;

namespace Inceptum.AppServer.Windsor
{
    public class SignalRFacility:AbstractFacility
    {
        private IDisposable m_SignalRhost;

        protected override void Init()
        {
            GlobalHost.DependencyResolver = new WindsorToSignalRAdapter(Kernel);
            m_SignalRhost = SignalRhost.Start();
        }

        protected override void Dispose()
        {
            m_SignalRhost.Dispose();
            base.Dispose();
        }
    }
}