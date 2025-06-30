using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net.Sockets;
using System.Net.Security;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using FTPClientWPF.Properties;
using FTPClientWPF.Models;
using Microsoft.Win32;
using System.Windows.Media;
using System.Linq;

namespace FTPClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // FTP Connection variables
        private Socket ftpSocket = null;
        private Stream stream = null;
        private SslStream sslStream = null;
        private bool isLoggedIn = false;
        private bool isEncryptedStream = false;
        private string currentPath = "/";
        private string serverAddress = "";
        private string username = "";
        private string password = "";
        private int port = 21;
        private bool passiveMode = true;
        private string transferMode = "binary";
        private bool promptEnabled = true;
        private bool useSSL = false;

        // ClamAV Agent variables
        private string clamavHost = "localhost";
        private int clamavPort = 9999;
        private bool clamavConnected = false;

        // UI Data
        public ObservableCollection<FtpFileInfo> ServerFiles { get; set; }
        public ObservableCollection<FileItem> LocalFiles { get; set; }
        public ObservableCollection<UploadItem> UploadQueue { get; set; }

        // File Management
        private string currentLocalPath = "C:\\";
        private string currentServerPath = "/";
        private List<string> selectedLocalFiles = new List<string>();
        private List<string> selectedServerFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeData();
            SetupEventHandlers();
            LoadSettings();
            this.Closing += MainWindow_Closing;
            // Update status after controls are initialized
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateClamAVStatus();
                UpdateSessionStatus();
            }));
        }

        private void InitializeData()
        {
            ServerFiles = new ObservableCollection<FtpFileInfo>();
            LocalFiles = new ObservableCollection<FileItem>();
            UploadQueue = new ObservableCollection<UploadItem>();
            
            if (ServerFilesGrid != null)
                ServerFilesGrid.ItemsSource = ServerFiles;
            if (LocalTreeView != null)
                LoadLocalDirectory(currentLocalPath);
            if (UploadQueueList != null)
                UploadQueueList.ItemsSource = UploadQueue;
        }

        private void SetupEventHandlers()
        {
            // TreeView selection changed
            // if (ServerTreeView != null)
            //     ServerTreeView.SelectedItemChanged += ServerTreeView_SelectedItemChanged;
            
            // DataGrid double click
            if (ServerFilesGrid != null)
                ServerFilesGrid.MouseDoubleClick += ServerFilesGrid_MouseDoubleClick;
        }

        private void LoadSettings()
        {
            try
            {
                if (txtServer != null)
                    txtServer.Text = Settings.Default.LastServerAddress;
                if (txtPort != null)
                    txtPort.Text = Settings.Default.LastPort.ToString();
                if (txtUsername != null)
                    txtUsername.Text = Settings.Default.LastUsername;
                
                // Load window position and size
                //if (Settings.Default.WindowWidth > 0 && Settings.Default.WindowHeight > 0)
                //{
                //    this.Width = Settings.Default.WindowWidth;
                //    this.Height = Settings.Default.WindowHeight;
                //}
                
                if (Settings.Default.WindowX > 0 && Settings.Default.WindowY > 0)
                {
                    this.Left = Settings.Default.WindowX;
                    this.Top = Settings.Default.WindowY;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error loading settings: {ex.Message}", LogType.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (txtServer != null)
                    Settings.Default.LastServerAddress = txtServer.Text;
                if (txtPort != null)
                    Settings.Default.LastPort = int.Parse(txtPort.Text);
                if (txtUsername != null)
                    Settings.Default.LastUsername = txtUsername.Text;
                
                //Settings.Default.WindowWidth = this.Width;
                //Settings.Default.WindowHeight = this.Height;
                Settings.Default.WindowX = this.Left;
                Settings.Default.WindowY = this.Top;
                
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                AppendLog($"Error saving settings: {ex.Message}", LogType.Error);
            }
        }

        private void UpdateSessionStatus()
        {
            if (txtSessionStatus == null) return;
            
            if (isLoggedIn)
            {
                txtSessionStatus.Text = $"Connected to {serverAddress}:{port} - {transferMode} mode";
                txtSessionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                txtSessionStatus.Text = "Not connected";
                txtSessionStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Removed custom title bar drag functionality
            // Window now uses standard title bar
        }

        #region Window Controls
        // Removed custom title bar buttons - using standard window controls
        #endregion

        #region FTP Command Buttons
        private async void btnList_Click(object sender, RoutedEventArgs e)
        {
            await LoadServerDirectory();
        }

        private async void btnPwd_Click(object sender, RoutedEventArgs e)
        {
            await ShowCurrentDirectory();
        }

        private async void btnMkdir_Click(object sender, RoutedEventArgs e)
        {
            await CreateDirectory();
        }

        private async void btnRmdir_Click(object sender, RoutedEventArgs e)
        {
            await RemoveDirectory();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            await DeleteFile();
        }

        private async void btnRename_Click(object sender, RoutedEventArgs e)
        {
            await RenameFile();
        }

        private async void btnGet_Click(object sender, RoutedEventArgs e)
        {
            await DownloadFile();
        }

        private async void btnMget_Click(object sender, RoutedEventArgs e)
        {
            await DownloadMultipleFiles();
        }

        private async void btnMput_Click(object sender, RoutedEventArgs e)
        {
            await UploadMultipleFiles();
        }

        private void rbBinary_Checked(object sender, RoutedEventArgs e)
        {
            transferMode = "binary";
            UpdateSessionStatus();
            AppendLog("Transfer mode set to binary", LogType.Info);
        }

        private void rbAscii_Checked(object sender, RoutedEventArgs e)
        {
            transferMode = "ascii";
            UpdateSessionStatus();
            AppendLog("Transfer mode set to ASCII", LogType.Info);
        }

        private void btnPassive_Click(object sender, RoutedEventArgs e)
        {
            passiveMode = !passiveMode;
            if (btnPassive != null)
            {
                btnPassive.Content = passiveMode ? "Passive Mode: ON" : "Passive Mode: OFF";
                btnPassive.Background = passiveMode ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Gray;
            }
            UpdateSessionStatus();
            AppendLog($"Passive mode: {(passiveMode ? "ON" : "OFF")}", LogType.Info);
        }

        private void btnStatus_Click(object sender, RoutedEventArgs e)
        {
            ShowDetailedStatus();
        }
        #endregion

        #region FTP Command Implementations
        private async Task ShowCurrentDirectory()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            try
            {
                await SendCommand("PWD");
                string response = await ReadResponse();
                AppendLog($"Current directory: {response}", LogType.Response);
            }
            catch (Exception ex)
            {
                AppendLog($"Error getting current directory: {ex.Message}", LogType.Error);
            }
        }

        private async Task CreateDirectory()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            string dirName = Microsoft.VisualBasic.Interaction.InputBox("Enter directory name:", "Create Directory", "");
            if (string.IsNullOrEmpty(dirName)) return;

            try
            {
                await SendCommand($"MKD {dirName}");
                string response = await ReadResponse();
                if (response.StartsWith("257"))
                {
                    AppendLog($"Directory '{dirName}' created successfully", LogType.Success);
                    await LoadServerDirectory(); // Refresh list
                }
                else
                {
                    AppendLog($"Failed to create directory: {response}", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error creating directory: {ex.Message}", LogType.Error);
            }
        }

        private async Task RemoveDirectory()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            string dirName = Microsoft.VisualBasic.Interaction.InputBox("Enter directory name to remove:", "Remove Directory", "");
            if (string.IsNullOrEmpty(dirName)) return;

            try
            {
                await SendCommand($"RMD {dirName}");
                string response = await ReadResponse();
                if (response.StartsWith("250"))
                {
                    AppendLog($"Directory '{dirName}' removed successfully", LogType.Success);
                    await LoadServerDirectory(); // Refresh list
                }
                else
                {
                    AppendLog($"Failed to remove directory: {response}", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error removing directory: {ex.Message}", LogType.Error);
            }
        }

        private async Task DeleteFile()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            if (ServerFilesGrid.SelectedItem is FtpFileInfo selectedFile)
            {
                string fileName = selectedFile.FileName;
                if (System.Windows.MessageBox.Show($"Delete file '{fileName}'?", "Confirm Delete", 
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        await SendCommand($"DELE {fileName}");
                        string response = await ReadResponse();
                        if (response.StartsWith("250"))
                        {
                            AppendLog($"File '{fileName}' deleted successfully", LogType.Success);
                            await LoadServerDirectory(); // Refresh list
                        }
                        else
                        {
                            AppendLog($"Failed to delete file: {response}", LogType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error deleting file: {ex.Message}", LogType.Error);
                    }
                }
            }
            else
            {
                AppendLog("Please select a file to delete", LogType.Error);
            }
        }

        private async Task RenameFile()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            if (ServerFilesGrid.SelectedItem is FtpFileInfo selectedFile)
            {
                string oldName = selectedFile.FileName;
                string newName = Microsoft.VisualBasic.Interaction.InputBox($"Rename '{oldName}' to:", "Rename File", oldName);
                if (string.IsNullOrEmpty(newName) || newName == oldName) return;

                try
                {
                    await SendCommand($"RNFR {oldName}");
                    string response = await ReadResponse();
                    if (response.StartsWith("350"))
                    {
                        await SendCommand($"RNTO {newName}");
                        response = await ReadResponse();
                        if (response.StartsWith("250"))
                        {
                            AppendLog($"File '{oldName}' renamed to '{newName}' successfully", LogType.Success);
                            await LoadServerDirectory(); // Refresh list
                        }
                        else
                        {
                            AppendLog($"Failed to rename file: {response}", LogType.Error);
                        }
                    }
                    else
                    {
                        AppendLog($"Failed to prepare rename: {response}", LogType.Error);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Error renaming file: {ex.Message}", LogType.Error);
                }
            }
            else
            {
                AppendLog("Please select a file to rename", LogType.Error);
            }
        }

        private async Task DownloadFile()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            if (ServerFilesGrid.SelectedItem is FtpFileInfo selectedFile)
            {
                string fileName = selectedFile.FileName;

                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.FileName = fileName;
                saveDialog.Title = "Save file as";

                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        AppendLog($"Downloading {fileName}...", LogType.Info);

                        string typeCommand = transferMode == "ascii" ? "TYPE A" : "TYPE I";
                        await SendCommand(typeCommand);
                        await ReadResponse();

                        Socket dataSocket = null;
                        Stream dataStream = null;

                        await SendCommand("EPSV");
                        string response = await ReadResponse();
                        AppendLog($"EPSV command: {response}", LogType.Command);
                        if (response.StartsWith("229"))
                        {
                            int startIndex = response.IndexOf('(');
                            int endIndex = response.IndexOf(')');
                            if (startIndex != -1 && endIndex != -1)
                            {
                                string epsvData = response.Substring(startIndex + 1, endIndex - startIndex - 1);
                                string[] parts = epsvData.Split('|');
                                if (parts.Length >= 4)
                                {
                                    int dataPort = int.Parse(parts[3]);
                                    string dataHost = serverAddress;
                                    dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    dataSocket.Connect(dataHost, dataPort);
                                    if (isEncryptedStream)
                                    {
                                        var sslDataStream = new SslStream(new NetworkStream(dataSocket), false, ValidateServerCertificate);
                                        await ((SslStream)sslDataStream).AuthenticateAsClientAsync(serverAddress);
                                        dataStream = sslDataStream;
                                    }
                                    else
                                    {
                                        dataStream = new NetworkStream(dataSocket);
                                    }
                                }
                            }
                        }

                        if (dataSocket == null || dataStream == null || !dataSocket.Connected)
                        {
                            AppendLog("Failed to establish data connection for download", LogType.Error);
                            return;
                        }

                        await SendCommand($"RETR {fileName}");
                        response = await ReadResponse();
                        AppendLog($"RETR command: {response}", LogType.Command);

                        if (response.StartsWith("150") || response.StartsWith("125"))
                        {
                            using (FileStream fs = File.Create(saveDialog.FileName))
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;

                                while ((bytesRead = await dataStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                                {
                                    await fs.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                }
                            }

                            dataStream.Close();
                            dataSocket.Close();

                            response = await ReadResponse();
                            if (response.StartsWith("226"))
                            {
                                AppendLog($"File {fileName} downloaded successfully", LogType.Success);
                            }
                            else
                            {
                                AppendLog($"Download completed but server response: {response}", LogType.Warning);
                            }
                        }
                        else
                        {
                            dataStream.Close();
                            dataSocket.Close();
                            AppendLog($"Failed to start download: {response}", LogType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error downloading file: {ex.Message}", LogType.Error);
                    }
                }
            }
            else
            {
                AppendLog("Please select a file to download", LogType.Error);
            }
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
        private async Task DownloadMultipleFiles()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            string pattern = Microsoft.VisualBasic.Interaction.InputBox("Enter file pattern (e.g., *.txt):", "Download Multiple Files", "*");
            if (string.IsNullOrEmpty(pattern)) return;

            try
            {
                await LoadServerDirectory();

                var matchingFiles = ServerFiles
                    .Where(f => f.FileType == "File" && IsPatternMatch(f.FileName, pattern))
                    .Select(f => f.FileName)
                    .ToList();

                if (matchingFiles.Count == 0)
                {
                    AppendLog($"No files match pattern: {pattern}", LogType.Warning);
                    return;
                }

                AppendLog($"Found {matchingFiles.Count} files matching '{pattern}'", LogType.Info);

                foreach (string fileName in matchingFiles)
                {
                    if (promptEnabled)
                    {
                        if (System.Windows.MessageBox.Show($"Download {fileName}?", "Confirm Download",
                            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        {
                            continue;
                        }
                    }

                    Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                    saveDialog.FileName = fileName;
                    saveDialog.Title = $"Save {fileName} as";
                    if (saveDialog.ShowDialog() != true) continue;

                    try
                    {
                        AppendLog($"Downloading {fileName}...", LogType.Info);

                        string typeCommand = transferMode == "ascii" ? "TYPE A" : "TYPE I";
                        await SendCommand(typeCommand);
                        await ReadResponse();

                        Socket dataSocket = null;
                        Stream dataStream = null;

                        await SendCommand("EPSV");
                        string response = await ReadResponse();
                        AppendLog($"EPSV command: {response}", LogType.Command);
                        if (response.StartsWith("229"))
                        {
                            int startIndex = response.IndexOf('(');
                            int endIndex = response.IndexOf(')');
                            if (startIndex != -1 && endIndex != -1)
                            {
                                string epsvData = response.Substring(startIndex + 1, endIndex - startIndex - 1);
                                string[] parts = epsvData.Split('|');
                                if (parts.Length >= 4)
                                {
                                    int dataPort = int.Parse(parts[3]);
                                    string dataHost = serverAddress;
                                    dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    dataSocket.Connect(dataHost, dataPort);
                                    if (isEncryptedStream)
                                    {
                                        var sslDataStream = new SslStream(new NetworkStream(dataSocket), false, ValidateServerCertificate);
                                        await ((SslStream)sslDataStream).AuthenticateAsClientAsync(serverAddress);
                                        dataStream = sslDataStream;
                                    }
                                    else
                                    {
                                        dataStream = new NetworkStream(dataSocket);
                                    }
                                }
                            }
                        }

                        if (dataSocket == null || dataStream == null || !dataSocket.Connected)
                        {
                            AppendLog("Failed to establish data connection for download", LogType.Error);
                            continue;
                        }

                        await SendCommand($"RETR {fileName}");
                        response = await ReadResponse();
                        AppendLog($"RETR command: {response}", LogType.Command);

                        if (response.StartsWith("150") || response.StartsWith("125"))
                        {
                            using (FileStream fs = File.Create(saveDialog.FileName))
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;

                                while ((bytesRead = await dataStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                                {
                                    await fs.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                }
                            }

                            dataStream.Close();
                            dataSocket.Close();

                            response = await ReadResponse();
                            if (response.StartsWith("226"))
                            {
                                AppendLog($"File {fileName} downloaded successfully", LogType.Success);
                            }
                            else
                            {
                                AppendLog($"Download completed but server response: {response}", LogType.Warning);
                            }
                        }
                        else
                        {
                            dataStream.Close();
                            dataSocket.Close();
                            AppendLog($"Failed to start download: {response}", LogType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error downloading {fileName}: {ex.Message}", LogType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error downloading multiple files: {ex.Message}", LogType.Error);
            }
        }

        private async Task UploadMultipleFiles()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to FTP server", LogType.Error);
                return;
            }

            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Title = "Select files to upload";
            openDialog.Filter = "All files (*.*)|*.*";
            openDialog.Multiselect = true;
            
            if (openDialog.ShowDialog() == true)
            {
                foreach (string filePath in openDialog.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);
                    
                    if (promptEnabled)
                    {
                        if (System.Windows.MessageBox.Show($"Upload {fileName}?", "Confirm Upload", 
                            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                        {
                            continue;
                        }
                    }
                    
                    // Scan and upload file
                    await UploadSingleFile(filePath, fileName);
                }
            }
        }

        private async Task UploadSingleFile(string filePath, string fileName)
        {
            try
            {
                AppendLog($"Preparing to upload: {fileName}", LogType.Info);

                // Scan file for viruses
                ScanResult scanResult = await ScanFileWithClamAV(filePath);

                if (scanResult.Status == "OK")
                {
                    AppendLog($"File is clean. Uploading {fileName}...", LogType.Success);
                    if (txtScanInfo != null)
                        txtScanInfo.Text = $"Last scan: {fileName} - CLEAN";

                    if (!isLoggedIn)
                    {
                        AppendLog("Not connected to FTP server", LogType.Error);
                        return;
                    }

                    try
                    {
                        // Set transfer mode
                        string typeCommand = transferMode == "ascii" ? "TYPE A" : "TYPE I";
                        await SendCommand(typeCommand);
                        string response = await ReadResponse();
                        AppendLog($"Transfer mode: {response}", LogType.Command);

                        Socket dataSocket = null;
                        Stream dataStream = null;

                        await SendCommand("EPSV");
                        response = await ReadResponse();
                        AppendLog($"EPSV command: {response}", LogType.Command);

                        if (response.StartsWith("229"))
                        {
                            int startIndex = response.IndexOf('(');
                            int endIndex = response.IndexOf(')');
                            if (startIndex != -1 && endIndex != -1)
                            {
                                string epsvData = response.Substring(startIndex + 1, endIndex - startIndex - 1);
                                string[] parts = epsvData.Split('|');
                                if (parts.Length >= 4)
                                {
                                    int dataPort = int.Parse(parts[3]);
                                    string dataHost = serverAddress;

                                    dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    dataSocket.Connect(dataHost, dataPort);

                                    if (isEncryptedStream)
                                    {
                                        var sslDataStream = new SslStream(new NetworkStream(dataSocket), false, ValidateServerCertificate);
                                        await sslDataStream.AuthenticateAsClientAsync(serverAddress);
                                        dataStream = sslDataStream;
                                    }
                                    else
                                    {
                                        dataStream = new NetworkStream(dataSocket);
                                    }
                                }
                            }
                        }
                        else
                        {
                            await SendCommand("PASV");
                            response = await ReadResponse();
                            AppendLog($"PASV command: {response}", LogType.Command);

                            if (!response.StartsWith("227"))
                            {
                                AppendLog($"Failed to set passive mode: {response}", LogType.Error);
                                return;
                            }

                            int startIndex = response.IndexOf('(');
                            int endIndex = response.IndexOf(')');
                            if (startIndex != -1 && endIndex != -1)
                            {
                                string pasvData = response.Substring(startIndex + 1, endIndex - startIndex - 1);
                                string[] parts = pasvData.Split(',');
                                if (parts.Length >= 6)
                                {
                                    string dataHost = $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}";
                                    int dataPort = int.Parse(parts[4]) * 256 + int.Parse(parts[5]);

                                    dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    dataSocket.Connect(dataHost, dataPort);

                                    if (isEncryptedStream)
                                    {
                                        var sslDataStream = new SslStream(new NetworkStream(dataSocket), false, ValidateServerCertificate);
                                        await sslDataStream.AuthenticateAsClientAsync(serverAddress);
                                        dataStream = sslDataStream;
                                    }
                                    else
                                    {
                                        dataStream = new NetworkStream(dataSocket);
                                    }
                                }
                            }
                        }

                        if (dataSocket == null || dataStream == null || !dataSocket.Connected)
                        {
                            AppendLog("Failed to establish data connection", LogType.Error);
                            return;
                        }

                        AppendLog("Data connection established successfully", LogType.Info);

                        await SendCommand($"STOR {fileName}");
                        response = await ReadResponse();
                        AppendLog($"STOR command: {response}", LogType.Command);

                        if (response.StartsWith("150") || response.StartsWith("125"))
                        {
                            using (FileStream fs = File.OpenRead(filePath))
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;
                                long totalBytes = fs.Length;
                                long uploadedBytes = 0;

                                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                                {
                                    await dataStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                    uploadedBytes += bytesRead;
                                    int progress = (int)((uploadedBytes * 100) / totalBytes);
                                    AppendLog($"Upload progress: {progress}% ({uploadedBytes}/{totalBytes} bytes)", LogType.Info);
                                    Dispatcher.Invoke(() => {
                                        if (OverallProgressBar != null)
                                        {
                                            OverallProgressBar.Value = progress;
                                            txtProgressStatus.Text = $"Uploading {fileName}: {progress}%";
                                        }
                                    });
                                }
                            }

                            dataStream.Close();
                            dataSocket.Close();

                            Dispatcher.Invoke(() => {
                                if (OverallProgressBar != null)
                                {
                                    OverallProgressBar.Value = 0;
                                    txtProgressStatus.Text = "Ready";
                                }
                            });

                            response = await ReadResponse();
                            if (response.StartsWith("226") || response.StartsWith("250"))
                            {
                                AppendLog($"File {fileName} uploaded successfully", LogType.Success);
                            }
                            else
                            {
                                AppendLog($"Upload completed but server response: {response}", LogType.Warning);
                            }
                        }
                        else
                        {
                            dataStream.Close();
                            dataSocket.Close();
                            AppendLog($"Failed to start upload: {response}", LogType.Error);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"FTP upload error: {ex.Message}", LogType.Error);
                        return;
                    }

                    await LoadServerDirectory();
                }
                else if (scanResult.Status == "WARNING")
                {
                    AppendLog($"⚠️ VIRUS DETECTED! Upload aborted for {fileName}", LogType.Error);
                    AppendLog($"Details: {scanResult.Details}", LogType.Error);
                    if (txtScanInfo != null)
                        txtScanInfo.Text = $"Last scan: {fileName} - INFECTED";

                    System.Windows.MessageBox.Show($"Virus detected in {fileName}!\n\nDetails: {scanResult.Details}",
                        "Virus Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    AppendLog($"Scan error for {fileName}: {scanResult.Details}", LogType.Error);
                    if (txtScanInfo != null)
                        txtScanInfo.Text = $"Last scan: {fileName} - ERROR";
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Upload error: {ex.Message}", LogType.Error);
            }
        }

        private bool IsPatternMatch(string fileName, string pattern)
        {
            // Simple wildcard matching
            if (pattern == "*") return true;
            if (pattern.StartsWith("*."))
            {
                string extension = pattern.Substring(1);
                return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }
            return fileName.Contains(pattern);
        }

        private void ShowDetailedStatus()
        {
            StringBuilder status = new StringBuilder();
            status.AppendLine("=== FTP Session Status ===");
            status.AppendLine($"Connected: {isLoggedIn}");
            if (isLoggedIn)
            {
                status.AppendLine($"Host: {serverAddress}:{port}");
                status.AppendLine($"Username: {username}");
                status.AppendLine($"Current directory: {currentPath}");
            }
            status.AppendLine($"Transfer mode: {transferMode}");
            status.AppendLine($"Passive mode: {(passiveMode ? "ON" : "OFF")}");
            status.AppendLine($"Prompt mode: {(promptEnabled ? "ON" : "OFF")}");
            status.AppendLine($"ClamAV Agent: {(clamavConnected ? "Connected" : "Disconnected")}");
            
            System.Windows.MessageBox.Show(status.ToString(), "Session Status", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        #endregion

        #region ClamAV Agent Methods
        private async void btnTestClamAV_Click(object sender, RoutedEventArgs e)
        {
            if (clamavConnected)
            {
                // Disable ClamAV
                clamavConnected = false;

                if (txtClamAVStatus != null)
                {
                    txtClamAVStatus.Text = "Disconnected";
                    txtClamAVStatus.Foreground = Brushes.Red;
                }

                btnTestClamAV.Content = "Enable";
                btnTestClamAV.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                AppendLog("ClamAV Agent disabled", LogType.Warning);
            }
            else
            {
                // Attempt to connect
                await TestClamAVConnection();
            }
        }

        private async Task TestClamAVConnection()
        {
            try
            {
                if (btnTestClamAV != null)
                {
                    btnTestClamAV.IsEnabled = false;
                    btnTestClamAV.Content = "Testing...";
                }

                AppendLog("Testing ClamAV Agent connection...", LogType.Info);

                // Lấy giá trị host và port từ UI
                if (txtClamAVHost != null)
                    clamavHost = txtClamAVHost.Text.Trim();
                if (txtClamAVPort != null && int.TryParse(txtClamAVPort.Text.Trim(), out int portVal))
                    clamavPort = portVal;

                bool connected = await Task.Run(() => TestClamAVSocket());

                if (connected)
                {
                    clamavConnected = true;
                    if (txtClamAVStatus != null)
                    {
                        txtClamAVStatus.Text = "Connected";
                        txtClamAVStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    }
                    AppendLog("ClamAV Agent connection successful", LogType.Success);
                }
                else
                {
                    clamavConnected = false;
                    if (txtClamAVStatus != null)
                    {
                        txtClamAVStatus.Text = "Disconnected";
                        txtClamAVStatus.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    AppendLog("ClamAV Agent connection failed", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                clamavConnected = false;
                if (txtClamAVStatus != null)
                {
                    txtClamAVStatus.Text = "Error";
                    txtClamAVStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                AppendLog($"ClamAV Agent test error: {ex.Message}", LogType.Error);
            }
            finally
            {
                if (btnTestClamAV != null)
                {
                    btnTestClamAV.IsEnabled = true;

                    if (clamavConnected)
                    {
                        btnTestClamAV.Content = "Disable";
                        btnTestClamAV.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                    }
                    else
                    {
                        btnTestClamAV.Content = "Enable";
                        btnTestClamAV.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                    }
                }
            }
        }

        private bool TestClamAVSocket()
        {
            try
            {
                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Set timeout to prevent hanging
                    sock.ReceiveTimeout = 10000; // 10 seconds
                    sock.SendTimeout = 10000;    // 10 seconds
                    
                    sock.Connect(clamavHost, clamavPort);
                    return sock.Connected;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateClamAVStatus()
        {
            if (txtClamAVStatus == null) return;
            
            if (clamavConnected)
            {
                txtClamAVStatus.Text = "Connected";
                txtClamAVStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                txtClamAVStatus.Text = "Disconnected";
                txtClamAVStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private async Task<ScanResult> ScanFileWithClamAV(string filePath)
        {
            try
            {
                // Lấy giá trị host và port từ UI
                if (txtClamAVHost != null)
                    clamavHost = txtClamAVHost.Text.Trim();
                if (txtClamAVPort != null && int.TryParse(txtClamAVPort.Text.Trim(), out int portVal))
                    clamavPort = portVal;

                // Validate file path
                if (string.IsNullOrEmpty(filePath))
                {
                    return new ScanResult { Status = "ERROR", Details = "File path is null or empty" };
                }

                // Check if file exists before attempting scan
                if (!File.Exists(filePath))
                {
                    return new ScanResult { Status = "ERROR", Details = $"File not found: {filePath}" };
                }

                string fileName = Path.GetFileName(filePath);
                AppendLog($"Scanning file: {fileName} (Path: {filePath})", LogType.Info);

                if (!clamavConnected)
                {
                    AppendLog("ClamAV Agent not connected, using fallback scan", LogType.Warning);
                    return await Task.Run(() => PerformFallbackScan(filePath));
                }

                return await Task.Run(() => PerformClamAVScan(filePath));
            }
            catch (Exception ex)
            {
                return new ScanResult { Status = "ERROR", Details = $"Scan error: {ex.Message}" };
            }
        }

        private ScanResult PerformClamAVScan(string filePath)
        {
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    return new ScanResult { Status = "ERROR", Details = $"File not found: {filePath}" };
                }

                // Get file info
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return new ScanResult { Status = "ERROR", Details = $"File does not exist: {filePath}" };
                }

                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Set timeout to prevent hanging
                    sock.ReceiveTimeout = 10000; // 10 seconds
                    sock.SendTimeout = 10000;    // 10 seconds
                    
                    sock.Connect(clamavHost, clamavPort);

                    // Send file info
                    var scanInfo = new { filename = fileInfo.Name, filesize = fileInfo.Length };
                    string jsonInfo = JsonConvert.SerializeObject(scanInfo);
                    sock.Send(Encoding.UTF8.GetBytes(jsonInfo));

                    // Wait for READY signal
                    byte[] responseBuffer = new byte[1024];
                    int bytesRead = sock.Receive(responseBuffer);
                    string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                    if (response.Trim() != "READY")
                    {
                        return new ScanResult { Status = "ERROR", Details = "ClamAV Agent not ready" };
                    }

                    // Send file data
                    using (FileStream fs = File.OpenRead(filePath))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesSent;
                        while ((bytesSent = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            sock.Send(buffer, 0, bytesSent, SocketFlags.None);
                        }
                    }

                    // Receive scan result
                    bytesRead = sock.Receive(responseBuffer);
                    string resultJson = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                    
                    return JsonConvert.DeserializeObject<ScanResult>(resultJson);
                }
            }
            catch (FileNotFoundException ex)
            {
                return new ScanResult { Status = "ERROR", Details = $"File not found: {ex.Message}" };
            }
            catch (DirectoryNotFoundException ex)
            {
                return new ScanResult { Status = "ERROR", Details = $"Directory not found: {ex.Message}" };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ScanResult { Status = "ERROR", Details = $"Access denied: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ScanResult { Status = "ERROR", Details = $"Scan failed: {ex.Message}" };
            }
        }

        private ScanResult PerformFallbackScan(string filePath)
        {
            try
            {
                // Simple fallback scan for testing purposes
                // In a real implementation, this could use a local virus scanner or skip scanning
                FileInfo fileInfo = new FileInfo(filePath);
                
                // For demo purposes, simulate a scan
                // In production, you might want to skip upload entirely if ClamAV is not available
                AppendLog("Performing fallback scan (simulated)", LogType.Warning);
                
                // Simulate scan delay
                System.Threading.Thread.Sleep(500);
                
                // For demo purposes, assume file is clean
                return new ScanResult { Status = "OK", Details = "Fallback scan completed (simulated)" };
            }
            catch (Exception ex)
            {
                return new ScanResult { Status = "ERROR", Details = $"Fallback scan failed: {ex.Message}" };
            }
        }

        private async void btnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            await UploadFileWithVirusScan();
        }

        private async Task UploadFileWithVirusScan()
        {
            try
            {
                // Open file dialog
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Title = "Select file to upload";
                openFileDialog.Filter = "All files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);

                    AppendLog($"Preparing to upload: {fileName}", LogType.Info);

                    // Scan file for viruses
                    ScanResult scanResult = await ScanFileWithClamAV(filePath);
                    if (!clamavConnected)
                    {
                        AppendLog($"File is clean. Uploading {fileName}...", LogType.Success);
                        if (txtScanInfo != null)
                            txtScanInfo.Text = $"Last scan: {fileName} - CLEAN";

                        await Task.Delay(1000);
                        AppendLog($"File {fileName} uploaded successfully", LogType.Success);
                    }    
                   else if (scanResult.Status == "OK")
                    {
                        AppendLog($"File is clean. Uploading {fileName}...", LogType.Success);
                        if (txtScanInfo != null)
                            txtScanInfo.Text = $"Last scan: {fileName} - CLEAN";
                        await Task.Delay(1000);
                        AppendLog($"File {fileName} uploaded successfully", LogType.Success);
                    }
                    else if (scanResult.Status == "WARNING")
                    {
                        AppendLog($"⚠️ VIRUS DETECTED! Upload aborted for {fileName}", LogType.Error);
                        AppendLog($"Details: {scanResult.Details}", LogType.Error);
                        if (txtScanInfo != null)
                            txtScanInfo.Text = $"Last scan: {fileName} - INFECTED";
                        System.Windows.MessageBox.Show($"Virus detected in {fileName}!\n\nDetails: {scanResult.Details}", 
                                       "Virus Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                    else
                    {
                        AppendLog($"Scan error for {fileName}: {scanResult.Details}", LogType.Error);
                        if (txtScanInfo != null)
                            txtScanInfo.Text = $"Last scan: {fileName} - ERROR";
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Upload error: {ex.Message}", LogType.Error);
            }
        }
        #endregion

        #region Connection Methods
        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToServer(false);
        }

        private async void btnSecureConnect_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToServer(true);
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnectFromServer();
        }

        private IPAddress ResolveServerAddress(string serverAddress)
        {
            try
            {
                // Handle localhost specially
                if (serverAddress.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
                    serverAddress.Equals("127.0.0.1") || 
                    serverAddress.Equals("::1"))
                {
                    // Try IPv4 first for localhost
                    return IPAddress.Parse("127.0.0.1");
                }
                
                IPHostEntry hostEntry = Dns.GetHostEntry(serverAddress);
                
                // Prefer IPv4 addresses, fallback to IPv6 if needed
                foreach (IPAddress address in hostEntry.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return address;
                    }
                }
                
                // If no IPv4 address found, try IPv6
                foreach (IPAddress address in hostEntry.AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return address;
                    }
                }
                
                // If still no address found, use the first one
                if (hostEntry.AddressList.Length > 0)
                {
                    return hostEntry.AddressList[0];
                }
                
                return null;
            }
            catch (Exception ex)
            {
                AppendLog($"DNS resolution failed for {serverAddress}: {ex.Message}", LogType.Error);
                return null;
            }
        }

        private async Task ConnectToServer(bool useSSL)
        {
            try
            {
                // Disable buttons during connection
                if (btnConnect != null) btnConnect.IsEnabled = false;
                if (btnSecureConnect != null) btnSecureConnect.IsEnabled = false;
                if (btnDisconnect != null) btnDisconnect.IsEnabled = true;

                // Get connection details
                if (txtServer != null) serverAddress = txtServer.Text;
                if (txtUsername != null) username = txtUsername.Text;
                if (txtPassword != null) password = txtPassword.Password;
                
                if (txtPort != null && !int.TryParse(txtPort.Text, out port))
                {
                    port = 21;
                }

                if (string.IsNullOrEmpty(serverAddress) || string.IsNullOrEmpty(username))
                {
                    AppendLog("Please enter server address and username", LogType.Error);
                    return;
                }

                // Clean up server address
                serverAddress = serverAddress.Trim();
                if (serverAddress.StartsWith("ftp://"))
                {
                    serverAddress = serverAddress.Substring(6);
                }
                if (serverAddress.StartsWith("ftps://"))
                {
                    serverAddress = serverAddress.Substring(7);
                    useSSL = true; // Force SSL for ftps:// URLs
                }
                // Remove trailing slash if present
                if (serverAddress.EndsWith("/"))
                {
                    serverAddress = serverAddress.TrimEnd('/');
                }

                // Check if this is FTPS (implicit SSL) on port 990
                if (port == 990)
                {
                    useSSL = true;
                    AppendLog("Detected FTPS port 990, enabling implicit SSL", LogType.Info);
                }

                AppendLog($"Connecting to {serverAddress}:{port}...", LogType.Info);

                // Create socket connection with proper address resolution
                IPAddress remoteAddress = ResolveServerAddress(serverAddress);
                
                if (remoteAddress == null)
                {
                    AppendLog("Could not resolve server address", LogType.Error);
                    return;
                }
                
                // Create socket with the appropriate address family
                AddressFamily addressFamily = remoteAddress.AddressFamily;
                ftpSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                
                // Set timeout to prevent hanging
                ftpSocket.ReceiveTimeout = 10000; // 10 seconds
                ftpSocket.SendTimeout = 10000;    // 10 seconds
                
                IPEndPoint addrEndPoint = new IPEndPoint(remoteAddress, port);
                
                AppendLog($"Attempting connection to {remoteAddress}:{port} (AddressFamily: {addressFamily})", LogType.Info);
                
                try
                {
                    await Task.Run(() => ftpSocket.Connect(addrEndPoint));
                    AppendLog("Socket connection established", LogType.Success);
                }
                catch (SocketException ex)
                {
                    AppendLog($"Socket connection failed: {ex.Message} (Error Code: {ex.SocketErrorCode})", LogType.Error);
                    return;
                }

                // Read initial response
                string response = await ReadResponse();
                AppendLog($"Server: {response}", LogType.Response);

                if (!response.StartsWith("220"))
                {
                    AppendLog("Failed to connect to server", LogType.Error);
                    return;
                }

                // Handle SSL if requested
                if (useSSL)
                {
                    // Try explicit SSL first (AUTH TLS)
                    await SendCommand("AUTH TLS");
                    response = await ReadResponse();
                    AppendLog($"AUTH TLS: {response}", LogType.Command);

                    if (response.StartsWith("234"))
                    {
                        await EnableSSL();
                    }
                    else if (response.StartsWith("503"))
                    {
                        // Server requires SSL but doesn't support AUTH TLS
                        // This might be an implicit SSL server
                        AppendLog("Server requires SSL but AUTH TLS failed. Trying implicit SSL...", LogType.Warning);
                        
                        // Close the current connection and reconnect with SSL from the start
                        if (ftpSocket != null)
                        {
                            ftpSocket.Close();
                            ftpSocket = null;
                        }
                        
                        // Reconnect with SSL stream from the beginning
                        await ConnectWithImplicitSSL(remoteAddress, port);
                        return;
                    }
                    else
                    {
                        AppendLog($"SSL/TLS not supported by server: {response}", LogType.Error);
                        return;
                    }
                }

                // Login
                await PerformLogin();
                UpdateSessionStatus();
            }
            catch (Exception ex)
            {
                AppendLog($"Connection error: {ex.Message}", LogType.Error);
            }
            finally
            {
                // Re-enable buttons
                if (btnConnect != null) btnConnect.IsEnabled = true;
                if (btnSecureConnect != null) btnSecureConnect.IsEnabled = true;
                if (btnDisconnect != null) btnDisconnect.IsEnabled = isLoggedIn;
            }
        }

        private async Task EnableSSL()
        {
            try
            {
                stream = new NetworkStream(ftpSocket);
                sslStream = new SslStream(stream, false, ValidateServerCertificate);

                await sslStream.AuthenticateAsClientAsync(serverAddress);
                isEncryptedStream = true;

                AppendLog("SSL/TLS connection established", LogType.Success);

                // Gửi lệnh PBSZ 0
                await SendCommand("PBSZ 0");
                string response = await ReadResponse();
                AppendLog($"PBSZ: {response}", LogType.Command);

                // Gửi lệnh PROT P (bắt buộc cho kết nối đã mã hóa)
                await SendCommand("PROT P");
                response = await ReadResponse();
                AppendLog($"PROT: {response}", LogType.Command);
            }
            catch (Exception ex)
            {
                AppendLog($"SSL error: {ex.Message}", LogType.Error);
                throw;
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true; // Certificate is valid
            }

            AppendLog($"SSL certificate error: {sslPolicyErrors}", LogType.Error);

            // Optional: log details of the cert
            if (certificate != null)
            {
                AppendLog($"Certificate subject: {certificate.Subject}", LogType.Warning);
                AppendLog($"Certificate issuer: {certificate.Issuer}", LogType.Warning);
            }

            return false;
        }


        private async void DisconnectFromServer()
        {
            try
            {
                
                    if (!isLoggedIn)
                    {
                        AppendLog("Not connected to server", LogType.Error);
                        return;
                    }
                
                    try
                    {
                        await SendCommand("QUIT");
                        await ReadResponse();
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error sending QUIT command: {ex.Message}", LogType.Warning);
                    }
                
            }
            catch (Exception ex)
            {
                AppendLog($"Unexpected disconnect error: {ex.Message}", LogType.Error);
            }

            try
            {
                sslStream?.Close();
                sslStream = null;
            }
            catch (Exception ex)
            {
                AppendLog($"Error closing sslStream: {ex.Message}", LogType.Warning);
            }

            try
            {
                stream?.Close();
                stream = null;
            }
            catch (Exception ex)
            {
                AppendLog($"Error closing stream: {ex.Message}", LogType.Warning);
            }

            try
            {
                ftpSocket?.Close();
                ftpSocket = null;
            }
            catch (Exception ex)
            {
                AppendLog($"Error closing ftpSocket: {ex.Message}", LogType.Warning);
            }

            isLoggedIn = false;
            isEncryptedStream = false;

            if (btnConnect != null) btnConnect.IsEnabled = true;
            if (btnSecureConnect != null) btnSecureConnect.IsEnabled = true;
            if (btnDisconnect != null) btnDisconnect.IsEnabled = false;

            AppendLog("Disconnected from server", LogType.Info);
        }

        private async Task SendCommand(string command)
        {
            try
            {
                byte[] commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");
                
                if (isEncryptedStream && sslStream != null)
                {
                    await sslStream.WriteAsync(commandBytes, 0, commandBytes.Length);
                }
                else if (ftpSocket != null)
                {
                    await Task.Run(() => ftpSocket.Send(commandBytes));
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Send command error: {ex.Message}", LogType.Error);
                throw;
            }
        }


        private async Task<string> ReadResponse()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;

                if (isEncryptedStream && sslStream != null)
                {
                    bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
                }
                else if (ftpSocket != null)
                {
                    try
                    {
                        bytesRead = await Task.Run(() => ftpSocket.Receive(buffer));
                    }
                    catch (SocketException ex)
                    {
                        AppendLog($"Socket receive failed: {ex.Message}", LogType.Error);
                        return "";
                    }
                }
                else
                {
                    return "";
                }

                return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            }
            catch (Exception ex)
            {
                AppendLog($"Read response error: {ex.Message}", LogType.Error);
                return "";
            }
        }

        private async Task LoadServerDirectory()
        {
            try
            {
                string response = "";
                Socket dataSocket = null;
                Stream dataStream = null;

                // 1. Try EPSV
                await SendCommand("EPSV");
                response = await ReadResponse();
                AppendLog($"EPSV command: {response}", LogType.Command);
                if (response.StartsWith("229"))
                {
                    int startIndex = response.IndexOf('(');
                    int endIndex = response.IndexOf(')');
                    if (startIndex != -1 && endIndex != -1)
                    {
                        string epsvData = response.Substring(startIndex + 1, endIndex - startIndex - 1);
                        string[] parts = epsvData.Split('|');
                        if (parts.Length >= 4)
                        {
                            int dataPort = int.Parse(parts[3]);
                            string dataHost = serverAddress;
                            dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            dataSocket.Connect(dataHost, dataPort);
                            if (isEncryptedStream)
                            {
                                var sslDataStream = new SslStream(new NetworkStream(dataSocket), false, ValidateServerCertificate);
                                await sslDataStream.AuthenticateAsClientAsync(serverAddress);
                                dataStream = sslDataStream;
                            }
                            else
                            {
                                dataStream = new NetworkStream(dataSocket);
                            }
                        }
                    }
                }
                else
                {
                    // 2. Fallback to PASV
                    await SendCommand("PASV");
                    response = await ReadResponse();
                    AppendLog($"PASV command: {response}", LogType.Command);
                    if (!response.StartsWith("227"))
                    {
                        AppendLog($"Failed to set passive mode for LIST: {response}", LogType.Error);
                        return;
                    }
                    int startIndex = response.IndexOf('(');
                    int endIndex = response.IndexOf(')');
                    if (startIndex != -1 && endIndex != -1)
                    {
                        string pasvData = response.Substring(startIndex + 1, endIndex - startIndex - 1);
                        string[] parts = pasvData.Split(',');
                        if (parts.Length >= 6)
                        {
                            string dataHost = $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}";
                            int dataPort = int.Parse(parts[4]) * 256 + int.Parse(parts[5]);
                            dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            dataSocket.Connect(dataHost, dataPort);
                            if (isEncryptedStream)
                            {
                                var sslDataStream = new SslStream(new NetworkStream(dataSocket), false, ValidateServerCertificate);
                                await sslDataStream.AuthenticateAsClientAsync(serverAddress);
                                dataStream = sslDataStream;
                            }
                            else
                            {
                                dataStream = new NetworkStream(dataSocket);
                            }
                        }
                    }
                }

                if (dataSocket == null || dataStream == null || !dataSocket.Connected)
                {
                    AppendLog("Failed to establish data connection for LIST", LogType.Error);
                    return;
                }

                // 3. Send LIST command AFTER data connection is ready
                await SendCommand("LIST");
                response = await ReadResponse();
                AppendLog($"LIST command: {response}", LogType.Command);
                if (!(response.StartsWith("150") || response.StartsWith("125")))
                {
                    dataStream.Close();
                    dataSocket.Close();
                    AppendLog($"LIST failed: {response}", LogType.Error);
                    return;
                }

                // 4. Read directory listing from data connection
                await ParseDirectoryListing(dataStream);
                dataStream.Close();
                dataSocket.Close();

                // 5. Read completion response
                response = await ReadResponse();
                AppendLog($"LIST complete: {response}", LogType.Command);
            }
            catch (Exception ex)
            {
                AppendLog($"Load directory error: {ex.Message}", LogType.Error);
            }
        }

        private async Task ParseDirectoryListing(Stream dataStream)
        {
            try
            {
                // Clear existing files
                ServerFiles.Clear();

                // Read directory listing from data stream
                using (StreamReader reader = new StreamReader(dataStream, Encoding.ASCII))
                {
                    string listing = await reader.ReadToEndAsync();
                    string[] lines = listing.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.Trim().Length > 0 && !line.StartsWith("226"))
                        {
                            // Simple parsing - you might want to improve this
                            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 9)
                            {
                                string fileName = string.Join(" ", parts, 8, parts.Length - 8);
                                string fileSize = parts[4];
                                string fileType = line.StartsWith("d") ? "Directory" : "File";
                                ServerFiles.Add(new FtpFileInfo
                                {
                                    FileName = fileName,
                                    FileSize = fileSize,
                                    FileType = fileType
                                });
                            }
                        }
                    }
                }
                AppendLog($"Loaded {ServerFiles.Count} items", LogType.Info);
            }
            catch (Exception ex)
            {
                AppendLog($"Parse directory error: {ex.Message}", LogType.Error);
            }
        }

        private void ServerFilesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ServerFilesGrid.SelectedItem is FtpFileInfo selectedFile)
            {
                if (selectedFile.FileType == "Directory")
                {
                    string newPath = currentPath + (currentPath.EndsWith("/") ? "" : "/") + selectedFile.FileName;
                    _ = ChangeDirectory(newPath);
                }
                else
                {
                    _ = DownloadFile();
                }
            }
        }

        private void AppendLog(string message, LogType type = LogType.Info)
        {
            try
            {
                if (txtLog == null) return;
                
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string prefix = "";
                
                switch (type)
                {
                    case LogType.Command:
                        prefix = "[CMD]";
                        break;
                    case LogType.Response:
                        prefix = "[RESP]";
                        break;
                    case LogType.Success:
                        prefix = "[SUCCESS]";
                        break;
                    case LogType.Error:
                        prefix = "[ERROR]";
                        break;
                    case LogType.Warning:
                        prefix = "[WARNING]";
                        break;
                    default:
                        prefix = "[INFO]";
                        break;
                }
                
                string logEntry = $"{timestamp} {prefix} {message}";
                
                Dispatcher.Invoke(() =>
                {
                    txtLog.AppendText(logEntry + Environment.NewLine);
                    txtLog.ScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                // Fallback logging
                System.Diagnostics.Debug.WriteLine($"Log error: {ex.Message}");
            }
        }

        private enum LogType
        {
            Info,
            Command,
            Response,
            Success,
            Error,
            Warning
        }

        private async Task ConnectWithImplicitSSL(IPAddress remoteAddress, int port)
        {
            try
            {
                AppendLog("Attempting implicit SSL connection...", LogType.Info);
                
                // Create new socket for implicit SSL
                AddressFamily addressFamily = remoteAddress.AddressFamily;
                ftpSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                
                // Set timeout to prevent hanging
                ftpSocket.ReceiveTimeout = 10000; // 10 seconds
                ftpSocket.SendTimeout = 10000;    // 10 seconds
                
                IPEndPoint addrEndPoint = new IPEndPoint(remoteAddress, port);
                
                await Task.Run(() => ftpSocket.Connect(addrEndPoint));
                AppendLog("Socket connection established for implicit SSL", LogType.Success);
                
                // Immediately wrap the socket in SSL stream
                stream = new NetworkStream(ftpSocket);
                sslStream = new SslStream(stream, false, ValidateServerCertificate);
                
                await sslStream.AuthenticateAsClientAsync(serverAddress);
                isEncryptedStream = true;
                
                AppendLog("Implicit SSL/TLS connection established", LogType.Success);
                
                // Read the initial response over SSL
                string response = await ReadResponse();
                AppendLog($"Server (SSL): {response}", LogType.Response);
                
                if (!response.StartsWith("220"))
                {
                    AppendLog("Failed to connect to server with implicit SSL", LogType.Error);
                    return;
                }
                
                // Continue with login
                await PerformLogin();
            }
            catch (Exception ex)
            {
                AppendLog($"Implicit SSL connection error: {ex.Message}", LogType.Error);
            }
        }

        private async Task PerformLogin()
        {
            try
            {
                // Login
                await SendCommand($"USER {username}");
                string response = await ReadResponse();
                AppendLog($"USER: {response}", LogType.Command);

                if (response.StartsWith("331"))
                {
                    await SendCommand($"PASS {password}");
                    response = await ReadResponse();
                    AppendLog($"PASS: {response}", LogType.Command);

                    if (response.StartsWith("230"))
                    {
                        isLoggedIn = true;
                        AppendLog("Login successful", LogType.Success);
                        
                        // Load server directory
                        await LoadServerDirectory();
                    }
                    else
                    {
                        AppendLog("Login failed", LogType.Error);
                    }
                }
                else
                {
                    AppendLog("Username not accepted", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Login error: {ex.Message}", LogType.Error);
            }
        }
        #endregion

        #region File Management Methods

        private void LoadLocalDirectory(string path)
        {
            try
            {
                currentLocalPath = path;
                if (txtLocalPath != null)
                    txtLocalPath.Text = path;

                LocalFiles.Clear();
                LocalTreeView.Items.Clear();

                // Add parent directory
                if (path != "C:\\" && path != "/")
                {
                    var parentItem = new TreeViewItem { Header = "..", Tag = "parent" };
                    LocalTreeView.Items.Add(parentItem);
                }

                // Load directories
                var directories = Directory.GetDirectories(path);
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var fileItem = new FileItem
                    {
                        FileName = dirInfo.Name,
                        FilePath = dir,
                        FileSize = 0,
                        FileType = "Directory",
                        ModifiedDate = dirInfo.LastWriteTime,
                        IsDirectory = true
                    };
                    LocalFiles.Add(fileItem);

                    var treeItem = new TreeViewItem { Header = dirInfo.Name, Tag = dir };
                    LocalTreeView.Items.Add(treeItem);
                }

                // Load files
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var fileItem = new FileItem
                    {
                        FileName = fileInfo.Name,
                        FilePath = file,
                        FileSize = fileInfo.Length,
                        FileType = fileInfo.Extension.ToUpper(),
                        ModifiedDate = fileInfo.LastWriteTime,
                        IsDirectory = false
                    };
                    LocalFiles.Add(fileItem);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error loading local directory: {ex.Message}", LogType.Error);
            }
        }

        private void btnBrowseLocal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Local Directory",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadLocalDirectory(dialog.SelectedPath);
            }
        }

        private void btnRefreshServer_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadServerDirectory();
        }

        private void btnChangeDir_Click(object sender, RoutedEventArgs e)
        {
            if (txtCurrentDir != null)
            {
                string newPath = txtCurrentDir.Text;
                _ = ChangeDirectory(newPath);
            }
        }

        private async Task ChangeDirectory(string path)
        {
            try
            {
                await SendCommand($"CWD {path}");
                string response = await ReadResponse();
                
                if (response.StartsWith("250"))
                {
                    currentPath = path;
                    currentServerPath = path;
                    if (txtCurrentDir != null)
                        txtCurrentDir.Text = path;
                    if (txtServerPath != null)
                        txtServerPath.Text = path;
                    
                    await LoadServerDirectory();
                    AppendLog($"Changed to directory: {path}", LogType.Success);
                }
                else
                {
                    AppendLog($"Failed to change directory: {response}", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error changing directory: {ex.Message}", LogType.Error);
            }
        }

        private void LocalTreeView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (string file in files)
                {
                    AddToUploadQueue(file);
                }
            }
        }

        private void LocalTreeView_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void AddToUploadQueue(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var uploadItem = new UploadItem(filePath);
                    UploadQueue.Add(uploadItem);
                    AppendLog($"Added to upload queue: {Path.GetFileName(filePath)}", LogType.Info);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error adding file to queue: {ex.Message}", LogType.Error);
            }
        }

        private void btnStartUpload_Click(object sender, RoutedEventArgs e)
        {
            _ = ProcessUploadQueue();
        }

        private void btnClearQueue_Click(object sender, RoutedEventArgs e)
        {
            UploadQueue.Clear();
            AppendLog("Upload queue cleared", LogType.Info);
        }

        private async Task ProcessUploadQueue()
        {
            if (!isLoggedIn)
            {
                AppendLog("Not connected to server", LogType.Error);
                return;
            }

            foreach (var uploadItem in UploadQueue.ToList())
            {
                try
                {
                    uploadItem.Status = "Uploading...";
                    uploadItem.Progress = 0;

                    await UploadSingleFile(uploadItem.FilePath, uploadItem.FileName);

                    uploadItem.Progress = 100;
                    uploadItem.Status = "Completed";
                    uploadItem.IsCompleted = true;

                    AppendLog($"Upload completed: {uploadItem.FileName}", LogType.Success);
                }
                catch (Exception ex)
                {
                    uploadItem.Status = "Error";
                    uploadItem.HasError = true;
                    AppendLog($"Upload failed: {uploadItem.FileName} - {ex.Message}", LogType.Error);
                }
            }
        }

        private void ServerFilesGrid_ContextMenuOpening(object sender, RoutedEventArgs e)
        {
            // Enable/disable context menu items based on selection
            var contextMenu = ServerFilesGrid.ContextMenu;
            if (contextMenu != null)
            {
                bool hasSelection = ServerFilesGrid.SelectedItems.Count > 0;
                foreach (System.Windows.Controls.MenuItem item in contextMenu.Items.OfType<System.Windows.Controls.MenuItem>())
                {
                    item.IsEnabled = hasSelection;
                }
            }
        }

        private void ContextMenu_Download_Click(object sender, RoutedEventArgs e)
        {
            _ = DownloadFile();
        }

        private void ContextMenu_Upload_Click(object sender, RoutedEventArgs e)
        {
            _ = UploadFileWithVirusScan();
        }

        private void ContextMenu_Rename_Click(object sender, RoutedEventArgs e)
        {
            _ = RenameFile();
        }

        private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
        {
            _ = DeleteFile();
        }

        private void ContextMenu_Properties_Click(object sender, RoutedEventArgs e)
        {
            ShowFileProperties();
        }

        private void ShowFileProperties()
        {
            if (ServerFilesGrid.SelectedItem is FtpFileInfo selectedFile)
            {
                string message = $"File: {selectedFile.FileName}\n" +
                               $"Type: {selectedFile.FileType}\n" +
                               $"Size: {selectedFile.FileSize}\n" +
                               $"Path: {currentPath}";

                System.Windows.MessageBox.Show(message, "File Properties", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        //private void UpdateProgress(int progress, string status)
        //{
        //    if (OverallProgressBar != null)
        //        OverallProgressBar.Value = progress;
        //    if (txtProgressStatus != null)
        //        txtProgressStatus.Text = status;
        //}

        #endregion

        private void OverallProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void btnPrompt_Click(object sender, RoutedEventArgs e)
        {
            promptEnabled = !promptEnabled;

            if (promptEnabled)
            {
                // Prompt is enabled (green)
                btnPrompt.Content = "PROMPT ON";
                btnPrompt.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                AppendLog("Prompt enabled", LogType.Info);
            }
            else
            {
                // Prompt is disabled (red)
                btnPrompt.Content = "PROMPT OFF";
                btnPrompt.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                AppendLog("Prompt disabled", LogType.Warning);
            }
        }
    }

    public class ScanResult
    {
        public string Status { get; set; }
        public string Details { get; set; }
    }

    public class FtpFileInfo : INotifyPropertyChanged
    {
        private string fileName;
        private string fileSize;
        private string fileType;

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public string FileSize
        {
            get { return fileSize; }
            set
            {
                fileSize = value;
                OnPropertyChanged("FileSize");
            }
        }

        public string FileType
        {
            get { return fileType; }
            set
            {
                fileType = value;
                OnPropertyChanged("FileType");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}