using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Raven;
using Raven.Client;

namespace Inceptum.AppServer.RavenDb
{
    public class Installer:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container
                .AddFacility<ConfigurationFacility>(f => f.Configuration("Raven"))
                .AddFacility<StartableFacility>();


            //Raven
            container.Register(
                Component.For<RavenBootstrapper>().StartUsingMethod(c => c.Start),
                Component.For<IDocumentStore>().UsingFactoryMethod(k => k.Resolve<RavenBootstrapper>().Store)
                );
        }
    }
}