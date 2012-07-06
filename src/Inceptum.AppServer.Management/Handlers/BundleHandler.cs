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
        
        [HttpOperation(ForUriName = "Configurations")]
        public OperationResult Get()
        {
            try
            {
                return new OperationResult.OK { ResponseResource = m_Provider.GetConfigurations().ToArray() };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.ToString(), Title = "Error" };
            }
        }

        [HttpOperation(ForUriName = "Configurations")]
        public OperationResult Post(ConfigurationInfo configuration)
        {
            try
            {
                var cfgName = m_Provider.CreateConfiguration(configuration.Name);
                return new OperationResult.OK { ResponseResource = new { name = cfgName, id = cfgName } };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
        }
        
        [HttpOperation(ForUriName = "Configuration")]
        public OperationResult GetConfiguration(string configuration)
        {
            try
            {
                return new OperationResult.OK { ResponseResource = m_Provider.GetConfiguration(configuration) };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.ToString(), Title = "Error" };
            }
        }

        [HttpOperation(ForUriName = "Configuration")]
        public OperationResult DeleteConfiguration(string configuration)
        {
            try
            {
                m_Provider.DeleteConfiguration(configuration);
                return new OperationResult.OK ();
            }
            catch (BundleNotFoundException e)
            {
                return new OperationResult.NotFound { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }

        }

        [HttpOperation(ForUriName = "Bundles")]
        public OperationResult GetBundles(string configuration)
        {
            try
            {
                return new OperationResult.OK { ResponseResource = m_Provider.GetBundles(configuration) };
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
                var bundleContent = m_Provider.GetBundle(configuration, bundle, overrides == null ? new string[0] : overrides.Split(new[] { ':' }));
                return new OperationResult.OK { ResponseResource = bundleContent };
            }
            catch (BundleNotFoundException e)
            {
                return new OperationResult.NotFound { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
        }

        public OperationResult Put(string configuration, string bundle, BundleInfo data)
        {
            try
            {
                m_Provider.CreateOrUpdateBundle(configuration, bundle, data.Content);
                return new OperationResult.OK { };
            }
            catch (BundleNotFoundException e)
            {
                return new OperationResult.NotFound { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }

        }

        public OperationResult Post(string configuration, string bundle, BundleInfo data)
        {
            try
            {
                m_Provider.CreateOrUpdateBundle(configuration, bundle, data.Content);
                return new OperationResult.OK { };
            }
            catch (BundleNotFoundException e)
            {
                return new OperationResult.NotFound { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }

        }

        public OperationResult Delete(string configuration, string bundle)
        {
            try
            {
                m_Provider.DeleteBundle(configuration, bundle);
                return new OperationResult.OK { };
            }
            catch (BundleNotFoundException e)
            {
                return new OperationResult.NotFound { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
            }

        }
    }
}