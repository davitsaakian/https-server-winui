using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;

namespace HttpsServerWinUI.LocalServer.Core.Models
{
    public class RequestModel
    {
        public HttpMethod Method { get; set; }
        public string LocalUrl { get; set; }
        public NameValueCollection QueryParameters { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public byte[] Body { get; set; }

        public string GetFullUrl(string baseUrl)
        {
            if (QueryParameters == null || QueryParameters.Count == 0)
                return $"{baseUrl}{LocalUrl}";

            var queryString = string.Join("&", QueryParameters.AllKeys.Select(k => $"{k}={QueryParameters[k]}"));
            return $"{baseUrl}{LocalUrl}?{queryString}";
        }
    }
}
