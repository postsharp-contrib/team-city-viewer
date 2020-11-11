using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace TeamCityViewer
{
    public class EnqueueNewBuildForm
    {
        private readonly MainWindow mainWindow;

        public EnqueueNewBuildForm(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public async void LoadProjects()
        {
            var response = await ApiCall.Client().GetAsync($"https://{SavedConfig.HostName}/app/rest/projects/?fields=project(id,name,buildTypes(buildType(id,name)))");
            var responseAsText = await response.Content.ReadAsStringAsync();
            XDocument xDoc = XDocument.Parse(responseAsText);
            this.mainWindow.cbProjects.Items.Clear();
            TCProject toSelect = null;
            foreach (XElement xProject in xDoc.Root.Elements("project"))
            {
                string id = xProject.Attribute("id").Value;
                string name = xProject.Attribute("name").Value;
                TCProject tcProject = new TCProject {Id = id, Name = name};
                foreach (XElement xBuildType in xProject.Descendants("buildType"))
                {
                    string btId = xBuildType.Attribute("id").Value;
                    string btName = xBuildType.Attribute("name").Value;
                    tcProject.Configurations.Add(new TCConfiguration { Id = btId, Name = btName });
                }

                if (tcProject.Id == SavedConfig.Instance.LastSelectedProjectId)
                {
                    toSelect = tcProject;
                }
                this.mainWindow.cbProjects.Items.Add(tcProject);
                // Intentional no await:
                LoadBranchesForProject(tcProject);
            }

            if (toSelect == null)
            {
                toSelect = (TCProject) this.mainWindow.cbProjects.Items[this.mainWindow.cbProjects.Items.Count - 1];
            }

            this.mainWindow.cbProjects.SelectedItem = toSelect;
            RefreshConfigurationsAndBranches();
        }

        private async Task LoadBranchesForProject(TCProject tcProject)
        {
            var response = await ApiCall.Client().GetAsync($"https://{SavedConfig.HostName}/app/rest/projects/id:{tcProject.Id}/branches");
            var responseAsText = await response.Content.ReadAsStringAsync();
            XDocument xDoc = XDocument.Parse(responseAsText);
            tcProject.Branches.Clear();
            foreach (XElement xBranch in xDoc.Root.Elements("branch"))
            {
                string name = xBranch.Attribute("name").Value;
                bool isDefault = xBranch.Attribute("default")?.Value == "true";
                tcProject.Branches.Add(name);
                if (isDefault)
                {
                    tcProject.DefaultBranch = name;
                }
            }

            if (mainWindow.cbProjects.SelectedItem == tcProject)
            {
                ProjectSelectionChanged();
            }
        }

        class TCProject
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public List<TCConfiguration> Configurations = new List<TCConfiguration>();
            public string DefaultBranch { get; set; }
            public List<string> Branches = new List<string>();

            public override string ToString()
            {
                return Name;
            }
        }

        class TCConfiguration
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }

        public void ProjectSelectionChanged()
        {   
            TCProject selectedProject = mainWindow.cbProjects.SelectedItem as TCProject;
            SavedConfig.Instance.LastSelectedProjectId = selectedProject.Id;
            SavedConfig.Instance.Save();
            RefreshConfigurationsAndBranches();
        }

        private void RefreshConfigurationsAndBranches()
        {
            TCProject selectedProject = mainWindow.cbProjects.SelectedItem as TCProject;
            mainWindow.cbConfigurations.Items.Clear();
            foreach (var configuration in selectedProject.Configurations)
            {
                mainWindow.cbConfigurations.Items.Add(configuration);
                if (configuration.Name == SavedConfig.Instance.LastSelectedBuildType)
                {
                    mainWindow.cbConfigurations.SelectedItem = configuration;
                }
            }
            mainWindow.cbBranches.Items.Clear();
            foreach (var branch in selectedProject.Branches)
            {
                mainWindow.cbBranches.Items.Add(branch);
            }
            mainWindow.cbBranches.Text = selectedProject.DefaultBranch;
        }

        public async Task RefreshBranchesClicked()
        {
            TCProject selectedProject = mainWindow.cbProjects.SelectedItem as TCProject;
            await LoadBranchesForProject(selectedProject);
            ProjectSelectionChanged();
        }

        public async Task EnqueueNewBuildClicked()
        {
            string content =
                $"<build branchName=\"{mainWindow.cbBranches.Text}\"><buildType id=\"{(mainWindow.cbConfigurations.SelectedItem as TCConfiguration).Id}\" /><comment><text>This is a new build triggered with the new form by the Team City Viewer.</text></comment></build>";

            var response = await ApiCall.Client().PostAsync($"https://{SavedConfig.HostName}/app/rest/buildQueue",
                new StringContent(
                    content, Encoding.UTF8, "application/xml"
                ));

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), "Build enqueue failed.");
            }
        }

        public void ConfigurationSelectionChanged()
        {
            SavedConfig.Instance.LastSelectedBuildType = ((TCConfiguration) this.mainWindow.cbConfigurations.SelectedItem).Name;
            SavedConfig.Instance.Save();
        }
    }
}