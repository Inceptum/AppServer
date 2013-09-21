using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.Windsor.Diagnostics.Extensions;
using Inceptum.AppServer.Model;
using NUnit.Framework;
using Castle.Core.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;

namespace Inceptum.AppServer.Tests
{
    public class MyComponent:IDisposable
    {
        public bool IsDisposed { get; private set; }

        public MyComponent(ILogger logger,string name)
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
    
   public  interface IMyComponentFactory
    {
        MyComponent Create(string name);
        void Release(MyComponent component);
    }

    class Manager:IDisposable
    {
        private readonly IMyComponentFactory m_Factory;
        private readonly List<MyComponent> m_Components=new List<MyComponent>();

        public Manager(IMyComponentFactory factory)
        {
            m_Factory = factory;
        }

        public void InitializeComponent(string name)
        {
            m_Components.Add(m_Factory.Create(name));
        }

        public void Dispose()
        {
            foreach (var component in m_Components)
            {
                m_Factory.Release(component);
            }
        }
    }

    [TestFixture]
    public class Investigation
    {
        [Test]
        [Ignore]
        public void Test()
        {
            using (var container=new WindsorContainer())
            {
                container
                    .AddFacility<TypedFactoryFacility>()
                    .AddFacility<LoggingFacility>()
                    .Register(
                        Component.For<IMyComponentFactory>().AsFactory(),
                        Component.For<MyComponent>().LifestyleTransient(),
                        Component.For<Manager>()
                        );

                var manager = container.Resolve<Manager>();

                manager.InitializeComponent("component1");
                manager.InitializeComponent("component2");
            }
        }

    }
}