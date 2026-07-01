using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HttpsServerWinUI.DependencyInjection;
using HttpsServerWinUI.Preview.ViewModels;
using HttpsServerWinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace HttpsServerWinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();
        }

        public static IServiceProvider Services { get; private set; }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();

            try
            {
                await InitializeComponents();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.UseMiniAppsSdkServices();
            services.AddSingleton<PreviewControlViewModel>();
            services.AddSingleton<WebViewControlViewModel>();
            services.AddSingleton<ICurrentWindowService, CurrentWindowService>();

            return services.BuildServiceProvider();
        }

        private async Task InitializeComponents()
        {
            var uri = new Uri("ms-appx:///Assets/HttpsCertificates/localhostCertificate.pfx");
            var password = "password";

            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            await Services.GetService<IServerInitializationService>().Initialize(file, password);
            Services.GetService<ICurrentWindowService>().SetCurrentWindow(_window);
        }
    }
}
