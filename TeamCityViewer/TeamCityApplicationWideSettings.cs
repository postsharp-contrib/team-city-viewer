using System;
using System.IO;
using System.Xml.Serialization;

namespace TeamCityViewer
{
    public class TeamCityApplicationWideSettings
    {
        public DateTime DeemphasizeUpTo = DateTime.MinValue;
        
        private static string _filename;
        private static TeamCityApplicationWideSettings _instance;
        public static TeamCityApplicationWideSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        public void Save()
        {
            XmlSerializer ser = new XmlSerializer(typeof(TeamCityApplicationWideSettings));
            TextWriter writer = new StreamWriter(_filename);
            ser.Serialize(writer, this);
            writer.Close();
        }
        private static TeamCityApplicationWideSettings Load()
        {
            XmlSerializer ser = new XmlSerializer(typeof(TeamCityApplicationWideSettings));
            try
            {
                TextReader reader = new StreamReader(_filename);
                TeamCityApplicationWideSettings settings = (TeamCityApplicationWideSettings)ser.Deserialize(reader);
                reader.Close();
                return settings;
            }
            catch (FileNotFoundException)
            {
                var a = new TeamCityApplicationWideSettings();
                return a;
            }
        }

        static TeamCityApplicationWideSettings()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string str = Path.Combine(appdata, "TeamCityViewer");
            Directory.CreateDirectory(str);
            _filename = Path.Combine(str, "TeamCityViewer.xml");
        }
    }
    
    
}