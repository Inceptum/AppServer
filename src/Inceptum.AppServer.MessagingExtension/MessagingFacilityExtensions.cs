using Castle.MicroKernel.Registration;
using Inceptum.AppServer.Configuration;
using Inceptum.Messaging.Castle;
using Inceptum.Messaging.Configuration;

namespace Inceptum.AppServer.Sdk.Messaging
{


    public static class MessagingFacilityExtensions
    {

        public static MessagingFacility ConfigureFromBundle(this MessagingFacility facility, string bundleName,
            params string[] parameters)
        {
            facility.AddInitStep(k =>
            {
                k.Register(Component.For<IMessagingConfiguration>().ImplementedBy<MessagingConfiguration>().DependsOnBundle(bundleName, "", parameters));
                facility.WithConfigurationFromContainer();
            });
            return facility;
        }
    }
}
