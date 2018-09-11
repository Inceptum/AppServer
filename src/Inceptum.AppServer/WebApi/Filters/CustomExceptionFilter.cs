using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using Castle.Core.Logging;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.WebApi.Filters
{
    internal sealed class CustomExceptionFilter : ExceptionFilterAttribute
    {
        private readonly ILogger m_Logger;

        public CustomExceptionFilter(ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            m_Logger = logger;
        }

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception.GetType() == typeof(HttpResponseException)) return;

            string request = actionExecutedContext.Request.BuildLogEntry(actionExecutedContext.ActionContext.ActionDescriptor.ControllerDescriptor.ControllerName, actionExecutedContext.ActionContext.ActionDescriptor.ActionName).GetSynchronousResult();
            m_Logger.ErrorFormat(actionExecutedContext.Exception, request);

            actionExecutedContext.Response = actionExecutedContext.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, null, actionExecutedContext.Exception.Message);

            base.OnException(actionExecutedContext);
        }
    }
    public static class TaskExtensions
    {
        public static T GetSynchronousResult<T>(this Task<T> task)
        {
            //return task.GetAwaiter().GetResult();
            return Task.Run(async () => await task).GetAwaiter().GetResult();
        }
    }
    internal static class HttpRequestMessageExtensions
    {
        private const string CURRENT_ACCEPT_LANGUAGE_PROPERTY_KEY = "REQUEST_ACCEPT_LANGUAGE";
        private static readonly HashSet<string> m_SupportedLanguages = new HashSet<string> { "ru-RU", "en-US" };
        private static readonly HashSet<string> m_GlobalSupportedLanguages = new HashSet<string> { "ru", "en" };

        public static CultureInfo GetCulture(this HttpRequestMessage request)
        {
            if (!request.Properties.ContainsKey(CURRENT_ACCEPT_LANGUAGE_PROPERTY_KEY))
            {
                var language = request.Headers.AcceptLanguage.FirstOrDefault(lang => m_SupportedLanguages.Contains(lang.Value));

                var globalLanguage = request.Headers.AcceptLanguage.FirstOrDefault(lang =>
                {
                    if (lang.Value == null || lang.Value.Length < 2) return false;
                    return m_GlobalSupportedLanguages.Contains(lang.Value.Substring(0, 2));
                });
                var requestCulture = new CultureInfo(language != null ? language.Value : globalLanguage != null ? globalLanguage.Value : "ru");
                request.Properties.Add(CURRENT_ACCEPT_LANGUAGE_PROPERTY_KEY, requestCulture);
            }
            return request.Properties[CURRENT_ACCEPT_LANGUAGE_PROPERTY_KEY] as CultureInfo;
        }

        public static string ToStringRepresentation(this HttpRequestMessage request)
        {
            return new StringBuilder()
                .Append(request.Method.ToString().ToUpperInvariant())
                .Append("\n")
                .Append(request.Content == null || request.Content.Headers == null || request.Content.Headers.ContentMD5 == null ? "" : Convert.ToBase64String(request.Content.Headers.ContentMD5))
                .Append("\n")
                .Append(request.Headers.Date == null ? "" : request.Headers.Date.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append("\n")
                .Append(request.RequestUri.PathAndQuery.ToLowerInvariant())
                .ToString();
        }

        public static async Task<string> BuildLogEntry(this HttpRequestMessage httpRequestMessage, string controllerName, string actionName)
        {
            string route = httpRequestMessage.GetRouteData().Route.RouteTemplate;
            string requestHeader = httpRequestMessage.ToString();
            string requestContent = "EMPTY";
            if (httpRequestMessage.Content != null)
            {
                requestContent = await httpRequestMessage.Content.ReadAsStringAsync();
                requestContent = string.IsNullOrEmpty(requestContent) ? "EMPTY" : requestContent;
            }
            return string.Format("route: {1}, controller:{2}, action:{3}{0}{4}{0}Request Content: {0}{5}", Environment.NewLine, route, controllerName, actionName, requestHeader, requestContent);
        }


        public static HttpResponseMessage CreateErrorResponse(this HttpRequestMessage request, HttpStatusCode statusCode, string field, string messsage)
        {
            var errors = new JObject();
            errors[field ?? "_"] = messsage;
            return request.CreateResponse(statusCode, new ErrorsModel() { Errors = errors });
        }
    }
    /// <summary>
    /// Errors
    /// </summary>
    /// <summary lang="ru">
    /// Ошибки
    /// </summary>
    public class ErrorsModel
    {
        /// <summary>
        /// Errors.
        /// </summary>
        /// <summary lang="ru">
        /// Ошибки.
        /// </summary>
        public JObject Errors { get; set; }
    }

}