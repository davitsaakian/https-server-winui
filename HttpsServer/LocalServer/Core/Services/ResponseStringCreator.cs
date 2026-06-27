using System.Linq;
using System.Text;
using HttpsServer.LocalServer.Core.Models.Responses;

namespace HttpsServer.LocalServer.Core.Services
{
    internal class ResponseStringCreator
    {
        private const string HTTP_PROTOCOL = "HTTP/1.1";
        private const string LINE_BREAK = "\r\n";

        public byte[] CreateResponseBytes(ResponseModel response)
        {
            var parsedStatusLine = $"{HTTP_PROTOCOL} {response.StatusCode} {response.StatusMessage}";
            var parsedHeaders = response.Headers?.Select(header => $"{header.Key}: {header.Value}").ToList();

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(parsedStatusLine);

            if (parsedHeaders != null && parsedHeaders.Count > 0)
            {
                stringBuilder.Append(LINE_BREAK);
                stringBuilder.Append(string.Join(LINE_BREAK, parsedHeaders));
            }

            if (response.Body != null)
            {
                stringBuilder.Append(LINE_BREAK);
                stringBuilder.Append(LINE_BREAK);
                var baseBytes = ConvertToBytes(stringBuilder.ToString());
                var bytesWithBody = baseBytes.Concat(response.Body).ToArray();
                return bytesWithBody;
            }

            stringBuilder.Append(LINE_BREAK);
            stringBuilder.Append(LINE_BREAK);

            return ConvertToBytes(stringBuilder.ToString());
        }

        private byte[] ConvertToBytes(string str) => Encoding.UTF8.GetBytes(str);
    }
}
