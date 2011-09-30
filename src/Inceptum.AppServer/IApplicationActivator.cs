using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace Inceptum.AppServer
{
    public interface IApplicationActivator
    {
        void GetInstallers(IDictionary<string, string> context, IWindsorContainer container);
    }
}