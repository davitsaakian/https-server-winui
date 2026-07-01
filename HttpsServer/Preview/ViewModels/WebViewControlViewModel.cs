using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using HttpsServerWinUI.WebView;
using Microsoft.Web.WebView2.Core;

namespace HttpsServerWinUI.Preview.ViewModels
{
    public class WebViewControlViewModel
    {
        private const string WEBVIEW_ASSETS_HOST_NAME = "html.local";
        private MessagingWebView? _webView;

        public void SetWebViewRef(MessagingWebView webView)
        {
            _webView = webView;
        }

        public async Task OpenUploadPage()
        {
            if (_webView == null)
                return;
            await _webView.EnsureCoreWebView2Async();
            var uploadPageFolderPath = Path.Combine(AppContext.BaseDirectory, "Assets", "html");
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(WEBVIEW_ASSETS_HOST_NAME, uploadPageFolderPath, CoreWebView2HostResourceAccessKind.Allow);
            _webView.SetSource($"https://{WEBVIEW_ASSETS_HOST_NAME}/upload-files.html");
        }

        public void WebViewMessageReceived(string id)
        {
            StrongReferenceMessenger.Default.Send(id, "WebMessageReceived");
        }
    }
}
