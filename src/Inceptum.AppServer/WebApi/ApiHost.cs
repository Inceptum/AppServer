using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;
using Inceptum.AppServer.WebApi.MessageHandlers;
using Inceptum.WebApi.Help;
using Inceptum.WebApi.Help.Builders;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Inceptum.AppServer.WebApi
{
    internal sealed class ApiHost : IDisposable
    {
        private readonly HostConfiguration m_HostConfiguration;
        private readonly IHttpControllerActivator m_HttpControllerActivator;
        private readonly IFilter[] m_Filters;
        private readonly ILogger m_Logger;
        private IDisposable m_Host;

        public ApiHost(ILogger logger, HostConfiguration hostConfiguration, IHttpControllerActivator httpControllerActivator, IFilter[] filters)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            if (hostConfiguration == null) throw new ArgumentNullException("hostConfiguration");
            if (httpControllerActivator == null) throw new ArgumentNullException("httpControllerActivator");
            m_Logger = logger;
            m_HostConfiguration = hostConfiguration;
            m_HttpControllerActivator = httpControllerActivator;
            m_Filters = filters;
        }

        public void Start()
        {
            if (m_Host != null) return;

            if (!m_HostConfiguration.Enabled)
            {
                m_Logger.WarnFormat("API host is disabled in configuration");
                return;
            }

            string baseUrl = string.Format(@"http://{0}:{1}", m_HostConfiguration.Host ?? "*", m_HostConfiguration.Port);

            m_Logger.InfoFormat("Starting WebAPI host on {0}", baseUrl);

            m_Host = runHost(baseUrl);
        }

        public void Stop()
        {
            if (m_Host != null)
            {
                m_Host.Dispose();
                m_Host = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private IDisposable runHost(string baseUrl)
        {
            return WebApp.Start(baseUrl, appBuilder =>
            {
                var config = configureHost(new HttpConfiguration());
                var hubConfig = new HubConfiguration { Resolver = GlobalHost.DependencyResolver, EnableJavaScriptProxies = true };
                appBuilder
                    .UseCors(CorsOptions.AllowAll)
                    .UseWebApi(config)
                    .MapSignalR(hubConfig)
                    .MapSignalR<LogConnection>("/log");
            });
        }

        private HttpConfiguration configureHost(HttpConfiguration config)
        {
            Array.ForEach(m_Filters, config.Filters.Add);
            config.Services.Replace(typeof(IHttpControllerActivator), m_HttpControllerActivator);
            config.MessageHandlers.Insert(0, new DefaultContentTypeMessageHandler());
            config.MessageHandlers.Add(new DefaultHeadersMessageHandler());

            config.Formatters.Remove(config.Formatters.XmlFormatter);

            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            jsonFormatter.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            jsonFormatter.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            // NOTE[tv]: JsonMediaTypeFormatter ignores deserialization errors.
            //           So deserialized data may be unconsistent, e.g. some fields are OK while others are null.
            //           This way we can at least log such kind of error.
            jsonFormatter.SerializerSettings.Error += (sender, args) =>
            {
                if (args.ErrorContext != null && args.ErrorContext.Error != null)
                {
                    m_Logger.ErrorFormat("Failed to deserialize JSON request.", args.ErrorContext.Error);
                }
                else
                {
                    m_Logger.ErrorFormat("Failed to deserialize JSON request.");
                }
            };

            // Allow Cross-Origin Requests from any domain            
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            configureRoutes(config);

            configureHelp(config);

            config.MessageHandlers.Insert(0, new StaticContentMessageHandler());

            return config;
        }

        private static void configureRoutes(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("RESTBundleSearch", "api/configurations/search",
                new { action = "Search", controller = "configurations" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) }
                );

            config.Routes.MapHttpRoute("RESTGetAll", string.Format("api/{{controller}}/"),
                new { action = "Index" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) }
                );
            config.Routes.MapHttpRoute("RESTGet", string.Format("api/{{controller}}/{{id}}"),
                new { action = "Get" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get), controller = @"^(?:(?!applications).)*$" }
                );
            config.Routes.MapHttpRoute("RESTCreate", string.Format("api/{{controller}}"),
                new { action = "Create" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Post), controller = @"^(?:(?!applications).)*$" }
                );
            config.Routes.MapHttpRoute("RESTUpdate", string.Format("api/{{controller}}/{{id}}"),
                new { action = "Update" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put), controller = @"^(?:(?!applications).)*$" }
                );
            config.Routes.MapHttpRoute("RESTDelete", string.Format("api/{{controller}}/{{id}}"),
                new { action = "Delete" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Delete), controller = @"^(?:(?!applications).)*$" }
                );
            
            config.Routes.MapHttpRoute("RESTBundleCreate", "api/configurations/{configuration}/bundles",
                new { action = "CreateBundle", controller = "configurations" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) }
                );
            config.Routes.MapHttpRoute("RESTBundleUpdate", "api/configurations/{configuration}/bundles/{id}",
                new { action = "UpdateBundle", controller = "configurations" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put) }
                );
            config.Routes.MapHttpRoute("RESTBundleDelete", "api/configurations/{configuration}/bundles/{id}",
                new { action = "DeleteBundle", controller = "configurations" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Delete) }
                );
            config.Routes.MapHttpRoute("RESTBundleGetWithOverrides", "configuration/{configuration}/{bundle}/{*overrides}",
                new { action = "GetBundleWithOverrides", controller = "configurations" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) }
                );

            config.Routes.MapHttpRoute("RESTApplicationsGet", string.Format("api/applications/{{vendor}}/{{application}}"),
                new { action = "Get", controller = "applications" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) }
                );

            config.Routes.MapHttpRoute("RESTApplicationsInstancesGet", string.Format("api/applications/{{vendor}}/{{application}}/instances"),
                new { action = "GetByApplication", controller = "instances" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) }
                );

            config.Routes.MapHttpRoute("RESTInstancesUpdateVersion", string.Format("api/instances/{{id}}/version/{{version}}"),
                new { action = "UpdateVersion", controller = "instances" },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Put) }
                );

            config.Routes.MapHttpRoute("RESTGlobalAction", string.Format("api/{{controller}}/{{action}}"),
                new { },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Post, HttpMethod.Get), action = @"^(?:(?!Create|Delete|Update|Index|Get).)*$" }
                );
            config.Routes.MapHttpRoute("RESTItemAction", string.Format("api/{{controller}}/{{id}}/{{action}}"),
                new { },
                new { httpMethod = new HttpMethodConstraint(HttpMethod.Post, HttpMethod.Get), action = @"^(?:(?!Create|Delete|Update|Index|Get).)*$" }
                );
        }

        private void configureHelp(HttpConfiguration config)
        {
            config.UseHelpPage(
                help => help
                        .SamplesUri(new Uri(string.Format(@"http://{0}:{1}", (m_HostConfiguration.Host == "*" || m_HostConfiguration.Host == "+") ? System.Net.Dns.GetHostEntry("").HostName : m_HostConfiguration.Host, m_HostConfiguration.Port)))
                        .WithDocumentationProvider(new XmlDocumentationProvider(Environment.CurrentDirectory))
                        .WithContentProvider(new LocalizableContentProvider(new HelpStaticContentProvider(help.ServiceLocator.Get<IContentProvider>())))
                        .ConfigureHelpProvider(hp => hp.ClearBuilders()
                                                       .RegisterBuilder(new DelegatingBuilder(buildDisclaimer))
                                                       .RegisterBuilder(new ErrorsDocumentationBuilder())
                                                       .RegisterBuilder(new ApiDocumentationBuilder(help, "API"))
                                                       .RegisterBuilder(new MarkdownHelpBuilder(GetType().Assembly, "Inceptum.AppServer.WebApi.Help.")))
                        .AutoDocumentedTypes(getTypesToDocument(), "API/Types")
                );
            config.SetSampleObjects(new Dictionary<Type, object>
            {
                { Samples.ApplicationInstanceInfo.GetType(), Samples.ApplicationInstanceInfo },
                { Samples.HostInfo.GetType(), Samples.HostInfo },
                { Samples.BundleInfo.GetType(), Samples.BundleInfo },

            });
        }

        private static IEnumerable<HelpItem> buildDisclaimer()
        {

            var locale = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            string message;
            string title;

            if (locale != "ru" && locale != "en")
                locale = "ru";

            if (locale == "ru")
            {
                message = "Контракт API не финализирован и может быть изменен или дополнен.";
                title = "Важно";
            }
            else
            {
                message = "API is not finalized and may be extended or amended at any time";
                title = "Disclamer";

            }

            yield return new HelpItem("disclamer")
            {
                Title = title, //HelpResources.Disclaimer_Title,
                Template = null,
                Data = string.Format("<div class=\"w50pc\"><h1><p style='color:red'>{0}</p></h1></div>", message /*HelpResources.Disclaimer_Message*/)
            };
        }
        private static IEnumerable<Type> getTypesToDocument()
        {
            yield return typeof(Application);
            yield return typeof(ApplicationInstanceInfo);
            yield return typeof(HostInfo);
        }

        internal sealed class HostConfiguration
        {
            private string m_Host;

            public HostConfiguration(int port, bool enabled)
            {
                if (port <= 0) throw new ArgumentException("port must be > 0");

                Port = port;
                Enabled = enabled;
            }

            public string Host
            {
                get { return m_Host ?? "*"; }
                set { m_Host = value; }
            }

            public int Port { get; private set; }

            public bool Enabled { get; private set; }
        }

        internal static class Samples
        {
            public static readonly ApplicationInstanceInfo ApplicationInstanceInfo = new ApplicationInstanceInfo()
                {
                    Id = "TestApp",
                    ApplicationId = "TestApp",
                    ApplicationVendor = "Test Company",
                    AutoStart = false,
                    ActualVersion = new Version(1, 2, 3, 456),
                    Version = new Version(1, 2, 3, 456),
                    Environment = "dev",
                    DefaultConfiguration = "test-config",
                    Name = "TestAppInstance"
                };

            public static readonly HostInfo HostInfo = new HostInfo()
                {
                    MachineName = Environment.MachineName,
                    Version = typeof(Samples).Assembly.GetName().Version.ToString(),
                    Name = "AppServer",
                    Mode = "Standalone"
                };

            public static readonly BundleInfo BundleInfo = new BundleInfo()
                {
                    id = "transports",
                    Name = "transports",
                    Configuration = "test-config",
                    Content = "{}",
                    Parent = null,
                    Bundles = new BundleInfo[0],
                    PureContent = "{}"
                };
        }
    }
}