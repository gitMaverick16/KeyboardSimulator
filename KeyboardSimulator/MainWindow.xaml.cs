using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KeyboardSimulator
{

    public partial class MainWindow : Window
    {

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string StyleSound = "Alpaca";
        private static double Volume = 50;
        private static bool IsMuted = false;

        private static string EnterPressSoundPath = "/Assets/Audio/Style/Press/ENTER.mp3";
        private static string EnterReleaseSoundPath = "/Assets/Audio/Style/Up/ENTER.mp3";
        private static string SpacePressSoundPath = "/Assets/Audio/Style/Press/SPACE.mp3";
        private static string SpaceReleaseSoundPath = "/Assets/Audio/Style/Up/SPACE.mp3";
        private static string BackspacePressSoundPath = "/Assets/Audio/Style/Press/BACKSPACE.mp3";
        private static string BackspaceReleaseSoundPath = "/Assets/Audio/Style/Up/BACKSPACE.mp3";
        private static string DefaultPressSoundPath = "/Assets/Audio/Style/Press/GENERIC.mp3";
        private static string DefaultReleaseSoundPath = "/Assets/Audio/Style/Up/GENERIC.mp3";

        private static HashSet<int> _pressedKeys = new HashSet<int>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hookID = SetHook(_proc);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var test = EnterPressSoundPath.Replace("Style", StyleSound);
            if (nCode >= 0 && !IsMuted)
            {
                int vkCode = Marshal.ReadInt32(lParam); // virtual key code
                string soundPath = string.Empty;

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (!_pressedKeys.Contains(vkCode))
                    {
                        _pressedKeys.Add(vkCode);
                        soundPath = vkCode switch
                        {
                            0x0D => EnterPressSoundPath.Replace("Style", StyleSound),      // Enter
                            0x20 => SpacePressSoundPath.Replace("Style", StyleSound),      // Space
                            0x08 => BackspacePressSoundPath.Replace("Style", StyleSound),  // Backspace
                            _ => DefaultPressSoundPath.Replace("Style", StyleSound)        // Todas las demás teclas
                        };
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    if (_pressedKeys.Contains(vkCode))
                    {
                        _pressedKeys.Remove(vkCode);
                        soundPath = vkCode switch
                        {
                            0x0D => EnterReleaseSoundPath.Replace("Style", StyleSound),     // Enter
                            0x20 => SpaceReleaseSoundPath.Replace("Style", StyleSound),     // Space
                            0x08 => BackspaceReleaseSoundPath.Replace("Style", StyleSound), // Backspace
                            _ => DefaultReleaseSoundPath.Replace("Style", StyleSound)       // Todas las demás teclas
                        };
                    }
                }

                if (!string.IsNullOrEmpty(soundPath))
                {
                    PlaySound(soundPath);
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void PlaySound(string path)
        {
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri(Directory.GetCurrentDirectory() + path));
            mediaPlayer.Volume = Volume / 100;
            mediaPlayer.Play();
            mediaPlayer.MediaEnded += (s, e) => mediaPlayer.Close();
        }

        private void MuteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsMuted = true;
        }

        private void MuteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsMuted = false;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = (int)VolumeSlider.Value;
        }

        private void SoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StyleSound = (SoundComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        }
    }
}