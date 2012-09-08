using System;
using Castle.DynamicProxy;

namespace Inceptum.AppServer.Hosting
{
    public class MarshalableProxy:MarshalByRefObject
    {
        

        public override object InitializeLifetimeService()
        {
            // prevents proxy from expiration
            return null;
        }

        public static T Generate<T>(T instance)
        {
            Type t = typeof(T);
            if (!t.IsInterface)
            {
                throw new ArgumentException("Type must be an interface");
            }
            try
            {
                //T instance = container.Resolve<T>();
                if (typeof(MarshalByRefObject).IsAssignableFrom(instance.GetType()))
                {
                    return instance;
                }

                var generator = new ProxyGenerator();
                var generatorOptions = new ProxyGenerationOptions { BaseTypeForInterfaceProxy = typeof(MarshalableProxy) };
                var proxy = (T)generator.CreateInterfaceProxyWithTarget(t, instance, generatorOptions);
                return proxy;

            }
            catch (Castle.MicroKernel.ComponentNotFoundException)
            {
                return default(T);
            }
        }

    }
}