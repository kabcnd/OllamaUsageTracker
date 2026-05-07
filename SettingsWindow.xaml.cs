using System.Windows;

namespace OllamaUsageTracker
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            
            txtCookie.Text = _settings.CookieValue;
            txtInterval.Text = _settings.UpdateIntervalSeconds.ToString();
            chkDebug.IsChecked = _settings.DebugEnabled;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginWin = new LoginWindow();
            loginWin.Owner = this;
            if (loginWin.ShowDialog() == true)
            {
                txtCookie.Text = loginWin.ExtractedCookie;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _settings.CookieValue = txtCookie.Text.Trim();
            if (int.TryParse(txtInterval.Text, out int interval) && interval > 0) {
                _settings.UpdateIntervalSeconds = interval;
            }
            _settings.DebugEnabled = chkDebug.IsChecked ?? false;
            _settings.Save();
            
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
