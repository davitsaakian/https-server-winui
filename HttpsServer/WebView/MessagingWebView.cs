using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HttpsServerWinUI.LocalServer.Constants;
using HttpsServerWinUI.LocalServer.Core.Managers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace HttpsServerWinUI.WebView
{
    internal partial class MessagingWebView : WebView2
    {
        public MessagingWebView()
        {
            ConfigureWebView();
        }

        public Dictionary<string, string>? AdditionalCookies { get; set; }

        public event Action? OnReady;
        public event Action<string>? MessageReceived;

        public Task<string?> RunScript(string script)
        {
            if (string.IsNullOrEmpty(script) || CoreWebView2 == null)
                return Task.FromResult<string?>(null);

            try
            {
                return ExecuteScriptAsync(script).AsTask();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Task.FromResult<string?>(null);
            }
        }

        public Task<string?> SendMessage(string functionName, string message)
        {
            var stringToExecute = $"window.{functionName}(String.raw`{message}`);";
            return RunScript(stringToExecute);
        }

        public void SetSource(string url)
        {
            Source = new Uri(url);
        }

        public void DisposeWebView()
        {
            Source = null;
            WebMessageReceived -= MessagingWebView_WebMessageReceived;
            CoreWebView2Initialized -= MessagingWebView_CoreWebView2Initialized;
            if (CoreWebView2 != null)
            {
                CoreWebView2.ServerCertificateErrorDetected -= MessagingWebView_ServerCertificateErrorDetected;
            }
            Close();
        }

        private void ConfigureWebView()
        {
            WebMessageReceived += MessagingWebView_WebMessageReceived;
            CoreWebView2Initialized += MessagingWebView_CoreWebView2Initialized;
        }

        private void MessagingWebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            CoreWebView2.ServerCertificateErrorDetected += MessagingWebView_ServerCertificateErrorDetected;
        }

        private void MessagingWebView_ServerCertificateErrorDetected(CoreWebView2 sender, CoreWebView2ServerCertificateErrorDetectedEventArgs args)
        {
            var localAddress = LocalServerConstants.LOCAL_ADDRESS;
            var serverCertificateSerial = args.ServerCertificate.DerEncodedSerialNumber;
            var isOurCertificate = HttpsCertificateManager.Instance.CompareDerEncodedSerialNumber(localAddress, serverCertificateSerial);

            if (isOurCertificate)
                args.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
        }

        //chrome.webview.postMessage
        private void MessagingWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var message = args.TryGetWebMessageAsString();
            MessageReceived?.Invoke(message);
        }
    }
}
