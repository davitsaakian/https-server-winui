namespace HttpsServer.LocalServer.Core.Models.Responses
{
    internal class NotFoundResponse : ResponseModel
    {
        public NotFoundResponse()
        {
            StatusCode = 404;
            StatusMessage = "Not Found";
        }
    }
}
