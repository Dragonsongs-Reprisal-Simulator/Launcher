using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

//dotnet publish Launcher.sln -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
namespace Launcher
{
    enum LauncherStatus
    {
        failed,
        readyToUpdate,
        downloading,
        installing,
        upToDate,
        notInstalled,
        checkingVersion,
        cannotFetchVersion,
        error
    }


    public partial class MainWindow : Window
    {
        private string versionTextPre = "Current Version {versionNum}";
        private string versionURL = "https://api.github.com/repos/Dragonsongs-Reprisal-Simulator/Simulator/releases/latest";
        private string buildURL = "https://github.com/Dragonsongs-Reprisal-Simulator/Simulator/releases/latest/download/Simulator.zip";
        private Version cloudVersion = Version.init;

        private string rootPath;
        private string simulatorPath;
        private string exePath;
        private string versionPath;
        private string downloadZipPath;
        private Version localVersion;

        private LauncherStatus _status;
        internal LauncherStatus status
        {
            get { return _status; }
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.upToDate:
                        UpdateButton.Content = "Up To Date";
                        break;
                    case LauncherStatus.readyToUpdate:
                        UpdateButton.Content = "Update Available";
                        break;
                    case LauncherStatus.downloading:
                        UpdateButton.Content = "Downloading";
                        break;
                    case LauncherStatus.installing:
                        UpdateButton.Content = "Installing";
                        break;
                    case LauncherStatus.failed:
                        UpdateButton.Content = "!! Failed - Retry";
                        break;
                    case LauncherStatus.notInstalled:
                        UpdateButton.Content = "Ready to Download";
                        //CurrentVersion.Text = "Simulator not Installed";
                        break;
                    case LauncherStatus.error:
                        UpdateButton.Content = "!! Version Mismatch";
                        //CurrentVersion.Text = "Simulator not Installed";
                        break;
                    case LauncherStatus.checkingVersion:
                        UpdateButton.Content = "Checking Version";
                        break;
                    case LauncherStatus.cannotFetchVersion:
                        UpdateButton.Content = "!! Failed - Retry";
                        break;
                    default:
                        break;
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            rootPath = Directory.GetCurrentDirectory();
            exePath = Path.Combine(rootPath, "bin", "Simulator.exe");
            simulatorPath = Path.Combine(rootPath, "bin");
            versionPath = Path.Combine(rootPath, "Version.txt");
            downloadZipPath = Path.Combine(rootPath, "update.zip");
        }

        
        private string versionStringify(Version input)
        {
            return versionTextPre.Replace("{versionNum}", input.ToString());
        }



        private async Task<bool> downloadVersionFile()
        {
            try
            {
                LauncherStatus previous_status = status;
                messageHelper(LauncherStatus.checkingVersion);
                HttpClient client = new HttpClient();
                //client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "DSR Simulator Launcher/0.0.1");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DSR-Simulator-Launcher", "0.0.1"));
                HttpResponseMessage response = await client.GetAsync(versionURL).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                dynamic content = JsonConvert.DeserializeObject<object>(await response.Content.ReadAsStringAsync());
                this.Dispatcher.Invoke((Action)(() =>
                {
                    cloudVersion = new Version((string) content["tag_name"]);
                    status = previous_status;
                }));
                return true;
            }
            catch (Exception e)
            {
                messageHelper(LauncherStatus.cannotFetchVersion);
                MessageBox.Show($"Error happened during Fetching Version process: {e.Message}");
                return false;
            }
        }

        private void messageHelper(LauncherStatus newStatus, string message = null)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                if (status != newStatus)
                {
                    status = newStatus;
                }
                if(message != null)
                {
                    CurrentVersion.Text = message;
                }
            }));
        }
        private async Task<bool> downloadZip()
        {
            try
            {
                messageHelper(LauncherStatus.downloading);

                HttpClient client = new HttpClient();
                var downloaded_file = await client.GetAsync(buildURL).ConfigureAwait(false);

                if (downloaded_file.StatusCode != System.Net.HttpStatusCode.OK) { throw new Exception("Server rejects download request."); }


                using (FileStream fs = new FileStream(downloadZipPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    fs.SetLength(0);
                    await downloaded_file.Content.CopyToAsync(fs);
                }

                return installation();
            }
            catch (Exception e)
            {
                messageHelper(LauncherStatus.failed);
                MessageBox.Show($"Error happened during download process: {e.Message}");
                return false;
            }
        }

        private bool installation()
        {
            try
            {
                messageHelper(LauncherStatus.installing);
                ZipFile.ExtractToDirectory(downloadZipPath, simulatorPath, true);
                File.Delete(downloadZipPath);

                File.WriteAllText(versionPath, cloudVersion.ToString());

                messageHelper(LauncherStatus.upToDate, versionStringify(cloudVersion));

                return true;
            }
            catch (Exception e)
            {
                messageHelper(LauncherStatus.failed);
                MessageBox.Show($"Error happened during installation process: {e.Message}");
                return false;
            }
        }

        private async void AskForDownload(string content)
        {
            // ask user whether to update, return whether update success or fail
            MessageBoxResult whetherToUpdate = MessageBox.Show(content, "Downloader", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (whetherToUpdate == MessageBoxResult.Yes)
            {
                // TODO: download file from the cloud
                var result = await downloadZip();
                if (!result)
                {
                    //status = LauncherStatus.failed;
                }
            }
            else
            {
                status = LauncherStatus.readyToUpdate;
            }
        }

        private void CheckCurrentVersion()
        {
            if (!File.Exists(exePath))
            {
                status = LauncherStatus.notInstalled;
                localVersion = Version.init;
                CurrentVersion.Text = "!! Simulator not Installed";
                return;
            }

            if (!File.Exists(versionPath))
            {
                status = LauncherStatus.notInstalled;
                localVersion = Version.init;
                CurrentVersion.Text = "!! Cannot find version file";
                return;
            }

            localVersion = new Version(File.ReadAllText(versionPath));
            CurrentVersion.Text = versionStringify(localVersion);
        }

        private async Task<bool> CheckForVersion()
        {
            CheckCurrentVersion();

            // if cloudVersion is not fetch, fetch version from cloud and check version
            bool success = await downloadVersionFile();
            //Debug.WriteLine(success);

            if (!success || status == LauncherStatus.notInstalled || localVersion == Version.init)
            {
                return success;
            }
            else if (localVersion < cloudVersion)
            {
                status = LauncherStatus.readyToUpdate;
            }
            else if (localVersion == cloudVersion)
            {
                status = LauncherStatus.upToDate;
            }
            else
            {
                status = LauncherStatus.error;
            }
            return success;

        }

        private async void CheckForUpdate()
        {
            await CheckForVersion();
            
            //Debug.WriteLine(status);

            if (status == LauncherStatus.notInstalled)
            {
                // no need to fetch version file, just download simulator zip
                AskForDownload("Do you want to download simulator?");
            }
            else if (status == LauncherStatus.readyToUpdate)
            {
                // no need to fetch version file, just download simulator zip
                AskForDownload("Do you want to update simulator?");
            }
            else if (status == LauncherStatus.error)
            {
                // no need to fetch version file, just download simulator zip
                AskForDownload($"Version Mismatch detected! Do you want to re-download simulator?\nLocal Version: {localVersion.ToString()} > Cloud Version: {cloudVersion.ToString()}");
            } 
            else if (status == LauncherStatus.failed)
            {
                AskForDownload($"Do you want to retry update/download simulator?");
            }
            else if (status == LauncherStatus.cannotFetchVersion)
            {
                AskForDownload($"Do you want to retry update/download simulator?");
            }
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            //Uri iconUri = new Uri("D:/Program Files/Unity/Project/Dragonsongs Reprisal Simulator/Launcher/Resource/main.ico", UriKind.RelativeOrAbsolute); //make sure your path is correct, and the icon set as Resource
            //this.Icon = BitmapFrame.Create(iconUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            await CheckForVersion();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(exePath) && status != LauncherStatus.notInstalled)
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(exePath);
                    startInfo.WorkingDirectory = rootPath;
                    Process.Start(startInfo);
                    Close();
                }
                catch (Exception execption)
                {
                    MessageBox.Show($"Unable to open the simulator, please download or update the simulator!\n Error message: {execption.Message}");
                }
            }
            else
            {
                MessageBox.Show($"Simulator not detected, please download the simulator!");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdate();
        }

        private void AuthorLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            ProcessStartInfo page_navi = new ProcessStartInfo(e.Uri.AbsoluteUri);
            page_navi.UseShellExecute = true;
            Process.Start(page_navi);
            e.Handled = true;
        }
    }
}
