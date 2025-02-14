using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Globalization;
using Microsoft.Win32;
using Jaina.Classes;
using Jaina.PageModels;
using static Jaina.PageModels.Chat;
using Control = System.Windows.Forms.Control;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows.Shapes;
using Label = System.Windows.Controls.Label;

namespace Jaina.WindowModels
{
    public partial class MainWindow
    {
        public static ObservableCollection<Messages>? ChatCollection { get; set; }
        public static bool MicAccess = true;

        private static readonly ChatManager ChatManager = new();
        private static MainWindow? Instance { get; set; }

        private static object _previousPage = new Home();

        public MainWindow()
        {
            InitializeComponent();
            
            getChatPage(this, null); // Сдлеано для инициализации коллекции с сообщениями
            ChatManager.deserializeChat();
            getHomePage(this, null);
            Classes.Vosk.main();

            if (Properties.Settings.Default.isMuted)
            {
                Mute.Content = (Image)TryFindResource("MuteImage");
                TrayIconMuteBtn.Header = "Вкл. микрофон";
            }
            else
            {
                Mute.Content = (Image)TryFindResource("MicrophoneImage");
                TrayIconMuteBtn.Header = "Выкл. микрофон";
            }

            Classes.Vosk.update();
            ChatCollection = new ObservableCollection<Messages>();
            Console.WriteLine(@"(C) Copyright Vitali Sannikov (Artics Dev.) 2024-2025");

            if (MicAccess == false)
            {
                Mute.Content = (Image)TryFindResource("MuteImage");
                Mute.Opacity = 0.5;
                Mute.IsEnabled = false;
                
                TrayIconMuteBtn.Header = "Вкл. микрофон";
                TrayIconMuteBtn.IsEnabled = false;
                TrayIconMuteBtn.Opacity = 0.5;
            }
            
            Instance = this;
        }
        
        private void windowLoaded(object sender, RoutedEventArgs e)
        { opacityAnimation(Name, 0, 1, 0.1, 2); }

        private void trayIconClick(object sender, RoutedEventArgs e)
        {
            if (Equals(sender, TrayIconChatBtn))
            {
                getChatPage(TrayIconChatBtn, null);
            }
            if (Equals(sender, TrayIconSettingsBtn))
            {
                getSettingsPage(TrayIconSettingsBtn, null);
            }


            Show();
            WindowState = WindowState.Normal;

            opacityAnimation(Name, 0, 1, 0.3, 2);
        }

        private void trayIconMute(object sender, RoutedEventArgs e)
        {
            if (MicAccess == false)
            {
                Classes.Vosk.error1();
                return;
            }

            if (Properties.Settings.Default.isMuted)
            {
                Properties.Settings.Default.isMuted = false;
                Properties.Settings.Default.Save();

                Mute.Content = (Image)TryFindResource("MicrophoneImage");
                TrayIconMuteBtn.Header = "Выкл. микрофон";
            }
            else
            {
                Properties.Settings.Default.isMuted = true;
                Properties.Settings.Default.Save();

                Mute.Content = (Image)TryFindResource("MuteImage");
                TrayIconMuteBtn.Header = "Вкл. микрофон";
            }

            Classes.Vosk.update();
        }

        private void trayIconClose(object sender, RoutedEventArgs e)
        { Close(); }

        private void movingWindow(object sender, MouseButtonEventArgs e) 
        { DragMove(); }

        private void opacityAnimation(string target, double at, double to, double time, int operation)
        {
            if (Properties.Settings.Default.allowOpacity)
            {
                var storyboardFade = new Storyboard();
                var animation = new DoubleAnimation(at, to, new Duration(TimeSpan.FromSeconds(time)));
                
                switch (operation)
                {
                    case 0:
                        animation.Completed += animationCompletedClose;
                        break;
                    case 1:
                        animation.Completed += animationCompletedHide;
                        break;
                }

                Storyboard.SetTargetName(animation, target);
                Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
                storyboardFade.FillBehavior = FillBehavior.Stop;
                storyboardFade.Children.Add(animation);

                storyboardFade.Begin(this);
            } 
            else
            {
                switch (operation)
                {
                    case 0:
                        Close();
                        break;
                    case 1:
                        Hide();
                        break;
                }
            }
        }

        private void animationCompletedClose(object? sender, EventArgs e)
        {
            TaskbarIcon.Visibility = Visibility.Hidden;
            Close();
        }

        private void animationCompletedHide(object? sender, EventArgs e)
        {
            Hide();
        }

        private void closeWindow(object sender, EventArgs e)
        {
            opacityAnimation(Name, 1, 0, 0.3, Properties.Settings.Default.isMinimizeToTrayTrue ? 1 : 0);
        }
        
        private void minimizeWindow(object sender, EventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void mute(object sender, EventArgs? e)
        {
            if (MicAccess == false)
            {
                Classes.Vosk.error1();
                return;
            }

            if (Properties.Settings.Default.isMuted)
            {
                Properties.Settings.Default.isMuted = false;
                Properties.Settings.Default.Save();

                Mute.Content = (Image)TryFindResource("MicrophoneImage");
            } else
            {
                Properties.Settings.Default.isMuted = true;
                Properties.Settings.Default.Save();

                Mute.Content = (Image)TryFindResource("MuteImage");
            }

            Classes.Vosk.update();
        }

        private void getHomePage(object sender, MouseButtonEventArgs? e)
        {
            removeMarkers();
            MainFrame.Content = new Home();
            HomeBtnMarker.Opacity = 1;
            _previousPage = new Home();
        }
        private void getSettingsPage(object sender, MouseButtonEventArgs? e)
        {
            removeMarkers();
            MainFrame.Content = new Settings();
            SettingsBtnMarker.Opacity = 1;
            _previousPage = new Settings();
        }
        private void getChatPage(object sender, MouseButtonEventArgs? e)
        { 
            removeMarkers();
            MainFrame.Content = new Chat();
            ChatBtnMarker.Opacity = 1;
            _previousPage = new Chat();
        }
        private void getAboutPage(object sender, MouseButtonEventArgs? e)
        {
            removeMarkers();
            MainFrame.Content = new About();
            AboutBtnMarker.Opacity = 1;
            _previousPage = new About();
        }
        public static void getSkillsPage()
        { Instance!.MainFrame.Content = new Skills(); }
        public static void backAboutPage()
        { Instance!.MainFrame.Content = new About(); }
        public static void getVoskSettingsPage()
        { Instance!.MainFrame.Content = new VoskSettings(); }
        public static void backPage()
        { Instance!.MainFrame.Content = _previousPage; }

        private void removeMarkers()
        {
            HomeBtnMarker.Opacity = 0;
            SettingsBtnMarker.Opacity = 0;
            ChatBtnMarker.Opacity = 0;
            AboutBtnMarker.Opacity = 0;
            HomeButton.Tag = null;
            SettingsButton.Tag = null;
            ChatButton.Tag = null;
            AboutButton.Tag = null;
        }
        
        private void authorLink(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo 
            { FileName = "https://vk.com/artics_official", UseShellExecute = true });
        }

        private void homeBtnMouseEnter(object sender, MouseEventArgs e)
        {
            HomeButton.Tag = "Selected";
        }
        private void homeBtnMouseLeave(object sender, MouseEventArgs e)
        {
            HomeButton.Tag = null;
        }

        private void settingsBtnMouseEnter(object sender, MouseEventArgs e)
        {
            SettingsButton.Tag = "Selected";
        }
        private void settingsBtnMouseLeave(object sender, MouseEventArgs e)
        {
            SettingsButton.Tag = null;
        }

        private void chatBtnMouseEnter(object sender, MouseEventArgs e)
        {
            ChatButton.Tag = "Selected";
        }
        private void chatBtnMouseLeave(object sender, MouseEventArgs e)
        {
            ChatButton.Tag = null;
        }

        private void aboutBtnMouseEnter(object sender, MouseEventArgs e)
        {
            AboutButton.Tag = "Selected";
        }
        private void aboutBtnMouseLeave(object sender, MouseEventArgs e)
        {
            AboutButton.Tag = null;
        }

        private void authorMouseEnter(object sender, MouseEventArgs e)
        {
            AuthorLine.Opacity = 1;
        }
        private void authorMouseLeave(object sender, MouseEventArgs e)
        {
            AuthorLine.Opacity = 0;
        }

        private void hotKeys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.M)
            {
                mute(this, null);
            }

            if (e.Key == Key.L && Control.ModifierKeys == Keys.Control)
            {
                Process.Start("explorer.exe", ".\\logs");
            }
        }
    }
}