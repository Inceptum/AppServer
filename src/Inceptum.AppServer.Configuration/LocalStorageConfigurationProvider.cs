using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using Inceptum.AppServer.Configuration.Json;
using Inceptum.AppServer.Configuration.Model;
using Inceptum.AppServer.Configuration.Persistence;

namespace Inceptum.AppServer.Configuration
{
    //TODO: extract ConfigurationService
    //TODO: better remoting implementation (MarshalByRefObject)
    public class LocalStorageConfigurationProvider : MarshalByRefObject, IConfigurationProvider, IConfigurationService
    {
        #region WebOperationContext wrapper (tests backdooor)

        private readonly IWebOperationContext m_Context;

        internal LocalStorageConfigurationProvider(IConfigurationPersister persister, IContentProcessor contentProcessor,
                                                   IWebOperationContext context)
        {
            m_Persister = persister;
            m_ContentProcessor = contentProcessor;
            m_Context = context;
        }

        #region Nested type: IWebOperationContext

        internal interface IWebOperationContext
        {
            Message CreateTextResponse(string content, string contentType, Encoding encoding);
        }

        #endregion

        #region Nested type: WebOperationContextWrapper

        public class WebOperationContextWrapper : IWebOperationContext
        {
            #region IWebOperationContext Members

            public Message CreateTextResponse(string content, string contentType, Encoding encoding)
            {
                return WebOperationContext.Current.CreateTextResponse(content, contentType, encoding);
            }

            #endregion
        }

        #endregion

        #endregion

        private readonly IContentProcessor m_ContentProcessor;

        private readonly IConfigurationPersister m_Persister;


        public LocalStorageConfigurationProvider(string configFolder) :
            this(new FileSystemConfigurationPersister(configFolder), new JsonContentProcessor())
        {
        }

        public LocalStorageConfigurationProvider(IConfigurationPersister persister, IContentProcessor contentProcessor)
            :
                this(persister, contentProcessor, new WebOperationContextWrapper())
        {
            m_Persister = persister;
            m_ContentProcessor = contentProcessor;
        }

        #region IConfigurationProvider Members

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            var param=new[] { bundleName }.Concat(extraParams);
            int paramLen = param.Count();

            Bundle bundle = null;

            Config config;
            try
            {
                config = m_Persister.Load(configuration, m_ContentProcessor);
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(string.Format("Failed to load configuration '{0}': {1}", configuration, e.Message));
            }
            while (bundle == null && paramLen != 0)
            {
                string name = string.Join(".", param.Take(paramLen));
                bundle = config[name];
                paramLen--;
            }

            if (bundle == null)
                throw new ConfigurationErrorsException("Bundle not found");
            return bundle.Content;
        }

        #endregion

        #region IConfigurationService Members

        public Message GetBundle(string configuration, string bundleName, string parameters)
        {
            var extraParams = parameters.Split('/').ToArray();
            string content;
            try
            {
                content = GetBundle(configuration, bundleName, extraParams);
            }
            catch (Exception e)
            {
                throw new WebFaultException<string>(e.Message,HttpStatusCode.NotFound);
            }
            return m_Context.CreateTextResponse(content, "application/json; charset=utf-8", Encoding.UTF8);
        }

        #endregion
    }
}