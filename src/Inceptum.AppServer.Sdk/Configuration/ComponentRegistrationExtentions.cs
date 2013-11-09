using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder.Descriptors;
using Castle.MicroKernel.Registration;
using Inceptum.Messaging;
using Inceptum.Messaging.Configuration;
using Inceptum.Messaging.Contract;

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
        	return r.ExtendedProperties(new {dependsOnBundle = bundleName, jsonPath, bundleParameters = parameters});
        }

        private static ComponentRegistration<T> with<T, TE>(this ComponentRegistration<T> r, Func<IDictionary<string, string>, object> getExtendedProperties, object dependencies)
            where T : class
        {
            var dictionary = new ReflectionBasedDictionaryAdapter(dependencies);

            var explicitDependencies = new Dictionary<string, TE>();
            var dependenciesNames = new Dictionary<string, string>();
            foreach (var key in dictionary.Keys)
            {
                var dependencyName = key.ToString();
                if (dictionary[key] is TE)
                {
                    var dependency = (TE)dictionary[key];
                    explicitDependencies[dependencyName] = dependency;
                }
                if (dictionary[key] is string)
                {
                    var depndencyName = (string)dictionary[key];
                    dependenciesNames[dependencyName] = depndencyName;
                }
            }

            return r.AddDescriptor(new CustomDependencyDescriptor(explicitDependencies)).ExtendedProperties(getExtendedProperties(dependenciesNames));
			
        }

        public static ComponentRegistration<T> WithConnectionStrings<T>(this ComponentRegistration<T> r, object connectionStrings)
             where T : class
        {
            //connecionStrings extended property is used to resolve connection string with ConnectionStringResolver
            return with<T, ConnectionString>(r, dictionary => new { connectionStrings = dictionary }, connectionStrings);
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


        public static ComponentRegistration<T> FromLiveConfiguration<T>(this ComponentRegistration<T> r, string bundleName, string jsonPath, params string[] parameters) where T : class
        {
            return FromLiveConfiguration<T, T>(r, bundleName, jsonPath, parameters);
        }



       public static ComponentRegistration<TSvc> FromLiveConfiguration<TSvc,TImpl>(this ComponentRegistration<TSvc> r, string bundleName, string jsonPath, params string[] parameters)
            where TImpl: class, TSvc where TSvc : class
       {
           if(typeof(TImpl).GetProperties().Any(p=>!p.GetGetMethod().IsVirtual))
               throw new InvalidOperationException(string.Format("All property getters of type {0} should be virtual", typeof(TImpl)));

           return r.UsingFactoryMethod(
               kernel =>
                   new ProxyGenerator().CreateClassProxy<TImpl>(
                           new LiveConfigurationInterceptor<TSvc>(
                               () => kernel.Resolve<IConfigurationFacility>().DeserializeFromBundle<TImpl>(null, bundleName, jsonPath, parameters))));

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

                kernel.Register(transportResolver, Component.For<IEndpointProvider>().Forward<ISubDependencyResolver>().ImplementedBy<EndpointResolver>().Named("EndpointResolver").DependsOnBundle(bundleName, "", parameters ?? new string[0]));
                var endpointResolver = kernel.Resolve<ISubDependencyResolver>("EndpointResolver");
                kernel.Resolver.AddSubResolver(endpointResolver);
            });
            return facility;
        }


        public static ConfigurationFacility ConfigureConnectionStrings(this ConfigurationFacility facility, string bundleName, params string[] parameters)
        {
            facility.AddInitStep(kernel =>
                {
                    kernel.Register(Component.For<IConnectionStringProvider>().Forward<ISubDependencyResolver>().ImplementedBy<ConnectionStringResolver>().Named("ConnectionStringResolver").DependsOnBundle(bundleName, "", parameters ?? new string[0]));

                    var connectionStringResolver = kernel.Resolve<ISubDependencyResolver>("ConnectionStringResolver");
                    kernel.Resolver.AddSubResolver(connectionStringResolver);
                });
            return facility;
        }
    }


    public class LiveConfigurationInterceptor<TService> : IInterceptor
    {
        private Func<TService> m_ConfigGetter;

        public LiveConfigurationInterceptor(Func<TService> configGetter)
        {
            if (configGetter == null) throw new ArgumentNullException("configGetter");
            m_ConfigGetter = configGetter;
        }

        public void Intercept(IInvocation invocation)
        {
            var config = m_ConfigGetter();
            invocation.ReturnValue = invocation.Method.Invoke(config, invocation.Arguments);
        }
    }
}