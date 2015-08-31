using System;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.TestApp
{
    public class ComponentWithMissingDependedncy
    {
        public ComponentWithMissingDependedncy(DateTime dateTime)
        {
        }
    }
    public class Installer:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container
                .AddFacility<ConfigurationFacility>()//(f => f.Configuration("TestApp"))
                .AddFacility<StartableFacility>()
                .Register(
                Component.For<ComponentWithMissingDependedncy>(),
                Component.For<TestConf>().FromConfiguration("test","", "{environment}", "{machineName}"),
                Component.For<LogWriter>().StartUsingMethod("Start")
                );
        }
    }
}