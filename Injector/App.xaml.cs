using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using MInject;
using System.Security.Cryptography;

namespace Injector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        const uint WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private static string karlsonDir;
        private Process karlson;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Environment.GetEnvironmentVariable("KarlsonLoaderLaunch") == null)
            {
                MessageBox((IntPtr)0, "Please launch with KarlsonLoader.exe.", "Error", (uint)(0x00000000L | 0x00000010L | 0x00040000L));
                Environment.Exit(0);
                return;
            }
            if (Process.GetProcessesByName("KarlsonLoader Injector").Length > 1 || Process.GetProcessesByName("KarlsonLoader").Length > 1)
            {
                if (MessageBox((IntPtr)0, "An instance of KarlsonLoader is already running.\nWould you like to kill all instancces of KarlsonLoader and Karlson?", "Error", (uint)(0x00000004L | 0x00000010L | 0x00040000L)) == 6)
                {
                    foreach (Process proc in Process.GetProcessesByName("KarlsonLoader"))
                    {
                        proc.Kill();
                    }
                    foreach (Process proc in Process.GetProcessesByName("KarlsonLoader Injector"))
                    {
                        proc.Kill();
                    }
                    foreach (Process proc in Process.GetProcessesByName("Karlson"))
                    {
                        proc.Kill();
                    }
                }
                Environment.Exit(0);
                return;
            }
            if (Process.GetProcessesByName("Karlson").Length > 0)
            {
                MessageBox((IntPtr)0, "Karlson is already running. Please shutdown all instances of Karlson.", "Error", (uint)(0x00000000L | 0x00000010L | 0x00040000L));
                Environment.Exit(0);
                return;
            }
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config")))
            {
                MessageBox((IntPtr)0, "Couldn't find 'config' file.\nPlease restart KarlsonLoader.", "Error", (uint)(0x00000000L | 0x00000010L | 0x00040000L));
                Environment.Exit(0);
                return;
            }
            karlsonDir = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"));
            if (!File.Exists(Path.Combine(karlsonDir, "Karlson.exe")))
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Update your karlson install directory",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Filter = "Karlson Executable|karlson.exe"
                };
                if ((bool)ofd.ShowDialog())
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"), Path.GetDirectoryName(ofd.FileName));
                }
                else
                    Environment.Exit(0);
            }
            if (File.Exists(Path.Combine(karlsonDir, "UnityCrashHandler32.exe")))
            {
                MessageBox((IntPtr)null, "Your Karlson install is 32-bit, but KarlsonLoader is designed for 64-bit.\nPlease restart KarlsonLoader and select an 64-bit install if possible.", "Error", (uint)(0x00000000L | 0x00000010L));
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"));
                Environment.Exit(0);
                return;
            }

            karlson = new Process
            {
                StartInfo = new ProcessStartInfo(Path.Combine(karlsonDir, "Karlson.exe"))
            };
            karlson.StartInfo.UseShellExecute = false;
            karlson.StartInfo.EnvironmentVariables.Add("KarlsonLoaderDir", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
            karlson.Start();
            while (karlson.MainWindowHandle == (IntPtr)0)
                Thread.Sleep(0);
            PostMessage(karlson.MainWindowHandle, WM_KEYDOWN, (int)System.Windows.Forms.Keys.Enter, 0);
            System.Windows.Forms.NotifyIcon trayIcon = new System.Windows.Forms.NotifyIcon
            {
                BalloonTipText = "KarlsonLoader",
                BalloonTipTitle = "KarlsonLoader",
                Text = "KarlsonLoader",
                Icon = new System.Drawing.Icon("icon.ico")
            };
            trayIcon.Visible = true;
            trayIcon.ShowBalloonTip(0, "KarlsonLoader", "KarlsonLoader minimised to tray.\nIt will shutdown when you exit Karlson.", System.Windows.Forms.ToolTipIcon.None);
            trayIcon.MouseClick += TrayIcon_MouseClick;
            Thread.Sleep(100);

            // run MInject with LoaderAsm.dll
            // Method: LoaderAsm.Loader.Init()
            if (MonoProcess.Attach(karlson, out MonoProcess m_karlson))
            {
                byte[] kmp_assemblyBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LoaderAsm.dll"));
                IntPtr monoDomain = m_karlson.GetRootDomain();
                m_karlson.ThreadAttach(monoDomain);
                m_karlson.SecuritySetMode(0);
                m_karlson.DisableAssemblyLoadCallback();

                IntPtr kmp_rawAssemblyImage = m_karlson.ImageOpenFromDataFull(kmp_assemblyBytes);
                IntPtr kmp_assemblyPointer = m_karlson.AssemblyLoadFromFull(kmp_rawAssemblyImage);
                IntPtr kmp_assemblyImage = m_karlson.AssemblyGetImage(kmp_assemblyPointer);
                IntPtr kmp_classPointer = m_karlson.ClassFromName(kmp_assemblyImage, "LoaderAsm", "Loader");
                IntPtr kmp_methodPointer = m_karlson.ClassGetMethodFromName(kmp_classPointer, "Init");

                m_karlson.RuntimeInvoke(kmp_methodPointer);
                m_karlson.EnableAssemblyLoadCallback();
                m_karlson.Dispose();
            }
            else
            {
                karlson.Kill();
                MessageBox((IntPtr)null, "Couldn't execute MInject.\nPlease retry", "Error", (uint)(0x00000000L | 0x00000010L | 0x00040000L));
                Environment.Exit(0);
                return;
            }

            karlson.WaitForExit();
            trayIcon.Visible = false;
            Environment.Exit(0);
            return;
        }

        private void TrayIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            karlson.Kill();
        }
        public string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = System.Security.Cryptography.SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
            }
        }
    }
}
