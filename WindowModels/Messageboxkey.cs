using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Jaina.WindowModels
{
    public partial class Messageboxkey : Window
    {
        public Messageboxkey(string title, string message)
        {
            InitializeComponent();
            this.Title = title;
            MessageText.Text = message;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Сворачивает окно
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Закрывает окно
            this.Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}