using System;
using Castle.DynamicProxy;

namespace Inceptum.AppServer.Hosting
{
    public class MarshalableProxy : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            // prevents proxy from expiration
            return null;
        }

        public static T Generate<T>(T instanceToProxy) where T : class
        {
            if (instanceToProxy == null) throw new ArgumentNullException("instanceToProxy");

            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException("Type must be an interface");
            }

            if (instanceToProxy is MarshalByRefObject)
            {
                return instanceToProxy;
            }

            var generator = new ProxyGenerator();
            var generatorOptions = new ProxyGenerationOptions { BaseTypeForInterfaceProxy = typeof(MarshalableProxy) };
            var proxy = (T)generator.CreateInterfaceProxyWithTarget(typeof(T), instanceToProxy, generatorOptions);
            return proxy;
        }
    }
}