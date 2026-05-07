using System;
using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace OllamaUsageTracker
{
    public partial class LoginWindow : Window
    {
        public string ExtractedCookie { get; private set; } = "";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure environment is initialized
                await webView.EnsureCoreWebView2Async(null);
                
                // Clear existing cookies to ensure fresh login if needed
                webView.CoreWebView2.CookieManager.DeleteAllCookies();
                
                webView.Source = new Uri("https://ollama.com/signin");
            }
            catch (Exception ex)
            {
                Logger.Log("WebView Initialization Error: " + ex.Message);
                System.Windows.MessageBox.Show("Failed to initialize WebView2. Please make sure WebView2 Runtime is installed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (webView.Source != null && webView.Source.ToString().StartsWith("https://ollama.com/settings", StringComparison.OrdinalIgnoreCase))
            {
                // User successfully logged in and was redirected to settings
                var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync("https://ollama.com");
                var sb = new StringBuilder();
                foreach (var cookie in cookies)
                {
                    sb.Append($"{cookie.Name}={cookie.Value}; ");
                }
                
                ExtractedCookie = sb.ToString().Trim();
                DialogResult = true;
                Close();
            }
        }
    }
}
