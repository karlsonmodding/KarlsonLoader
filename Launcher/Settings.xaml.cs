using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        #region >>> PInvoke
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, String lpString);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        /// <summary>
        /// Contains information about the placement of a window on the screen.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
            /// <para>
            /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
            /// </para>
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowState ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public POINT MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public POINT MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public RECT NormalPosition;

            /// <summary>
            /// Gets the default (empty) value.
            /// </summary>
            public static WINDOWPLACEMENT Default
            {
                get
                {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.Length = Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }


            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        public enum ShowState : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, string lParam);
        public const uint WM_SETTEXT = 0x000C;

        private void InteropSetText(IntPtr iptrHWndDialog, int iControlID, string strTextToSet)
        {
            IntPtr iptrHWndControl = GetDlgItem(iptrHWndDialog, iControlID);
            HandleRef hrefHWndTarget = new HandleRef(null, iptrHWndControl);
            SendMessage(hrefHWndTarget, WM_SETTEXT, IntPtr.Zero, strTextToSet);
        }

        #endregion

        public Settings()
        {
            InitializeComponent();
        }
        string dataDir;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", Path.Combine(dataDir, "UML"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Path.Combine(dataDir, "InjectorData", "config")))
            {
                EditKarlsonSettings_Button.Content = "KarlsonLoader not initialized";
                return;
            }
            string karlsonDir = File.ReadAllText(Path.Combine(dataDir, "InjectorData", "config"));
            Process karlson = new Process
            {
                StartInfo = new ProcessStartInfo(Path.Combine(karlsonDir, "Karlson.exe"))
            };
            karlson.StartInfo.UseShellExecute = false;
            karlson.StartInfo.EnvironmentVariables.Add("KarlsonLoaderDir", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
            karlson.Start();
            while (karlson.MainWindowHandle == (IntPtr)0)
            {
                Thread.Sleep(1);
            }
            InteropSetText(karlson.MainWindowHandle, 1, "Apply");
            InteropSetText(karlson.MainWindowHandle, 2, "Cancel");
            SetWindowText(karlson.MainWindowHandle, "Edit Karlson Configuration");
            while (true)
            {
                if (karlson.MainWindowHandle != (IntPtr)0)
                {
                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                    GetWindowPlacement(karlson.MainWindowHandle, ref wp);
                    if (wp.ShowCmd == ShowState.SW_HIDE)
                        break;
                }
                Thread.Sleep(0);
            }
            Thread.Sleep(5);
            karlson.Kill();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KarlsonLoader");
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KarlsonLoader")))
            {
                if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InjectorData")) && Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UML")))
                {
                    // normal install, in cwd
                    dataDir = AppDomain.CurrentDomain.BaseDirectory;
                }
                else
                {
                    if (Directory.GetFileSystemEntries(AppDomain.CurrentDomain.BaseDirectory).Length == 1)
                    {
                        dataDir = AppDomain.CurrentDomain.BaseDirectory;
                    }
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Path.Combine(dataDir, "InjectorData", "config")))
            {
                EditConfig_Button.Content = "KarlsonLoader not initialized";
                return;
            }
            if(!File.Exists(Path.Combine(dataDir, "UML", "config")))
            {
                EditConfig_Button.Content = "KarlsonLoader not initialized";
                return;
            }
            new Config(File.ReadAllLines(Path.Combine(dataDir, "UML", "config")));
            Config_ShowConsole.IsChecked = Config.config.console;
            Config_UnityLog.IsChecked = Config.config.unitylog;
            Config_LogFile.IsChecked = Config.config.logfile;
            ConfigGrid.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
        }

        public class Config
        {
            public Config(string[] lines)
            {
                foreach (string text in lines)
                {
                    string label = text.Split('=')[0];
                    string value = text.Split('=')[1];
                    switch (label)
                    {
                        case "console":
                            console = value == "true";
                            break;
                        case "unitylog":
                            unitylog = value == "true";
                            break;
                        case "logfile":
                            logfile = value == "true";
                            break;
                        default:
                            break;
                    }
                }
                config = this;
            }

            public static Config config;
            public bool console = true;
            public bool unitylog = true;
            public bool logfile = true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ConfigGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(Path.Combine(dataDir, "UML", "config"), $"console={((bool)Config_ShowConsole.IsChecked ? "true" : "false")}\nunitylog={((bool)Config_UnityLog.IsChecked ? "true" : "false")}\nlogfile={((bool)Config_LogFile.IsChecked ? "true" : "false")}");
            ConfigGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Update your karlson install",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "Karlson Executable|karlson.exe"
            };
            if ((bool)ofd.ShowDialog())
            {
                File.WriteAllText(Path.Combine(dataDir, "InjectorData", "config"), Path.GetDirectoryName(ofd.FileName));
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            using(WebClient wc = new WebClient())
            {
                string checkHash = wc.DownloadString("https://api.xiloe.fr/karlsonloader/karlsonloader/launcher/hash");
                Console.WriteLine(checkHash);
                if (checkHash.Trim() != SHA256CheckSum(System.Reflection.Assembly.GetExecutingAssembly().Location).Trim())
                {
                    wc.DownloadFile(new Uri("https://api.xiloe.frf/karlsonloader/karlsonloader/launcher/update.exe"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.exe"));

                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.bat"), "timeout 5\ncopy update.exe KarlsonLoader.exe / Y\npowershell \"start KarlsonLoader.exe -Args \"settings\" -WindowStyle Hidden\"\ndel update.exe\ntimeout 1\ndel updater.bat");
                    Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.bat"));
                    Process.GetCurrentProcess().Kill();
                    Environment.Exit(0);
                }
            }
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            return true;
        }
        public string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            string karlsonDir = File.ReadAllText(Path.Combine(dataDir, "InjectorData", "config"));
            Process.Start(Path.Combine(karlsonDir, "Karlson.exe"));
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            Process krll = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Arguments = "launch -disablefilechecks"
                }
            };
            krll.Start();
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            Process krll = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Arguments = "launch"
                }
            };
            krll.Start();
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }
    }
}
