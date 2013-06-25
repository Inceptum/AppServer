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
            container.AddFacility<ConfigurationFacility>(f=>f.Configuration("ibank"))
                .Register(Component.For<TestConf>().FromConfiguration("test","", "{environment}", "{instance}"));
        }
    }
}