using HttpsServerWinUI.Preview.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HttpsServerWinUI.Preview.Views
{
    public sealed partial class WebViewControl : UserControl
    {
        public WebViewControl()
        {
            InitializeComponent();
            Loaded += WebViewControl_Loaded;
            ViewModel = App.Services.GetService<WebViewControlViewModel>();
        }

        public WebViewControlViewModel ViewModel { get; }

        private void WebViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.SetWebViewRef(MessagingWebView);
        }
    }
}
