using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Inceptum.AppServer.Utils
{
    static class SearchUtils
    {
        public static bool ContainsTerm(string value, string term)
        {
            value = Normalize(value);

            int length;
            var index = FindTermIndex(value, term, out length);

            return index > -1;
        }

        public static string GetAnnotationByTerm(string value, string term, int maxCharsAroundTerm)
        {
            value = Normalize(value);

            int length;
            var index = FindTermIndex(value, term, out length);
            if (index > -1)
            {
                var startIndex = Math.Max(index - maxCharsAroundTerm, 0);
                var endIndex = Math.Min(index + maxCharsAroundTerm, value.Length - 1);

                var result = new StringBuilder();
                if (startIndex > 0)
                {
                    result.Append("<b>...</b>");
                }

                var fullV = value.Substring(startIndex, endIndex - startIndex);
                var startV = fullV.Substring(0, index - startIndex);
                var middleV = "<b class='match'>" + fullV.Substring(index - startIndex, length) + "</b>";
                var endV = fullV.Substring(index - startIndex + length);

                result.Append(startV.Trim())
                    .Append(middleV)
                    .Append(endV);

                if (endIndex < value.Length - 1)
                {
                    result.Append("<b>...</b>");
                }

                return result.ToString();
            }

            return value;
        }

        static string Normalize(string value)
        {
            return value.Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
        }

        static int FindTermIndex(string value, string term, out int length)
        {
            if (term == null)
            {
                term = string.Empty;
            }
            term = term.Trim();

            if (term.StartsWith("/") && term.EndsWith("/"))
            {
                try
                {
                    var regex = new Regex(term.Substring(1, term.Length - 2), RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
                    var match = regex.Match(value);
                    if (match.Success)
                    {
                        length = match.Length;
                        return match.Index;
                    }
                }
                catch
                {
                    
                }
            }

            var index = value.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            length = term.Length;
            return index;
        }
    }
}