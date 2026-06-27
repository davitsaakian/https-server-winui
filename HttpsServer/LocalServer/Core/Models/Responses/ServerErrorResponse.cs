namespace HttpsServer.LocalServer.Core.Models.Responses
{
    internal class ServerErrorResponse : ResponseModel
    {
        public ServerErrorResponse()
        {
            StatusCode = 500;
            StatusMessage = "Server Error";
        }
    }
}
