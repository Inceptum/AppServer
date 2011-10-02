using System;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;

namespace Inceptum.AppServer.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationProvider m_Provider;
        private readonly IWebOperationContext m_Context;

        public ConfigurationService(IConfigurationProvider provider)
            : this(provider,new WebOperationContextWrapper())
        {
        }

        internal ConfigurationService(IConfigurationProvider provider, IWebOperationContext context)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (context == null) throw new ArgumentNullException("context");
            m_Provider = provider;
            m_Context = context;
        }


        public Message GetBundle(string configuration, string bundleName, string parameters)
        {
            string[] extraParams = parameters.Split('/').ToArray();
            string content;
            try
            {
                content = m_Provider.GetBundle(configuration, bundleName, extraParams);
            }
            catch (Exception e)
            {
                throw new WebFaultException<string>(e.Message, HttpStatusCode.NotFound);
            }
            return m_Context.CreateTextResponse(content, "application/json; charset=utf-8", Encoding.UTF8);
        }

       

        internal interface IWebOperationContext
        {
            Message CreateTextResponse(string content, string contentType, Encoding encoding);
        }

        

        public class WebOperationContextWrapper : IWebOperationContext
        {
            public Message CreateTextResponse(string content, string contentType, Encoding encoding)
            {
                return WebOperationContext.Current.CreateTextResponse(content, contentType, encoding);
            }
        }
 
    }
}