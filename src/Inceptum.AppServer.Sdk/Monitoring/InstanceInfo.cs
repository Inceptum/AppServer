using System;

namespace Inceptum.AppServer.Monitoring
{
    public class InstanceInfo
    {
        private HostHbMessage m_LastMessage;

        public InstanceInfo(HostHbMessage message)
        {
            LastMessage = message;
            Period = message.Period;
        }

        public HostHbMessage LastMessage
        {
            get { return m_LastMessage; }
            set
            {
                LastMessageDate = DateTime.Now;
                m_LastMessage = value;
            }
        }

        public DateTime LastMessageDate { get; set; }
        public long Period { get; set; }

        public string[] Servcies
        {
            get { return LastMessage.Services; }
        }

        public bool Alive
        {
            get { return (DateTime.Now - LastMessageDate).TotalMilliseconds <= Period*2; }
        }
    }
}