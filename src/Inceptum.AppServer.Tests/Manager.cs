using System;
using System.Collections.Generic;

namespace Inceptum.AppServer.Tests
{
    internal class Manager : IDisposable
    {
        private readonly List<MyComponent> m_Components = new List<MyComponent>();
        private readonly IMyComponentFactory m_Factory;

        public Manager(IMyComponentFactory factory)
        {
            m_Factory = factory;
        }

        public void Dispose()
        {
            foreach (var component in m_Components)
            {
                m_Factory.Release(component);
            }
        }

        public void InitializeComponent(string name)
        {
            m_Components.Add(m_Factory.Create(name));
        }
    }
}