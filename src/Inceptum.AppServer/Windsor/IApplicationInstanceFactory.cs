using System;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Windsor
{
    interface IApplicationInstanceFactory
    {
        ApplicationInstance Create(string applicationId, string name, Version version, ApplicationParams applicationParams, AppServerContext context);
        void Release(ApplicationInstance instance);
    }
}