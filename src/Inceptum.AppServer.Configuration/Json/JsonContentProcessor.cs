using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Configuration.Json
{
    public class JsonContentProcessor : IContentProcessor
    {
        private static void merge(JObject receiver, JObject donor)
        {
            foreach (var property in donor)
            {
                var receiverValue = receiver[property.Key] as JObject;
                var donorValue = property.Value as JObject;
                if (receiverValue != null && donorValue != null)
                {
                    merge(receiverValue, donorValue);
                }
                else
                    receiver[property.Key] = property.Value;
            }
        }

        public bool IsEmptyContent(string content)
        {
            return string.IsNullOrEmpty(content) || content == "{}";
        }

        public string Merge(string parentContent, string childContent)
        {
            var result = JObject.Parse(parentContent);
            merge(result, JObject.Parse(childContent));
            return result.ToString();
        }

        private static JObject diff(JObject original, JObject changed)
        {
            var result= new JObject();
            foreach (var property in changed)
            {
                var originalValue = original[property.Key] as JObject;
                var donorValue = property.Value as JObject;
                if (originalValue != null && donorValue != null)
                {
                    var val = diff(originalValue, donorValue);
                    if (val.HasValues)
                        result[property.Key] = val;
                }
                else if (!property.Value.Equals(original[property.Key]))
                    result[property.Key] = property.Value;
            }
            return result;
        }

        public string Diff(string parentContent, string childContent)
        {
            var result = diff(JObject.Parse(parentContent), JObject.Parse(childContent));
            return result.ToString();
        }

        public string GetEmptyContent()
        {
            return "{}";
        }

    }
}
