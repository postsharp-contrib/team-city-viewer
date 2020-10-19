using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using LearningFridays;

namespace TeamCityViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool allReady = false;
        private EnqueueNewBuildForm enqueueNewBuildForm;
        public MainWindow()
        {
            InitializeComponent();
            this.listBox.Items.Clear();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(38);
            timer.Tick += timer_Tick;
            timer.Start();
            RefreshOnlyMe();
            this.enqueueNewBuildForm = new EnqueueNewBuildForm(this);
            this.enqueueNewBuildForm.LoadProjects();
            allReady = true;
        }

        private void RefreshOnlyMe()
        {
            this.chOnlyMe.Content = "Only builds triggered by " + SavedConfig.Instance.Email;
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
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + SavedConfig.Instance.Token;
                string xml;
                try
                {
                    xml = await webClient.DownloadStringTaskAsync(
                        "https://tc.postsharp.net/app/rest/builds?locator=defaultFilter:false&count=1000&fields=build(id,status,queuedDate,statusText,triggered(user),buildType,state,percentageComplete,branchName)");
                }
                catch (Exception ex)
                {
                    this.lblCannotContent.Text = "Cannot connect to TeamCity.\n" + ex.Message;
                    this.lblCannotContent.Visibility = Visibility.Visible;
                    return;
                }

                List<Build> builds = ParseXml(xml);
                IEnumerable<Build> sorted = builds.OrderByDescending(bld => bld.QueuedDate);
                if (this.chOnlyMe.IsChecked.Value)
                {
                    sorted = sorted.Where(bld => bld.TriggeredByEmail == SavedConfig.Instance.Email);
                }

                var buildList = new List<object>();
                Build lastBuild = null;
                foreach (var build in sorted)
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
                this.lblCannotContent.Visibility = Visibility.Collapsed;
            }
            finally
            {
                this.button.IsEnabled = true;
            }
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
        <queuedDate>20191031T12012ChangeEmail_Clicke>
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


        private async void bRerunThisBuild_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBuild != null)
            {
                string content =
                        "<build branchName=\"" + selectedBuild.BranchName + "\"><buildType id=\"" + selectedBuild.BuildTypeId + "\" /><comment><text>This is a re-run triggered by the Team City Viewer.</text></comment></build>";

                var response = await ApiCall.Client().PostAsync("https://tc.postsharp.net/app/rest/buildQueue",
                    new StringContent(
                        content, Encoding.UTF8, "application/xml"
                    ));

                await RefreshBuilds();
          
            }
        }

        private async void bDownloadOpenLog_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow downloadWindow = new DownloadWindow(selectedBuild.Id, selectedBuild.BuildTypeName, selectedBuild.BranchName);
            downloadWindow.Show();
        }

        private async void ChangeEmail_Click(object sender, RoutedEventArgs e)
        {
            string email = Microsoft.VisualBasic.Interaction.InputBox("Enter your TeamCity e-mail login.", "Update user (part 1/2)", SavedConfig.Instance.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }
            string token = Microsoft.VisualBasic.Interaction.InputBox("Enter your TeamCity token (Create it with My Settings & Tools -> Access Tokens -> Create access token).", "Update user (part 2/2)", SavedConfig.Instance.Token);
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }
            SavedConfig.Instance.Token = token;
            SavedConfig.Instance.Email = email;
            SavedConfig.Instance.Save();
            RefreshOnlyMe();
            await RefreshBuilds();
        }

        private void CbProjects_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allReady && this.cbProjects.SelectedItem != null)
            {
                enqueueNewBuildForm.ProjectSelectionChanged();
            }
        }

        private async void bRefreshBranches_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.bRefreshBranches.IsEnabled = false;
                await enqueueNewBuildForm.RefreshBranchesClicked();
            }
            finally
            {
                this.bRefreshBranches.IsEnabled = true;
            }
        }

        private async void bEnqueueNewBuild_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.bEnqueueNewBuild.IsEnabled = false;
                await enqueueNewBuildForm.EnqueueNewBuildClicked();
                await RefreshBuilds();
            }
            finally
            {
                this.bEnqueueNewBuild.IsEnabled = true;
            }
        }

        private void CbConfiguration_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allReady && this.cbConfigurations.SelectedItem != null)
            {
                this.enqueueNewBuildForm.ConfigurationSelectionChanged();
            }
        }
    }
}
