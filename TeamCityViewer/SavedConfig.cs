using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace LearningFridays
{
    public class SavedConfig
    {
        public string Token = "eyJ0eXAiOiAiVENWMiJ9.aVdEZnJEaFlwcW8tcEl4US1sRUpvR3ZpTENn.Y2UwYWVjYjUtZThmZi00NzFjLTk0MDUtM2ExNDAzNDFiYzM4";
        public string Email = "petr@postsharp.net";
        public string LastSelectedProjectId = "PostSharp67";
        public string LastSelectedBuildType = "???";
        
        private bool _loaded = false;
        
        
        
        private static SavedConfig _instance;
        private static string _filename;
        public bool IsLoaded()
        {
            return _loaded;
        }
        public static SavedConfig Instance
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
            XmlSerializer ser = new XmlSerializer(typeof(SavedConfig));
            TextWriter writer = new StreamWriter(_filename);
            ser.Serialize(writer, this);
            writer.Close();
        }
        private static SavedConfig Load()
        {
            XmlSerializer ser = new XmlSerializer(typeof(SavedConfig));
            try
            {
                TextReader reader = new StreamReader(_filename);
                SavedConfig settings = (SavedConfig)ser.Deserialize(reader);
                reader.Close();
                settings._loaded = true;
                return settings;
            }
            catch (FileNotFoundException)
            {
                var a = new SavedConfig();
                a._loaded = true;
                return a;
            }
        }

        static SavedConfig()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string str = Path.Combine(appdata, "TeamCityViewer");
            Directory.CreateDirectory(str);
            _filename = Path.Combine(str, "SavedConfig.xml");
        }
    }
}