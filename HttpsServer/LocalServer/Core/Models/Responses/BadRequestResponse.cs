namespace HttpsServerWinUI.LocalServer.Core.Models.Responses
{
    internal class BadRequestResponse : ResponseModel
    {
        public BadRequestResponse()
        {
            StatusCode = 400;
            StatusMessage = "Bad Request";
        }
    }
}
