using System;
using System.Linq;
using System.Reflection;

namespace Inceptum.AppServer.AppDiscovery
{
    internal class AssemblyTester : MarshalByRefObject
    {
        public HostedAppInfo Try(string assemblyFile)
        {
            //TODO[KN]: Load reflection only 
            //var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyFile);
            var assembly = Assembly.LoadFrom(assemblyFile);
            var attr = assembly.GetCustomAttributes(typeof (HostedApplicationAttribute), false).FirstOrDefault() as HostedApplicationAttribute;
            if (attr == null)
                return null;
            var appType = assembly.GetTypes().Where(t=>typeof(IHostedApplication).IsAssignableFrom(t)).FirstOrDefault();


             return new HostedAppInfo(attr.Name, appType == null ? null : typeof(ApplicationHost<>).MakeGenericType(appType).FullName, new[] { assemblyFile })
                       {
                           
                           Version = assembly.GetName().Version.ToString(),
                           ConfigFile = assemblyFile+".config"
                       };
        }
    }
}