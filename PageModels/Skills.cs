using System.Windows;
using Jaina.WindowModels;

namespace Jaina.PageModels
{
    public partial class Skills
    {
        public Skills() { InitializeComponent(); }

        private void back(object sender, RoutedEventArgs e)
        { MainWindow.backPage(); }
    }
}