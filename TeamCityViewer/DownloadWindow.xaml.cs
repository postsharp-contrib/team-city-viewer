using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace TeamCityViewer
{
    public partial class DownloadWindow : Window
    {
        private string tempDirectory;
        private string buildLogFileName;
        private string zipFileName;
        private string zipFolder;
        private string finalTitle;
        private bool buildLogDownloaded;
        private bool logsDownloaded;
        public int SelectedBuildId { get; }

        public DownloadWindow(int selectedBuildId, string buildTypeName, string branchName)
        {
            SelectedBuildId = selectedBuildId;
            InitializeComponent();

            this.Title = "Downloading window: " + buildTypeName + " " + branchName;
            this.finalTitle = "Done: " + buildTypeName + " " + branchName;

            tempDirectory = Path.Combine(Path.GetTempPath(), "TeamCityViewer");
            Directory.CreateDirectory(tempDirectory);
            buildLogFileName = Path.Combine(tempDirectory, "BuildLog" + selectedBuildId + ".log");
            zipFileName = Path.Combine(tempDirectory, "logs" + selectedBuildId + ".zip");
            zipFolder = Path.Combine(tempDirectory, "logs" + selectedBuildId);
            HttpClientDownloadWithProgress httpClient = new HttpClientDownloadWithProgress($"https://{SavedConfig.HostName}/downloadBuildLog.html?buildId=" + selectedBuildId, buildLogFileName);
            httpClient.ProgressChanged += (size, downloaded, percentage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    this.tbLog.Text = "Downloading build log: " + FormatProgress(percentage, downloaded, size);
                });
            };
            httpClient.StartDownload().ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    buildLogDownloaded = true;
                    if (t.IsFaulted)
                    {
                        this.tbLog.Text = t.Exception.Message;
                    }
                    else
                    {
                        this.tbLog.Text = "Build log downloaded.";
                        this.bOpenLogFile.IsEnabled = true;
                    }
                    ConsiderChangingTitle();
                });
            });
            HttpClientDownloadWithProgress httpClient2 = new HttpClientDownloadWithProgress($"https://{SavedConfig.HostName}/app/rest/builds/id:" + selectedBuildId + "/artifacts/content/logs.zip", zipFileName);
            httpClient2.ProgressChanged += (size, downloaded, percentage) =>
            {
                this.tbZip.Text = "Downloading logs.zip: " + FormatProgress(percentage, downloaded, size);
            };
            httpClient2.StartDownload().ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (t.IsFaulted)
                    {
                        this.tbZip.Text = t.Exception.InnerExceptions[0].Message;
                    }
                    else
                    {
                        this.tbZip.Text = "Extracting logs.zip...";
                        Task.Run(() =>
                        {
                            if (Directory.Exists(zipFolder))
                            {
                                Directory.Delete(zipFolder, true);
                            }
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipFileName, zipFolder);
                            Dispatcher.Invoke(() =>
                            {
                                this.tbZip.Text = "Logs downloaded and extracted.";
                                this.bOpenInCode.IsEnabled = true;
                            });
                        }).ContinueWith(tt =>
                        {
                            if (tt.IsFaulted)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    this.tbZip.Text = tt.Exception.InnerExceptions[0].Message;
                                });
                            }

                            Dispatcher.Invoke(() =>
                            {
                                this.logsDownloaded = true;
                                this.ConsiderChangingTitle();
                            });
                        });
                    }
                });
            });
        }

        private void ConsiderChangingTitle()
        {
            if (buildLogDownloaded && logsDownloaded)
            {
                this.Title = finalTitle;
            }
        }

        private string FormatProgress(double? percentage, long totalBytesDownloaded, long? totalFileSize)
        {
            double mb = Math.Round((double)totalBytesDownloaded / 1000000, 1);
            if (totalFileSize.HasValue)
            {
                double mb2 = Math.Round((double)totalFileSize / 1000000, 1);
                return percentage + "% (" + mb + " / " + mb2 + " MB)";
            }
            else
            {
                return mb + " MB";
            }
        }


        private void BOpenLogFile_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(buildLogFileName);
        }    
        
        private void BOpenZip_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("code", zipFolder);
        }


        private void BOpenLogFolder_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(tempDirectory);
        }
    }





    public class HttpClientDownloadWithProgress : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly string _destinationFilePath;

        private HttpClient _httpClient;

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
        {
            _downloadUrl = downloadUrl;
            _destinationFilePath = destinationFilePath;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient {Timeout = TimeSpan.FromDays(1)};
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                SavedConfig.Instance.Token);
            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 100 == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                } while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double) totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}