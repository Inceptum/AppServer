using System;
using Castle.Core.Logging;

namespace Inceptum.AppServer.Tests
{
    public class MyComponent : IDisposable
    {
        public MyComponent(ILogger logger, string name)
        {
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}