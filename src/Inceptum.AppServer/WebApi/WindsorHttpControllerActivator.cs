using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;

namespace Inceptum.AppServer.WebApi
{
    internal class WindsorHttpControllerActivator : IHttpControllerActivator
    {
        private readonly IKernel m_Kernel;

        public WindsorHttpControllerActivator(IKernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException("kernel");
            m_Kernel = kernel;
        }

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            var scope = m_Kernel.BeginScope();
            request.RegisterForDispose(scope);
            return (IHttpController)m_Kernel.Resolve(controllerType);
        }
    }
}