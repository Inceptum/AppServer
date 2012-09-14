using System;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
    interface IApplicationInstanceFactory
    {
        ApplicationInstance Create(ApplicationName application, string name, Version version, ApplicationParams applicationParams, AppServerContext context);
    }
}