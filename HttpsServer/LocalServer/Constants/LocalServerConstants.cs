using System.Net;

namespace HttpsServerWinUI.LocalServer.Constants
{
    public static class LocalServerConstants
    {
        public static readonly IPAddress LOCAL_ADDRESS = IPAddress.Loopback;
        public const int PORT = 4000;
        public const string STORAGE_ROUTE = "/storage";
        public const string FILE_ID_QUERY_NAME = "file";
        public static readonly string FULL_ADDRESS = $"https://localhost:{PORT}{STORAGE_ROUTE}?{FILE_ID_QUERY_NAME}=";
    }
}
