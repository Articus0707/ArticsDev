using System; // Для базовых типов данных и операций
using System.Diagnostics; // Для работы с процессами
using System.IO; // Для работы с файлами и потоками
using System.Runtime.InteropServices; // Для использования DllImport
using System.Windows.Media; // Для работы с MediaPlayer
using System.Windows.Threading; // Для работы с Dispatcher
using Windows.Media.Control; // Для управления мультимедиа
using NAudio.Wave; // Для работы с аудио
using Newtonsoft.Json.Linq; // Для работы с JSON
using Jaina.PageModels; // Для доступа к моделям страниц
using Vosk; // Для работы с VoskRecognizer
using MessageBox = Jaina.WindowModels.MessageBox; // Для работы с MessageBox
using Settings = Jaina.PageModels.Settings; // Для доступа к настройкам
using Jaina.WindowModels; // Для работы с окнами
using Jaina.Properties; // Для доступа к ресурсам

namespace Jaina.Classes
{
    public class Vosk
    {
        private static Dispatcher? _dispatcher;

        private static VoskRecognizer? _recognizer; // Объект распознавания VOSK
        private static WaveFileWriter? _writer; // Объект записи с микрофона
        private static bool _busy;

        public static string? RecognizedText;
        private static bool _wakeWordStatus;
        private static int _num = 1;

        private static readonly Logger Logger = new();
        private static readonly Stopwatch WakeTimer = new();
        private static readonly WaveInEvent WaveIn = new();

        private enum RecycleFlags : uint;

        private readonly MediaPlayer _player = new();
        private static readonly MessageBox MessageBox = new();

        public static void main()
        {
            // Инициализация модели
            var model = new Model(".\\VoskModel");
            _recognizer = new VoskRecognizer(model, 16000f);

            // Инициализация записи
            WaveIn.WaveFormat = new WaveFormat(16000, 1);
            WaveIn.DataAvailable += WaveInOnDataAvailable;

            // Временный файл записи голоса
            _writer = new WaveFileWriter(".\\voice.wav", WaveIn.WaveFormat);

            Dispatcher currentDispatcher = Dispatcher.CurrentDispatcher;
            _dispatcher = currentDispatcher;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, RecycleFlags dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32", SetLastError = true)]
        private static extern int GetWindowThreadProcessId([In] IntPtr hwnd, [Out] out int lProcessId);

        public static void update()
        {
            if (Properties.Settings.Default.isMuted)
            {
                WakeTimer.Stop();
                WaveIn.StopRecording();
            }
            else
            {
                try
                {
                    WaveIn.StartRecording();
                }
                catch
                {
                    error1();
                    Logger.warn("Jaina couldn't start voice recognition. Microphone access is not allowed.");
                    MainWindow.MicAccess = false;
                }
            }
        }

        public static void error1()
        {
            MessageBox.showMessage(Resources.MessageBox_errorSign, Resources.error1, MessageBox.MessageBoxType.Error, MessageBox.MessageBoxButtons.Ok);
        }

        private static void WaveInOnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);

            var vosk = new Vosk();

            if (_recognizer!.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                // Парсинг объекта с текстом
                var pResult = JObject.Parse(_recognizer.Result());
                RecognizedText = pResult["text"]!.ToString();
                vosk.speechRecognized(); // Проверка результатов
            }
            else
            {
                // Парсинг объекта с текстом
                var pResult = JObject.Parse(_recognizer.PartialResult());
                RecognizedText = pResult["partial"]!.ToString();
                vosk.speechRecognized(); // Проверка результатов
            }
        }

        public void speechRecognized()
        {
            if (RecognizedText != string.Empty)
            {
                Console.WriteLine($@"[VOSK] Recognized > {RecognizedText}");
            }

            if (WakeTimer.Elapsed.Seconds >= 20 && _wakeWordStatus)
            {
                playSound(@".\Assets\Sounds\stop.wav");
                _wakeWordStatus = false;

                _dispatcher?.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)Home.deactivateAnimation);

                Logger.message("Assistant deactivated");

                WakeTimer.Stop();
                WakeTimer.Reset();
            }

            // WAKE WORD
            if (RecognizedText!.Contains("джайна") || RecognizedText == "джайна")
            {
                WakeTimer.Reset();
                WakeTimer.Start();

                if (!_wakeWordStatus)
                {
                    playSound(@".\Assets\Sounds\start.wav");
                    initialMessage("Джайна", "Right");
                    initialMessage("Чем могу помочь?", "Left");

                    _dispatcher?.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)Home.activateAnimation);
                }

                _wakeWordStatus = true;

                Logger.message("Jaina Assistant activated");
            }

            // Спасибо
            if (RecognizedText == "спасибо" && !_busy && _wakeWordStatus)
            {

                _busy = true;
                WakeTimer.Restart();

                initialMessage("Спасибо", "Right");

                playVoice(@".\Assets\Voices\thanksYou.wav");
                initialMessage("Всегда пожалуйста!", "Left");

                Logger.message($"Vosk recognized the phrase - {RecognizedText}");

                RecognizedText = "";

                _recognizer?.Reset();
                _busy = false;
            }

            // Кто ты такая
            if (!string.IsNullOrEmpty(RecognizedText) && RecognizedText.Trim().ToLower().Contains("кто ты такая") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();

                initialMessage("Кто ты", "Right");

                // Проигрывание голоса
                playVoice(@".\Assets\Voices\ijaina.wav");

                initialMessage("Меня создал Артикс. Я голосовой помощник ДЖАЙНА! " +
                    "которая помогает вам с различными задачами. М" +
                    "огу ответить на вопросы, предоставить информацию, по-мочь с управлением " +
                    "программами и многим другим. Чем могу по-мочь", "Left");

                Logger.message($"Vosk recognized the phrase - {RecognizedText}");

                // Очистка распознанного текста
                RecognizedText = "";

                // Сброс распознавателя
                _recognizer?.Reset();

                _busy = false;
            }

            // Алиса
            if (RecognizedText == "алиса" && !_busy)
            {

                _busy = true;
                WakeTimer.Restart();

                initialMessage("Алиса", "Right");

                playVoice(@".\Assets\Voices\denial\alica.wav");
                initialMessage("Я не Алиса! Я Джайна!", "Left");

                _recognizer?.Reset();
                _busy = false;
            }

            // Siri
            if (RecognizedText == "сири" && !_busy)
            {
                _busy = true;
                WakeTimer.Restart();

                initialMessage("Siri", "Right");


                playVoice(@".\Assets\Voices\denial\siri.wav");
                initialMessage("Я не Siri! Я Джайна!", "Left");

                _recognizer?.Reset();
                _busy = false;
            }

            // Маруся
            if (RecognizedText == "маруся" && !_busy)
            {
                _busy = true;
                WakeTimer.Restart();

                initialMessage("Маруся", "Right");

                playVoice(@".\Assets\Voices\denial\marusa.wav");
                initialMessage("Я не Маруся! Я Джайна!", "Left");

                _recognizer?.Reset();
                _busy = false;
            }

            // Очистка корзины
            if (RecognizedText.Contains("очисти корзину") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();

                initialMessage("Очисти корзину", "Right");

                if (Properties.Settings.Default.allowComputerControl)
                {
                    var result = SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                    if (result == 0)
                    {
                        playVoice(@".\Assets\Voices\bin_messages\bin1.wav");
                        initialMessage("Корзина очищена", "Left");
                    }
                    else
                    {
                        playVoice(@".\Assets\Voices\bin_messages\bin2.wav");
                        initialMessage("Корзина уже пуста!", "Left");
                    }
                }
                else
                {
                    playVoice(@".\Assets\Voices\err1.wav");
                    initialMessage("Мне запрещено делать это", "Left");
                }
                _recognizer?.Reset();
                _busy = false;

            }

            // Закрытие процесса в фокусе
            if (RecognizedText.Contains("закрой") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();

                initialMessage("Закрой", "Right");

                if (Properties.Settings.Default.allowComputerControl)
                {
                    playVoice(@".\Assets\Voices\good.wav");
                    initialMessage("Хорошо", "Left");

                    IntPtr hWnd = GetForegroundWindow();
                    GetWindowThreadProcessId(hWnd, out var processId);

                    Process proc = Process.GetProcessById(processId);
                    proc.Kill();
                }
                else
                {
                    playVoice(@".\Assets\Voices\errors\err1.wav");
                    initialMessage("Мне запрещено делать это", "Left");
                }
                _recognizer?.Reset();
                _busy = false;
            }

            // Музыка
            if (RecognizedText.Contains("открой яндекс музыку") && !_busy && _wakeWordStatus)
            {
                string appdt = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                appdt += @"\Programs\YandexMusic\Яндекс Музыка.exe";

                initialMessage("Открой яндекс музыку", "Right");
                startProgram(appdt,
                    @".\Assets\Voices\happyListening.wav",
                    @".\Assets\Voices\err3.wav",
                    1,
                    "Открываю! [Yandex Music]");
            }

            // Запуск ТГ
            if ((RecognizedText.Contains("открой телеграмм")
                 || RecognizedText.Contains("открой телеграм")
                 || RecognizedText.Contains("открой телегу")) && !_busy && _wakeWordStatus)
            {
                string appDataFolder = "%APPDATA%\\Telegram Desktop\\Telegram.exe";
                appDataFolder = Environment.ExpandEnvironmentVariables(appDataFolder);

                initialMessage("Открой телеграм", "Right");
                startProgram(appDataFolder,
                    $@".\Assets\Voices\open\open{_num}.wav",
                    @".\Assets\Voices\err3.wav",
                    4,
                    "Открываю! [Telegram Desktop]");
            }

            if (RecognizedText.Contains("открой консоль") && !_busy && _wakeWordStatus)
            {
                initialMessage("Открой консоль", "Right");
                startProgram("cmd.exe",
                    $@".\Assets\Voices\open\open{_num}.wav",
                    @".\Assets\Voices\err2.wav",
                    3,
                    "Открываю! [Console]");
            }

            if ((RecognizedText.Contains("открой вконтакте")
                 || RecognizedText.Contains("открой мою страницу в контакте")) && !_busy && _wakeWordStatus)
            {
                initialMessage("Открой ВКонтакте", "Right");
                openWebsite("https://vk.com",
                    $@".\Assets\Voices\open\open{_num}.wav",
                    @".\Assets\Voices\err1.wav",
                    "Открываю! [vk.com]");
            }






















            // Обучение
            string[] phrases = {
    "открой мою страницу",
    "открой мою страницу в контакте",
    "перейди на мою страницу"
};

            // Метод для проверки наличия любой из заданных фраз
            bool ContainsPhrase(string text, string[] phrases)
            {
                text = text.Trim().ToLower(); // Очистка текста от лишних пробелов и приведение к нижнему регистру
                foreach (var phrase in phrases)
                {
                    if (text.Contains(phrase))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Проверка распознанного текста
            if (ContainsPhrase(RecognizedText, phrases) && !_busy && _wakeWordStatus)
            {
                initialMessage("Открой мою страницу", "Right");

                // Массив звуковых файлов для разных вариантов
                string[] sounds = new string[]
                {
        @".\Assets\Voices\open\open1.wav",
        @".\Assets\Voices\open\open2.wav",
        @".\Assets\Voices\open\open3.wav"
                };

                // Выбор случайного звукового файла
                Random random = new Random();
                string selectedSound = sounds[random.Next(sounds.Length)];

                openWebsite(
                    "https://vk.com/artics_official",
                    selectedSound,
                    @".\Assets\Voices\err1.wav",
                    "Открываю! [Твою страницу]");
            }

            // Обучение
























            if (RecognizedText.Contains("открой ютуб") && !_busy && _wakeWordStatus)
            {
                initialMessage("Открой YouTube", "Right");
                openWebsite("https://youtube.com",
                    $@".\Assets\Voices\open\open{_num}.wav",
                    @".\Assets\Voices\err1.wav",
                    "Открываю! [youtube.com]");
            }

            if ((RecognizedText.Contains("запусти майнкрафт") || RecognizedText.Contains("открой майн")) && !_busy && _wakeWordStatus)
            {
                initialMessage("Запусти Minecraft", "Right");
                startProgram(@"C:\XboxGames\Minecraft Launcher\Content\Minecraft.exe",
                    $@".\Assets\Voices\open\open{_num}.wav",
                    @".\Assets\Voices\err2.wav",
                    3,
                    "Открываю! [Minecraft Launcher]");
            }

            if (RecognizedText.Contains("открой почту") && !_busy && _wakeWordStatus)
            {
                initialMessage("Открой почту", "Right");
                openWebsite("https://mail.google.com",
                    $@".\Assets\Voices\open\open{_num}.wav",
                    @".\Assets\Voices\err1.wav",
                    "Открываю! [mail.google.com]");
            }

            if (RecognizedText.Contains("поставь на паузу") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();

                if (Properties.Settings.Default.allowComputerControl)
                {
                    playVoice(@".\Assets\Voices\good.wav");
                    initialMessage("Поставь на паузу", "Right");
                    musicInteraction(InteractionVariations.Pause);
                    initialMessage("Хорошо", "Left");
                }
                else
                {
                    playVoice(@".\Assets\Voices\errors\err1.wav");
                    initialMessage("Мне запрещено делать это", "Left");
                }

                _recognizer?.Reset();
                _busy = false;
            }

            if (RecognizedText.Contains("включи обратно") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();
                playVoice(@".\Assets\Voices\happyListening.wav");

                if (Properties.Settings.Default.allowComputerControl)
                {
                    initialMessage("Включи обратно", "Right");
                    musicInteraction(InteractionVariations.Play);
                    initialMessage("Приятного прослушивания", "Left");
                }
                else
                {
                    playVoice(@".\Assets\Voices\errors\err1.wav");
                    initialMessage("Мне запрещено делать это", "Left");
                }

                _recognizer?.Reset();
                _busy = false;
            }

            if (RecognizedText.Contains("предыдущий трек") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();

                if (Properties.Settings.Default.allowComputerControl)
                {
                    playVoice(@".\Assets\Voices\good.wav");
                    initialMessage("Предыдущий трек", "Right");
                    musicInteraction(InteractionVariations.PreviousTrack);
                    initialMessage("Хорошо", "Left");
                }
                else
                {
                    playVoice(@".\Assets\Voices\errors\err1.wav");
                    initialMessage("Мне запрещено делать это", "Left");
                }

                _recognizer?.Reset();
                _busy = false;
            }

            if (RecognizedText.Contains("следующий трек") && !_busy && _wakeWordStatus)
            {
                _busy = true;
                WakeTimer.Restart();

                if (Properties.Settings.Default.allowComputerControl)
                {
                    playVoice(@".\Assets\Voices\good.wav");
                    initialMessage("Следующий трек", "Right");
                    musicInteraction(InteractionVariations.NextTrack);
                    initialMessage("Хорошо", "Left");
                }
                else
                {
                    playVoice(@".\Assets\Voices\errors\err1.wav");
                    initialMessage("Мне запрещено делать это", "Left");
                }

                _recognizer?.Reset();
                _busy = false;
            }
        }

        private enum InteractionVariations
        {
            Play,
            Pause,
            PreviousTrack,
            NextTrack
        }

        private static async void musicInteraction(Enum interactionVariations)
        {
            var mediaTransportManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var mediaSession = mediaTransportManager.GetCurrentSession();

            try
            {
                switch (interactionVariations.ToString())
                {
                    case "Play":
                        await mediaSession.TryPlayAsync();
                        break;
                    case "Pause":
                        await mediaSession.TryPauseAsync();
                        break;
                    case "PreviousTrack":
                        await mediaSession.TrySkipPreviousAsync();
                        break;
                    case "NextTrack":
                        await mediaSession.TrySkipNextAsync();
                        break;
                }
            }
            catch
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }

        private void startProgram(string target, string media, string error, int rndInt, string mesText)
        {
            _busy = true;
            WakeTimer.Restart();

            if (Properties.Settings.Default.allowProgrammsStart)
            {
                try
                {
                    Random rnd = new Random();
                    _num = rnd.Next(1, rndInt);

                    playVoice(media);
                    initialMessage(mesText, "Left");

                    var p = new Process();
                    p.StartInfo.FileName = target;
                    p.Start();
                    RecognizedText = "";
                    _num = 1;

                }
                catch (System.ComponentModel.Win32Exception)
                {
                    playVoice(error);
                    initialMessage("Приложение не найдено на вашем устройстве!", "Right");

                    Logger.error("Jaina was unable to open the program. The program was not found on the device.");

                    RecognizedText = "";
                }
            }
            else
            {
                playVoice(@".\voices\err1.wav");
                initialMessage("Мне запрещено делать это", "Left");
            }

            _recognizer?.Reset();
            _busy = false;
        }

        private void openWebsite(string url, string media, string error, string mesText)
        {
            _busy = true;
            WakeTimer.Restart();

            if (Properties.Settings.Default.allowBrowserStart)
            {
                playVoice(media);
                initialMessage(mesText, "Left");

                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                RecognizedText = "";

            }
            else
            {
                playVoice(error);
                initialMessage("Мне запрещено делать это", "Left");
            }

            _recognizer?.Reset();
            _busy = false;
        }

        private void playSound(string file)
        {
            _player.Open(new Uri(file, UriKind.Relative));
            _player.Volume = Settings.SoundVolume / 100.0f;
            _player.Play();
        }

        private void playVoice(string file)
        {
            _player.Open(new Uri(file, UriKind.Relative));
            _player.Volume = Settings.VoiceVolume / 100.0f;
            _player.Play();
        }

        private void initialMessage(string message, string alignment)
        {
            _dispatcher?.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                Chat.addMessage(message, alignment);
            });
        }
    }
}