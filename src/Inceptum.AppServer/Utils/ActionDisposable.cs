using System;

namespace Inceptum.AppServer.Utils
{
    internal class ActionDisposable : IDisposable
    {
        private Action m_OnDispose;

        public ActionDisposable(Action onDispose)
        {
            if (onDispose == null) throw new ArgumentNullException("onDispose");

            m_OnDispose = onDispose;
        }

        public void Dispose()
        {
            if (m_OnDispose != null)
            {
                m_OnDispose();
                m_OnDispose = null;
            }
        }

        public static IDisposable Create(Action onDispose)
        {
            return new ActionDisposable(onDispose);
        }
    }
}