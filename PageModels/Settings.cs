using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Jaina.Classes;
using System.Diagnostics;
using MessageBox = Jaina.WindowModels.MessageBox;
using Jaina.WindowModels;
using System.IO;

namespace Jaina.PageModels
{
    public partial class Settings
    {
        public static float VoiceVolume = Properties.Settings.Default.voiceVol;
        public static float SoundVolume = Properties.Settings.Default.soundVol;

        private readonly MediaPlayer _player = new();
        private readonly Logger _logger = new();
        private readonly MessageBox _messageBox = new();

        public Settings()
        {
            InitializeComponent();

            // Добавляем проверку состояния активации при загрузке
            if (LoadActivationStatus())
            {
                LicenseKeyLabel.Visibility = Visibility.Collapsed;
                LicenseKeyTextBox.Visibility = Visibility.Collapsed;
                CheckLicenseKeyButton.Visibility = Visibility.Collapsed;
                ActivationLabel.Visibility = Visibility.Visible;
            }

            DevModeBox.IsChecked = Properties.Settings.Default.isDevModeTrue;
            MinimizeToTrayBox.IsChecked = Properties.Settings.Default.isMinimizeToTrayTrue;
            AutoRunBox.IsChecked = Properties.Settings.Default.isAutoRun;
            OpacityBox.IsChecked = Properties.Settings.Default.allowOpacity;
            
            AppStartBox.IsChecked = Properties.Settings.Default.allowProgrammsStart;
            BrowserStartBox.IsChecked = Properties.Settings.Default.allowBrowserStart;
            UsingNetworkBox.IsChecked = Properties.Settings.Default.allowNetworkUsing;
            ComputerControlBox.IsChecked = Properties.Settings.Default.allowComputerControl;
            
            VoiceVolumeSlider.Value = Properties.Settings.Default.voiceVol;
            SoundVolumeSlider.Value = Properties.Settings.Default.soundVol;
            
            NotSaveMessageBox.IsChecked = Properties.Settings.Default.notSaveMessages;
            OffLotMessageWarnBox.IsChecked = Properties.Settings.Default.offLotMessageWarn;
        }
        
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateShortcut(string shortcutFilePath, string targetFilePath);

        private void voiceVolumeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VoiceVolume = (float)VoiceVolumeSlider.Value;

            Properties.Settings.Default.voiceVol = VoiceVolume;
            Properties.Settings.Default.Save();
        }

        private void SaveLicenseKey(string key)
        {
            try
            {
                // Создаем защищенную строку с ключом (простое шифрование)
                string dataToSave = $"{key}|activated";
                string encryptedKey = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(dataToSave));

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.dat");
                File.WriteAllText(filePath, encryptedKey);
            }
            catch (Exception ex)
            {
                _logger.error($"Ошибка при сохранении лицензионного ключа: {ex.Message}");
            }
        }

        // Добавляем метод для проверки активации при запуске
        private bool CheckActivation()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.dat");
                if (File.Exists(filePath))
                {
                    string encryptedData = File.ReadAllText(filePath);
                    string decryptedData = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(encryptedData));

                    return decryptedData.EndsWith("|activated");
                }
            }
            catch (Exception ex)
            {
                _logger.error($"Ошибка при проверке активации: {ex.Message}");
            }
            return false;
        }

        private string LoadLicenseKey()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.dat");
                if (File.Exists(filePath))
                {
                    string encryptedKey = File.ReadAllText(filePath);
                    // Расшифровываем ключ
                    byte[] data = Convert.FromBase64String(encryptedKey);
                    return System.Text.Encoding.UTF8.GetString(data);
                }
            }
            catch (Exception ex)
            {
                _logger.error($"Ошибка при загрузке лицензионного ключа: {ex.Message}");
            }
            return string.Empty;
        }

        private void CheckLicenseKey_Click(object sender, RoutedEventArgs e)
        {
            string enteredKey = LicenseKeyTextBox.Text;

            if (ValidateLicenseKey(enteredKey))
            {
                // Сохраняем статус активации
                SaveLicenseKey(enteredKey);
                SaveActivationStatus(true);

                // Показываем сообщение об успешной активации
                var successMessageBox = new Messageboxkey("Успех", "Ключ активирован успешно!");
                successMessageBox.Show();

                // Скрываем элементы ввода ключа
                LicenseKeyLabel.Visibility = Visibility.Collapsed;
                LicenseKeyTextBox.Visibility = Visibility.Collapsed;
                CheckLicenseKeyButton.Visibility = Visibility.Collapsed;
                LicenseKeyLabel.Visibility = Visibility.Collapsed;

                // После успешной проверки ключа
                ActivationLabel.Visibility = Visibility.Visible;
            }
            else
            {
                var errorMessageBox = new Messageboxkey("Ошибка", "Неверный ключ. Пожалуйста, попробуйте снова.");
                errorMessageBox.ShowDialog();
            }
        }

        private void SaveActivationStatus(bool isActivated)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activation_status.txt");
                File.WriteAllText(filePath, isActivated.ToString());
            }
            catch (Exception ex)
            {
                _logger.error($"Ошибка при сохранении статуса активации: {ex.Message}");
            }
        }

        private bool LoadActivationStatus()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activation_status.txt");
                if (File.Exists(filePath))
                {
                    string status = File.ReadAllText(filePath);
                    return bool.TryParse(status, out bool isActivated) && isActivated;
                }
            }
            catch (Exception ex)
            {
                _logger.error($"Ошибка при загрузке статуса активации: {ex.Message}");
            }
            return false;
        }

        private bool ValidateLicenseKey(string key)
        {
            // Список предустановленных ключей
            List<string> validKeys = new List<string>
    {
        "ABCD-1234-EFGH-5678", // Пример ключа
        "WXYZ-5678-IJKL-1234",
        "TEST"
    };

            return validKeys.Contains(key); // Проверка наличия ключа в списке
        }

        private void voiceVolumeTest(object sender, MouseEventArgs e)
        {
            _player.Open(new Uri(@".\voices\test.wav", UriKind.Relative));
            _player.Volume = VoiceVolume / 100.0f;
            _player.Play();
        }

        private void soundVolumeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SoundVolume = (float)SoundVolumeSlider.Value;

            Properties.Settings.Default.soundVol = SoundVolume;
            Properties.Settings.Default.Save();
        }

        private void soundVolumeTest(object sender, MouseEventArgs e)
        {
            _player.Open(new Uri(@".\sounds\start.wav", UriKind.Relative));
            _player.Volume = SoundVolume / 100.0f;
            _player.Play();
        }

        private void devModeBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isDevModeTrue = true;
            Properties.Settings.Default.Save();
        }
        private void devModeBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isDevModeTrue = false;
            Properties.Settings.Default.Save();
        }

        private void minimizeToTrayBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isMinimizeToTrayTrue = true;
            Properties.Settings.Default.Save();
        }
        private void minimizeToTrayBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isMinimizeToTrayTrue = false;
            Properties.Settings.Default.Save();
        }

        private void appStartBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowProgrammsStart = true;
            Properties.Settings.Default.Save();
        }
        private void appStartBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowProgrammsStart = false;
            Properties.Settings.Default.Save();
        }

        private void browserStartBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowBrowserStart = true;
            Properties.Settings.Default.Save();
        }
        private void browserStartBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowBrowserStart = false;
            Properties.Settings.Default.Save();
        }

        private void usingNetworkBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowNetworkUsing = true;
            Properties.Settings.Default.Save();
        }
        private void usingNetworkBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowNetworkUsing = false;
            Properties.Settings.Default.Save();
        }

        private void computerControlBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowComputerControl = true;
            Properties.Settings.Default.Save();
        }
        private void computerControlBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowComputerControl = false;
            Properties.Settings.Default.Save();
        }

        private void opacityBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowOpacity = true;
            Properties.Settings.Default.Save();
        }
        private void opacityBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.allowOpacity = false;
            Properties.Settings.Default.Save();
        }

        private void addToAutoRun(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isAutoRun = true;
            Properties.Settings.Default.Save();
            var message = "Не удалось создать запись в реестре\n\nКод ошибки: 1423";
            var label = "Что-то пошло не так...";
            
            try
            {
                const string path = @"%AppData%\Microsoft\Windows\Start Menu\Programs\Startup";
                Environment.ExpandEnvironmentVariables(path);

                const string shortcutFilePath = "Assistant Jaina.lnk";
                var targetFilePath = Environment.CurrentDirectory + @"\Assistant Jaina.exe";

                CreateShortcut(shortcutFilePath, targetFilePath);
            }
            catch
            { 
                _messageBox.showMessage(message, label, MessageBox.MessageBoxType.Error, MessageBox.MessageBoxButtons.Ok);
                _logger.error("Jaina was unable to add himself to autostart.");
            }
        }

        private void removeToAutoRun(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isAutoRun = false;
            Properties.Settings.Default.Save();
            var message = "Не удалось изменить/удалить запись в реестре\n\nКод ошибки: 4044";
            var label = "Что-то пошло не так...";
            
            try { }
            catch
            { 
                _messageBox.showMessage(message, label, MessageBox.MessageBoxType.Error, MessageBox.MessageBoxButtons.Ok);
                _logger.error("Jaina was unable to remove himself from startup.");
            }
        }

        private void openLogs(object sender, RoutedEventArgs e)
        { Process.Start("explorer.exe", @".\Logs"); }

        private void clearMessages(object sender, RoutedEventArgs e)
        {
            ChatManager chatManager = new();
            chatManager.clearChat();
        }

        private void voskSettings(object sender, RoutedEventArgs e)
        { MainWindow.getVoskSettingsPage(); }

        private void settingsReset(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.isDevModeTrue = false;
            Properties.Settings.Default.isMinimizeToTrayTrue = false;
            Properties.Settings.Default.isAutoRun = false;
            Properties.Settings.Default.allowOpacity = true;

            Properties.Settings.Default.allowProgrammsStart = true;
            Properties.Settings.Default.allowBrowserStart = true;
            Properties.Settings.Default.allowNetworkUsing = true;
            Properties.Settings.Default.allowComputerControl = true;

            Properties.Settings.Default.voiceVol = 100.0f;
            Properties.Settings.Default.soundVol = 100.0f;

            Properties.Settings.Default.notSaveMessages = false;
            Properties.Settings.Default.offLotMessageWarn = false;
            
            Properties.Settings.Default.Save();

            DevModeBox.IsChecked = Properties.Settings.Default.isDevModeTrue;
            MinimizeToTrayBox.IsChecked = Properties.Settings.Default.isMinimizeToTrayTrue;
            AutoRunBox.IsChecked = Properties.Settings.Default.isAutoRun;
            OpacityBox.IsChecked = Properties.Settings.Default.allowOpacity;
            
            AppStartBox.IsChecked = Properties.Settings.Default.allowProgrammsStart;
            BrowserStartBox.IsChecked = Properties.Settings.Default.allowBrowserStart;
            UsingNetworkBox.IsChecked = Properties.Settings.Default.allowNetworkUsing;
            ComputerControlBox.IsChecked = Properties.Settings.Default.allowComputerControl;
            
            VoiceVolumeSlider.Value = Properties.Settings.Default.voiceVol;
            SoundVolumeSlider.Value = Properties.Settings.Default.soundVol;
            
            NotSaveMessageBox.IsChecked = Properties.Settings.Default.notSaveMessages;
            OffLotMessageWarnBox.IsChecked = Properties.Settings.Default.offLotMessageWarn;
        }
        
        private void notSaveMessagesBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.notSaveMessages = true;
            Properties.Settings.Default.Save();
        }

        private void notSaveMessagesBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.notSaveMessages = false;
            Properties.Settings.Default.Save();
        }
        
        private void offLotMessageWarnBoxChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.offLotMessageWarn = true;
            Properties.Settings.Default.Save();
        }

        private void offLotMessageWarnBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.offLotMessageWarn = false;
            Properties.Settings.Default.Save();
        }
    }
}
