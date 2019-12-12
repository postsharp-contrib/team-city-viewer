using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace TeamCityViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool allReady = false;
        public MainWindow()
        {
            InitializeComponent();
            this.listBox.Items.Clear();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(38);
            timer.Tick += timer_Tick;
            timer.Start();
            allReady = true;
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            if (this.chAutorefresh.IsChecked.Value)
            {
                await RefreshBuilds();
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            await RefreshBuilds();
        }

        private async Task RefreshBuilds()
        {
            this.button.IsEnabled = false;
            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.Authorization] = "Bearer eyJ0eXAiOiAiVENWMiJ9.aVdEZnJEaFlwcW8tcEl4US1sRUpvR3ZpTENn.Y2UwYWVjYjUtZThmZi00NzFjLTk0MDUtM2ExNDAzNDFiYzM4";
            string xml = await webClient.DownloadStringTaskAsync("https://tc.postsharp.net/app/rest/builds?locator=defaultFilter:false&count=1000&fields=build(id,status,queuedDate,statusText,triggered(user),buildType,state,percentageComplete,branchName)");
            List<Build> builds = ParseXml(xml);
            IEnumerable<Build> sorted = builds.OrderByDescending(bld => bld.QueuedDate);
            if (this.checkBox.IsChecked.Value)
            {
                sorted = sorted.Where(bld => bld.TriggeredByEmail == "petr@postsharp.net");
            }
            var buildList = new List<object>();
            Build lastBuild = null;
            foreach(var build in sorted)
            {
                if (lastBuild != null)
                {
                    if (lastBuild.BranchName != build.BranchName ||
                        lastBuild.QueuedDate != build.QueuedDate)
                    {
                        buildList.Add(new BuildSeparator());
                    }
                }
                lastBuild = build;
                buildList.Add(build);
            }
            this.listBox.ItemsSource = buildList;
            RefreshOpacities();
            this.button.IsEnabled = true;
        }

        private List<Build> ParseXml(string xml)
        {
            XDocument xDoc = XDocument.Parse(xml);
            List<Build> builds = new List<Build>();
            foreach(var build in xDoc.Root.Elements("build"))
            {
                builds.Add(ParseBuild(build));
            }
            return builds;
            /*
   <builds>
    <build id="12633" status="SUCCESS" state="running" percentageComplete="2" branchName="topic/6.4/bug-15285-25432-rewrite-initialized">
        <statusText>Resolving artifact dependencies</statusText>
        <buildType id="PostSharp64_TestNetCore30vs2019onWindows" name="Test .NET Core 3.0 (VS2019) on Windows" projectName="PostSharp 6.4 (preview)" projectId="PostSharp64" href="/app/rest/buildTypes/id:PostSharp64_TestNetCore30vs2019onWindows" webUrl="https://tc.postsharp.net/viewType.html?buildTypeId=PostSharp64_TestNetCore30vs2019onWindows"/>
        <queuedDate>20191031T120128+0000</queuedDate>
        <triggered>
            <user username="petr@postsharp.net" name="Petr Hudecek" id="8" href="/app/rest/users/id:8"/>
        </triggered>
    </build>*/
        }

        private Build ParseBuild(XElement build)
        {
            return new Build()
            {
                Id = int.Parse(build.Attribute("id").Value),
                Status = build.Attribute("status")?.Value,
                State = build.Attribute("state")?.Value,
                PercentageComplete = int.Parse(build.Attribute("percentageComplete")?.Value ?? "0"),
                BranchName = build.Attribute("branchName")?.Value,
                StatusText = build.Element("statusText")?.Value,
                BuildTypeId = build.Element("buildType")?.Attribute("id")?.Value,
                BuildTypeName = build.Element("buildType")?.Attribute("name")?.Value,
                QueuedDate = ParseQueuedDate(build.Element("queuedDate")?.Value),
                TriggeredByName = build.Element("triggered")?.Element("user")?.Attribute("name")?.Value,
                TriggeredByEmail = build.Element("triggered")?.Element("user")?.Attribute("username")?.Value
            };
        }

        private DateTime ParseQueuedDate(string value)
        {
            string format = "yyyyMMdd'T'HHmmss'+0000'";
            string dtNow = value;
            DateTime time;
            if (DateTime.TryParseExact(dtNow, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out time))
            {
                return time;
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        private async void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            if (allReady)
            {
                await RefreshBuilds();
            }
        }

        private void chAutorefresh_Checked(object sender, RoutedEventArgs e)
        {

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshBuilds();
        }

        Build selectedBuild = null;

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.listBox.SelectedItem is Build build) {
                this.lblSelectedBuild.Text = "Selected: " + Environment.NewLine + build.BranchName + " (" + build.BuildTypeName + ")";
                selectedBuild = build;
            }
        }

        private void bDownloadLog_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                System.Diagnostics.Process.Start("https://tc.postsharp.net/downloadBuildLog.html?buildId=" + selectedBuild.Id);
            }

        }

        private void bGoToWebsite_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                System.Diagnostics.Process.Start("https://tc.postsharp.net/viewLog.html?buildId=" + selectedBuild.Id);
            }
        }

        private void bDeemphasize_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                TeamCityApplicationWideSettings.Instance.DeemphasizeUpTo = selectedBuild.QueuedDate;
                TeamCityApplicationWideSettings.Instance.Save();
            }
            RefreshOpacities();
        }

        private void bReemphasize_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                TeamCityApplicationWideSettings.Instance.DeemphasizeUpTo = selectedBuild.QueuedDate.AddMilliseconds(-1);
                TeamCityApplicationWideSettings.Instance.Save();
            }
            RefreshOpacities();
        }

        private void RefreshOpacities()
        {
            foreach (var item in this.listBox.ItemsSource)
            {
                if (item is Build build)
                {
                    build.RefreshOpacity(TeamCityApplicationWideSettings.Instance.DeemphasizeUpTo);
                }
            }
        }

        private void bOpenLogInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                System.Diagnostics.Process.Start("https://tc.postsharp.net/viewLog.html?buildId=" + selectedBuild.Id + "&tab=buildLog");
            }
        }

        HttpClient httpClient = new HttpClient();

        private async void bRerunThisBuild_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                string content =
                        "<build branchName=\"" + selectedBuild.BranchName + "\"><buildType id=\"" + selectedBuild.BuildTypeId + "\" /><comment><text>Triggered from Petr's Team City Viewer, I hope this works.</text></comment></build>";

                if (MessageBox.Show(content, "Run this build?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                        "eyJ0eXAiOiAiVENWMiJ9.aVdEZnJEaFlwcW8tcEl4US1sRUpvR3ZpTENn.Y2UwYWVjYjUtZThmZi00NzFjLTk0MDUtM2ExNDAzNDFiYzM4");
                   var response = await httpClient.PostAsync("https://tc.postsharp.net/app/rest/buildQueue", new StringContent(
                        content, Encoding.UTF8, "application/xml"
                        ));

                    await RefreshBuilds();
                }
            }
        }
    }

    internal class BuildSeparator
    {
        public BuildSeparator()
        {
        }
    }

    public class Build : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public int PercentageComplete { get; set; }
        public string BranchName { get; set; }
        public string StatusText { get; set; }
        public string BuildTypeName { get; set; }
        public DateTime QueuedDate { get; set; }
        public string DisplayedQueuedDate => QueuedDate.ToString("d. M. yyyy HH:mm:ss");
        public string TriggeredByName { get; set; }
        public string TriggeredByEmail { get; set; }

        public bool IsFinished => State == "finished";
        public bool IsQueued => State == "queued";
        public bool IsRunning => State == "running";

        public bool IsSuccess => Status == "SUCCESS";
        public bool IsFailure => Status == "FAILURE";
        public bool IsUnknown => Status == "UNKNOWN";

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshOpacity(DateTime deempUpTo)
        {
            if (this.QueuedDate <= deempUpTo)
            {
                BuildOpacity = 0.3f;
            }
            else
            {
                BuildOpacity = 1;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BuildOpacity)));
        }

        public float BuildOpacity { get; private set;  }

        public Visibility ProgressBarVisibility
        {
            get
            {
                if (IsQueued)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public Brush DisplayedBackground
        {
            get
            {
                if (IsFinished)
                {
                    if (IsSuccess)
                    {
                        return Brushes.LightGreen;
                    }
                    else if (IsFailure)
                    {
                        return Brushes.Pink;
                    }
                    else if (IsUnknown)
                    {
                        return Brushes.LightGray;
                    }
                }
                else if (IsQueued)
                {
                    return Brushes.AntiqueWhite;
                }
                else if (IsRunning)
                {
                    return Brushes.LightYellow;
                }

                return Brushes.Brown;
            }
        }

        public Brush DisplayedForeground
        {
            get
            {
                if (IsFinished)
                {
                    if (IsSuccess)
                    {
                        return Brushes.LawnGreen;
                    }
                    else if (IsFailure)
                    {
                        return Brushes.LightPink;
                    }
                    else if (IsUnknown)
                    {
                        return Brushes.Silver;
                    }
                }
                else if (IsQueued)
                {
                    return Brushes.Transparent;
                }
                else if (IsRunning)
                {
                    return Brushes.Orange;
                }
                return Brushes.Brown;
            }
        }

        public int DisplayedPercentage
        {
            get
            {
                if (IsFinished)
                {
                    return 100;
                }
                else if (IsQueued)
                {
                    return 0;
                }
                else if (IsRunning)
                {
                    return PercentageComplete;
                }
                return PercentageComplete;
            }
        }

        public string BuildTypeId { get; internal set; }
    }
}
