using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Inceptum.Messaging;

namespace Inceptum.AppServer.Configuration
{
    public static class ComponentRegistrationExtentions
    {
        public static ComponentRegistration<T> DependsOnBundle<T>(this ComponentRegistration<T> r, string bundleName) where T : class
        {
            return r.DependsOnBundle(bundleName, "");
        }
        public static ComponentRegistration<T> DependsOnBundle<T>(this ComponentRegistration<T> r, string bundleName, string jsonPath, params string[] parameters) where T : class
        {
            return r.ExtendedProperties(new { dependsOnBundle = bundleName, jsonPath, bundleParameters=parameters });
        }

        public static ComponentRegistration<T> FromConfiguration<T>(this ComponentRegistration<T> r, string bundleName, string jsonPath, params string[] parameters) where T : class
        {
            return FromConfiguration<T, T>(r, bundleName, jsonPath, parameters);
        }

        public static ComponentRegistration<TSvc> FromConfiguration<TSvc,TImpl>(this ComponentRegistration<TSvc> r, string bundleName, string jsonPath, params string[] parameters)
            where TImpl:TSvc where TSvc : class
        {
            //TODO: implement without facility  resolving
                return r.UsingFactoryMethod(
                    kernel =>
                    kernel.Resolve<IConfigurationFacility>().DeserializeFromBundle<TImpl>(null,bundleName, jsonPath, parameters));
        }

        public static ConfigurationFacility ConfigureTransports(this ConfigurationFacility facility, string bundleName, params string[] parameters)
        {
            return ConfigureTransports(facility, null, bundleName, parameters);
        }

        public static ConfigurationFacility ConfigureTransports(this ConfigurationFacility facility, IDictionary<string, JailStrategy> jailStrategies, string bundleName, params string[] parameters)
        {
            facility.AddInitStep(kernel =>
            {
                var transportResolver = Component.For<ITransportResolver>()
                                                 .ImplementedBy<TransportResolver>()
                                                 .DependsOnBundle(bundleName, "", parameters)
                                                 .DependsOn(new { jailStrategies = jailStrategies});

                kernel.Register(transportResolver, Component.For<EndpointResolver>().DependsOnBundle(bundleName, "", parameters ?? new string[0]));
                var endpointResolver = kernel.Resolve<EndpointResolver>();
                kernel.Resolver.AddSubResolver(endpointResolver);
            });
            return facility;
        }


        public static ConfigurationFacility ConfigureConnectionStrings(this ConfigurationFacility facility, string bundleName, params string[] parameters)
        {
            facility.AddInitStep(kernel =>
            {
                kernel.Register(Component.For<ConnectionStringResolver>().DependsOnBundle(bundleName, "", parameters ?? new string[0]));
                var connectionStringResolver = kernel.Resolve<ConnectionStringResolver>();
                kernel.Resolver.AddSubResolver(connectionStringResolver);
            });
            return facility;
        }
    }
}