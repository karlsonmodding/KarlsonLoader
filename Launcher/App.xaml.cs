using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainWindow window;
        int filesDownloaded = 0;
        int filesCount = 0;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if(IsAdministrator() && !Environment.GetCommandLineArgs().ToList().Contains("launch") || Environment.GetCommandLineArgs().ToList().Contains("settings"))
            {
                string _checkDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KarlsonLoader");
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KarlsonLoader")))
                {
                    if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InjectorData")) && Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UML")))
                    {
                        // normal install, in cwd
                        _checkDir = AppDomain.CurrentDomain.BaseDirectory;
                    }
                    else
                    {
                        if (Directory.GetFileSystemEntries(AppDomain.CurrentDomain.BaseDirectory).Length == 1)
                        {
                            _checkDir = AppDomain.CurrentDomain.BaseDirectory;
                        }
                    }
                }
                if (!Directory.Exists(_checkDir))
                {
                    MessageBox.Show("Can not enter settings because KarlsonLoader hasn't been initialized.\nInstead, KarlsonLoader will launch normally.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    Settings settingsWindow = new Settings();
                    settingsWindow.Show();
                    return;
                }
            }
            window = new MainWindow();
            window.Show();
            window.Status.Content = "Please wait..\nChecking integrity..";
            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KarlsonLoader");
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
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            // prepare WebClient for https
            if (!Environment.GetCommandLineArgs().ToList().Contains("-disablefilechecks"))
            {
                ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
                WebClient wc = new WebClient();
                /* Download hashtable for file checking
                 * Format:
                 * 
                 * relativeFilePath:hash
                 * ...
                 * 
                 * Example:
                 * InjectorData/injector.exe:123456
                 * InjectorData/LoaderAsm.dll:abc123
                 */
                window.Status.Content = "Please wait\nDownloading hashtable\nand foldertable..";
                string hashtableString;
                hashtableString = wc.DownloadString("https://api.xiloe.fr/karlsonloader/karlsonloader/hashtable");
                Dictionary<string, string> hashtable = new Dictionary<string, string>();
                foreach (string hashdef in hashtableString.Split('\n'))
                {
                    hashtable.Add(hashdef.Split(':')[0].Trim(), hashdef.Split(':')[1].Trim());
                }
                /* Download foldertable for file existance checking
                 * Format:
                 * 
                 * relativeFilePath
                 * ...
                 * 
                 * Example:
                 * InjectorData/
                 * InjectorData/injector.exe
                 * InjectorData/LoaderAsm.dll
                 * 
                 * NOTE:
                 * First folders, then files
                 */
                string foldertableString;
                foldertableString = wc.DownloadString("https://api.xiloe.fr/karlsonloader/karlsonloader/foldertable");
                List<string> filesToReplace = new List<string>();
                window.Status.Content = "Please wait\nChecking files integrity..";
                foreach (string folder in foldertableString.Split('\n'))
                {
                    string path = Path.Combine(dataDir, folder.Trim());
                    if (path.EndsWith("/"))
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path); // create missing directories
                    }
                    else
                    {
                        if (File.Exists(path))
                        {
                            string shaDebug = SHA256CheckSum(path);
                        }
                        if (!File.Exists(path) || !hashtable.ContainsKey(folder.Trim()) || (File.Exists(path) && SHA256CheckSum(path) != hashtable[folder.Trim()]))
                            filesToReplace.Add(folder);
                    }
                }
                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                filesCount = filesToReplace.Count;
                window.Status.Content = "Downloading files..\nFiles downloaded: " + filesDownloaded + "/" + filesCount + "\nCurrent progress: 0%";
                foreach (string file in filesToReplace)
                {
                    string path = Path.Combine(dataDir, file.Trim());
                    if (File.Exists(path))
                        File.Delete(path);
                    await wc.DownloadFileTaskAsync(new Uri("https://api.xiloe.fr/karlsonloader/karlsonloader/files/" + file), path);
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    window.Status.Content = "File checks disabled\nvia CMD arguments.";
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            window.Status.Content = "Starting injector..";
            if (!File.Exists(Path.Combine(dataDir, "InjectorData", "config")))
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Select your karlson install",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Filter = "Karlson Executable|karlson.exe"
                };
                if ((bool)ofd.ShowDialog())
                {
                    File.WriteAllText(Path.Combine(dataDir, "InjectorData", "config"), Path.GetDirectoryName(ofd.FileName));
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            Process krllInjector = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(dataDir, "InjectorData", "KarlsonLoader Injector.exe"),
                    WorkingDirectory = Path.Combine(dataDir, "InjectorData"),
                    UseShellExecute = false,
                }
            };
            krllInjector.StartInfo.EnvironmentVariables.Add("KarlsonLoaderLaunch", "y");
            krllInjector.Start();
            Environment.Exit(0);
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            window.Status.Content = "Downloading files..\nFiles downloaded: " + filesDownloaded + "/" + filesCount + "\nCurrent progress: " + e.ProgressPercentage + "%";
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            filesDownloaded++;
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

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
