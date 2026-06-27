using System;
using Microsoft.Extensions.DependencyInjection;

namespace HttpsServer.DependencyInjection
{
    internal static class ServerServiceProvider
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T GetService<T>()
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException(
                    "MiniAppsSdkServiceProvider is not initialized. Call InitializeMiniAppsSdk() after building the service provider."
                );

            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
