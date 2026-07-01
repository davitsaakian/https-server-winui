using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace HttpsServerWinUI.Preview.ViewModels
{
    public sealed partial class PreviewControl : UserControl
    {
        public PreviewControl()
        {
            InitializeComponent();
            ViewModel = App.Services.GetService<PreviewControlViewModel>();
        }

        public PreviewControlViewModel ViewModel { get; }
    }
}
