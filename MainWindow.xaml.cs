using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace OllamaUsageTracker
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon trayIcon;
        private DispatcherTimer timer;
        private HttpClient httpClient;
        
        private List<double> sessionHistory = new List<double>();
        private List<double> weeklyHistory = new List<double>();
        private const int MaxHistory = 50;
        
        private AppSettings settings;

        public MainWindow()
        {
            InitializeComponent();
            settings = AppSettings.Load();
            Logger.IsDebugEnabled = settings.DebugEnabled;
            
            SetupTrayIcon();
            
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(settings.UpdateIntervalSeconds > 0 ? settings.UpdateIntervalSeconds : 10);
            timer.Tick += Timer_Tick;
            timer.Start();
            
            Timer_Tick(null, EventArgs.Empty);
        }

        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            
            // Try to load SVG icon
            try 
            {
                if (File.Exists("ollama-icon.svg"))
                {
                    var svgDoc = Svg.SvgDocument.Open("ollama-icon.svg");
                    using (var bitmap = svgDoc.Draw())
                    {
                        trayIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                    }
                }
                else
                {
                    trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            } 
            catch (Exception ex)
            {
                Logger.Log("Failed to load SVG icon: " + ex.Message);
                try {
                    trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                } catch {
                    trayIcon.Icon = System.Drawing.SystemIcons.Information;
                }
            }

            trayIcon.Text = "Ollama Usage Tracker";
            trayIcon.Visible = true;
            
            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Show/Hide", null, (s, e) => { this.Visibility = this.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible; });
            menu.Items.Add("Settings", null, (s, e) => { OpenSettings(); });
            menu.Items.Add("Exit", null, (s, e) => { System.Windows.Application.Current.Shutdown(); });
            trayIcon.ContextMenuStrip = menu;
        }

        private void OpenSettings()
        {
            var settingsWin = new SettingsWindow(settings);
            if (settingsWin.ShowDialog() == true)
            {
                Logger.IsDebugEnabled = settings.DebugEnabled;
                timer.Interval = TimeSpan.FromSeconds(settings.UpdateIntervalSeconds > 0 ? settings.UpdateIntervalSeconds : 10);
                Logger.Log($"Settings updated. New interval: {settings.UpdateIntervalSeconds}s");
                Timer_Tick(null, EventArgs.Empty); // trigger immediate fetch
            }
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(settings.CookieValue) || settings.CookieValue.StartsWith("Paste_"))
            {
                txtSession.Text = "Session: No Cookie";
                txtWeekly.Text = "Weekly: No Cookie";
                Logger.Log("Tick: No cookie configured.");
                return;
            }

            try
            {
                Logger.Log("Tick: Fetching data from https://ollama.com/settings");
                var request = new HttpRequestMessage(HttpMethod.Get, "https://ollama.com/settings");
                request.Headers.Add("Cookie", settings.CookieValue);
                
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();
                
                double sessionVal = ExtractPercentage(html, "Session usage");
                double weeklyVal = ExtractPercentage(html, "Weekly usage");
                
                Logger.Log($"Tick: Found Session={sessionVal}%, Weekly={weeklyVal}%");

                if (sessionVal >= 0) sessionHistory.Add(sessionVal);
                if (weeklyVal >= 0) weeklyHistory.Add(weeklyVal);
                
                if (sessionHistory.Count > MaxHistory) sessionHistory.RemoveAt(0);
                if (weeklyHistory.Count > MaxHistory) weeklyHistory.RemoveAt(0);
                
                UpdateUI(sessionVal, weeklyVal);
            }
            catch (Exception ex)
            {
                txtSession.Text = "Session: Error";
                txtWeekly.Text = "Weekly: Error";
                Logger.Log("Tick Error: " + ex.Message);
            }
        }
        
        private double ExtractPercentage(string text, string keyword)
        {
            var match = Regex.Match(text, keyword + @"[^%]{0,100}?([0-9]+(?:\.[0-9]+)?)\s*%", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            return -1;
        }

        private void UpdateUI(double sessionVal, double weeklyVal)
        {
            if (sessionVal >= 0) txtSession.Text = $"{sessionVal}%";
            if (weeklyVal >= 0) txtWeekly.Text = $"{weeklyVal}%";
            
            DrawChart();
        }

        private void DrawChart()
        {
            lineSession.Points.Clear();
            lineWeekly.Points.Clear();
            
            double width = chartCanvas.ActualWidth > 0 ? chartCanvas.ActualWidth : 80;
            double height = chartCanvas.ActualHeight > 0 ? chartCanvas.ActualHeight : 60;
            
            if (width == 0 || height == 0) return;
            
            double stepX = width / Math.Max(1, MaxHistory - 1);
            
            for (int i = 0; i < sessionHistory.Count; i++)
            {
                double x = i * stepX;
                double y = height - (sessionHistory[i] / 100.0 * height);
                lineSession.Points.Add(new System.Windows.Point(x, y));
            }
            
            for (int i = 0; i < weeklyHistory.Count; i++)
            {
                double x = i * stepX;
                double y = height - (weeklyHistory[i] / 100.0 * height);
                lineWeekly.Points.Add(new System.Windows.Point(x, y));
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnClosed(e);
        }
    }
}