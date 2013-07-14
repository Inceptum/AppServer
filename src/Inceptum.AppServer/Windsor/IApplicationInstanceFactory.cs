using System;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Windsor
{
    interface IApplicationInstanceFactory
    {
        ApplicationInstance Create(string name, string environment, AppServerContext context);
        void Release(ApplicationInstance instance);
    }
}