namespace HttpsServerWinUI.LocalServer.Core.Models.Responses
{
    internal class SuccessResponse : ResponseModel
    {
        public SuccessResponse()
        {
            StatusCode = 200;
            StatusMessage = "OK";
        }
    }
}
