using System;
using HttpsServer.LocalServer.Core.Middleware;
using HttpsServer.LocalServer.Managers;
using HttpsServer.LocalServer.ResourcesLocalServer.Controllers;

namespace HttpsServer.DependencyInjection
{
    public static class MiniAppsSdkServiceCollectionExtensions
    {
        public static void UseMiniAppsSdkServices(this IServiceCollection collection)
        {
            UseResourcesServerServices(collection);

            collection.AddTransient<IMiniAppSharedStorageManager, MiniAppSharedStorageManager>();
            collection.AddTransient<IMiniAppsSDKInitializationService, MiniAppsSDKInitializationService>();
        }

        public static void InitializeMiniAppsSdk(this IServiceProvider serviceProvider)
        {
            ServerServiceProvider.Initialize(serviceProvider);
        }

        private static void UseResourcesServerServices(IServiceCollection collection)
        {
            collection.AddSingleton<IServerFileStorageManager, ServerFileStorageManager>();
            collection.AddSingleton<StorageResourceGetController>();
            collection.AddSingleton<StorageResourceMultipartDataPostController>();
            collection.AddSingleton<PreflightController>();
            collection.AddSingleton<CorsMiddleware>();
            collection.AddSingleton<HttpsServer>();
        }
    }
}
