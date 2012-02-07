using System;
using System.Collections.Generic;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

namespace Inceptum.AppServer.Windsor
{
    public class ConventionBasedResolver : ISubDependencyResolver
    {
        private readonly IKernel m_Kernel;
        private readonly IDictionary<DependencyModel, string> m_KnownDependencies = new Dictionary<DependencyModel, string>();

        public ConventionBasedResolver(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }
            m_Kernel = kernel;
        }

        #region ISubDependencyResolver Members

        public object Resolve(CreationContext context, ISubDependencyResolver contextHandlerResolver, ComponentModel model, DependencyModel dependency)
        {
            string componentName;
            if (!m_KnownDependencies.TryGetValue(dependency, out componentName))
            {
                componentName = dependency.DependencyKey;
            }
            return m_Kernel.Resolve(componentName, dependency.TargetType);
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver contextHandlerResolver, ComponentModel model, DependencyModel dependency)
        {
            if (m_KnownDependencies.ContainsKey(dependency))
                return true;

            IHandler[] handlers = m_Kernel.GetHandlers(dependency.TargetType);

            //if there's just one, we're not interested.
            if (handlers.Length < 2)
                return false;
            foreach (IHandler handler in handlers)
            {
                if (IsMatch(handler.ComponentModel, dependency) && handler.CurrentState == HandlerState.Valid)
                {
                    if (!handler.ComponentModel.Name.Equals(dependency.DependencyKey, StringComparison.Ordinal))
                    {
                        m_KnownDependencies.Add(dependency, handler.ComponentModel.Name);
                    }
                    return true;
                }
            }
            return false;
        }

        #endregion

        private bool IsMatch(ComponentModel model, DependencyModel dependency)
        {
            return dependency.DependencyKey.Equals(model.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}