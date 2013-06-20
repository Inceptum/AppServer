using System;
using System.Threading;

namespace Inceptum.AppServer.Utils
{
    public class PeriodicalBackgroundWorker : IDisposable
    {
        private readonly Thread m_Thread;
        private readonly ManualResetEvent m_StopEvent;
        private readonly int m_Period;
        private readonly Action m_DoWork;

        public PeriodicalBackgroundWorker(string name, int period, Action doWork, bool start = true)
        {
            m_StopEvent = new ManualResetEvent(!start);
            if (doWork == null) throw new ArgumentNullException("doWork");
            m_DoWork = doWork;
            if (period <= 0)
                throw new ArgumentException("period should be >=1ms", "period");
            m_Period = period;
            m_Thread = new Thread(worker) { Name = name };
            if (start)
                m_Thread.Start();
        }

        public void Start()
        {
            if (m_StopEvent.WaitOne(0))
            {
                m_StopEvent.Reset();
                m_Thread.Start();
            }
        }

        private void worker()
        {
            do
            {
                m_DoWork();
            } while (!m_StopEvent.WaitOne(m_Period));
        }

        public void Dispose()
        {
            if (m_StopEvent.WaitOne(0))
                return;

            m_StopEvent.Set();
            m_Thread.Join();

        }
    }
}
