using HttpsServerWinUI.LocalServer.Core;
using HttpsServerWinUI.LocalServer.Core.Middleware;
using HttpsServerWinUI.LocalServer.Managers;
using HttpsServerWinUI.LocalServer.ResourcesLocalServer.Controllers;
using HttpsServerWinUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HttpsServerWinUI.DependencyInjection
{
    public static class MiniAppsSdkServiceCollectionExtensions
    {
        public static void UseMiniAppsSdkServices(this IServiceCollection collection)
        {
            collection.AddSingleton<IServerFileStorageManager, ServerFileStorageManager>();
            collection.AddSingleton<StorageResourceGetController>();
            collection.AddSingleton<StorageResourceMultipartDataPostController>();
            collection.AddSingleton<PreflightController>();
            collection.AddSingleton<CorsMiddleware>();
            collection.AddSingleton<HttpsServer>();

            collection.AddTransient<IServerInitializationService, ServerInitializationService>();
        }
    }
}
