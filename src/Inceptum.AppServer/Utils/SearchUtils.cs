using System;
using System.Text;

namespace Inceptum.AppServer.Utils
{
    static class SearchUtils
    {
        public static bool ContainsTerm(string value, string term)
        {
            value = Normalize(value);
            var index = value.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            return index > -1;
        }

        public static string GetAnnotationByTerm(string value, string term, int maxCharsArountTerm)
        {
            value = Normalize(value);

            var index = value.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index > -1)
            {
                var startIndex = Math.Max(index - maxCharsArountTerm, 0);
                var endIndex = Math.Min(index + maxCharsArountTerm, value.Length - 1);

                var result = new StringBuilder();
                if (startIndex > 0)
                {
                    result.Append("...");
                }
                result.Append(value.Substring(startIndex, endIndex - startIndex));
                if (endIndex < value.Length - 1)
                {
                    result.Append("...");
                }

                return result.ToString();
            }

            return value;
        }

        static string Normalize(string value)
        {
            return value.Replace("\r", "\0").Replace("\n", "\0").Trim();
        }
    }
}