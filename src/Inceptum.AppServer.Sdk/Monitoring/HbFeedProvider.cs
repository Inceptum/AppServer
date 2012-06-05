using Inceptum.Core.Messaging;
using Inceptum.DataBus;
using Inceptum.DataBus.Messaging;

namespace Inceptum.AppServer.Monitoring
{
	/// <summary>
	/// Hb feed provider
	/// </summary>
	[Channel(ServicesMonitor.HB_CHANNEL)]
	public class HbFeedProvider : MessagingFeedProviderBase<HostHbMessage, EmptyContext>
	{
		private readonly Endpoint m_HbEndpoint;

		public HbFeedProvider(IMessagingEngine messagingEngine, Endpoint hbEndpoint)
			: base(messagingEngine)
		{
			m_HbEndpoint = hbEndpoint;
		}

		protected override Endpoint GetEndpoint(EmptyContext context)
		{
			return m_HbEndpoint;
		}
	}
}