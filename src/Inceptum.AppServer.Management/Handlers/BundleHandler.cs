using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Inceptum.AppServer.Configuration;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Handlers
{
    public class BundleHandler
    {
        private readonly IManageableConfigurationProvider m_Provider;

        public BundleHandler(IServerCore serverCore)
        {
            m_Provider = serverCore.LocalConfigurationProvider;
        }
        public OperationResult Get()
        {
            try
            {
                return new OperationResult.OK { ResponseResource =  m_Provider.GetAvailableConfigurations().ToArray() };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.ToString(), Title = "Error" };
            }
        }

        public OperationResult Get(string configuration)
        {
            try
            {
                var bundles = m_Provider.GetBundles(configuration).ToArray();
                return new OperationResult.OK { ResponseResource = new {name=configuration,bundles} };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.ToString(), Title = "Error" };
            }
        }

        public OperationResult Get(string configuration, string bundle, [Optional, DefaultParameterValue(null)]string overrides)
        {
            try
            {
                var bundleContent = m_Provider.GetBundle(configuration, bundle, overrides == null ? new string[0] : overrides.Split(new[] {':'}));
                return new OperationResult.OK {ResponseResource =bundleContent};
            }
            catch (BundleNotFoundException e)
            {
                return new OperationResult.NotFound{Description = e.Message,ResponseResource = e.Message,Title = "Error"};
            }            
            catch (Exception e)
            {
                return new OperationResult.InternalServerError{Description = e.Message,ResponseResource = e.ToString(),Title = "Error"};
            }
            
        }
    }
}