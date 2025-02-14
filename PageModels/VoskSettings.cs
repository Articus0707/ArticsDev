using System.Diagnostics; // Добавьте эту строку
using System.Windows;
using System.Windows.Controls;
using Jaina.WindowModels;

namespace Jaina.PageModels
{
    public partial class VoskSettings : Page
    {
        public VoskSettings()
        {
            InitializeComponent();
        }

        private void back(object sender, RoutedEventArgs e)
        {
            MainWindow.backPage();
        }

        private void OpenDiscord(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/p5uB5Y2DEJ",
                UseShellExecute = true
            });
        }

        private void OpenWebsite(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://artics-jaina.tilda.ws/",
                UseShellExecute = true
            });
        }
    }
}