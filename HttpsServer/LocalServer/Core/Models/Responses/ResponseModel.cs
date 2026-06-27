using System.Collections.Generic;

namespace HttpsServer.LocalServer.Core.Models.Responses
{
    public class ResponseModel
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public byte[] Body { get; set; }
    }
}
