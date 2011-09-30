using System.Text.RegularExpressions;

namespace Inceptum.AppServer.Configuration
{
    public class ValidationHelper
    {
        private static readonly Regex m_NameRegex = new Regex(@"^[-a-zA-Z0-9_]+([-a-zA-Z0-9_ ]*[-a-zA-Z0-9_])?$", RegexOptions.Compiled);
        private static readonly Regex m_UrlRegexp = new Regex("^(https?)://[-A-Z0-9+&@#/%?=~_|!:,.;]*[A-Z0-9+&@#/%=~_|]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public const string INVALID_NAME_MESSAGE = "name should be not empty string without leading or trailing spaces. Allowed chars are '-','_',' ', english letters and digits";

        public static bool IsValidBundleName(string name)
        {
            return name != null && m_NameRegex.IsMatch(name);
        }

        public static bool IsValidUrl(string configurationServiceUrl)
        {
            return configurationServiceUrl!=null && m_UrlRegexp.IsMatch(configurationServiceUrl);
        }
    }
}