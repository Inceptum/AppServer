using System;
using System.Text.RegularExpressions;
using OpenRasta.Web;
using OpenRasta.Web.UriDecorators;

namespace Inceptum.AppServer.Management
{
    public class SplitParamsUriDecorator : IUriDecorator
    {
        //private static readonly Regex segmentRegex = new Regex("!(?<method>[a-zA-Z]+)", RegexOptions.Compiled);
        private readonly ICommunicationContext _context;

        public SplitParamsUriDecorator(ICommunicationContext context)
        {
            _context = context;
        }

        #region IUriDecorator Members

        public bool Parse(Uri uri, out Uri processedUri)
        {
            string[] segments = uri.GetSegments();
            if (segments.Length > 3 && segments[1].ToLower() == "configuration/")
            {
                processedUri = new UriBuilder(uri)
                                   {
                                       Path = string.Join("", segments, 0, 4)+(string.Join("", segments, 4, segments.Length - 4)).Replace("/",":")
                                   }.Uri;
                return true;
            }
            
            if (segments.Length > 2 && segments[1].ToLower() == "content/"|| segments[1].ToLower() == "ui/")
            {
                processedUri = new UriBuilder(uri)
                                   {
                                       Path = string.Join("", segments, 0, 2)+(string.Join("", segments, 2, segments.Length - 2)).Replace("/",".")
                                   }.Uri;
                return true;
            }
         
            if (segments.Length > 2 && (segments[1].ToLower() == "files/" ))
            {
                processedUri = new UriBuilder(uri)
                                   {
                                       Path = string.Join("", segments, 0, 2)+(string.Join("", segments, 2, segments.Length - 2)).Replace("/","---")
                                   }.Uri;
                return true;
            }

            processedUri = uri;
            return false;
        }

        public void Apply()
        {
            //_context.Request.HttpMethod = newVerb;
        }

        #endregion
    }
}