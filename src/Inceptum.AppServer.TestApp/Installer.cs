using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.TestApp
{
    public class Installer:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container
                .AddFacility<ConfigurationFacility>()//(f => f.Configuration("TestApp"))
                .AddFacility<StartableFacility>()
                .Register(Component.For<TestConf>().FromConfiguration("test","", "{environment}", "{machineName}"),
                Component.For<LogWriter>().StartUsingMethod(writer => writer.Start));
        }
    }
}