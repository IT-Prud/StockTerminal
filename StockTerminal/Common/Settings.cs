using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Utils
{
    public class Settings
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private Dictionary<string, string> SettingDict = new Dictionary<string, string>(10);
        private static readonly char[] ItemPartSep = new char[] { '=' };

        public Settings()
        {
        }

        public Settings(params string[] Keys)
        {
            int i, n = Keys.Length;
            for (i = 0; i < n; i++)
            {
                SettingDict[Keys[i]] = "";
            }
        }

        public string this[string Key]
        {
            get
            {
                string Value;
                SettingDict.TryGetValue(Key, out Value);
                return Value;
            }

            set
            {
                SettingDict[Key] = value;
            }
        }

        public string PersistString
        {
            get
            {
                StringBuilder sb = new StringBuilder(1000);
                string sep = "";

                foreach (KeyValuePair<string, string> kvp in SettingDict)
                {
                    sb.Append(sep);
                    sb.Append(System.Web.HttpUtility.UrlEncode(kvp.Key));
                    sb.Append("=");
                    sb.Append(System.Web.HttpUtility.UrlEncode(kvp.Value));
                    sep = "&";
                }

                return sb.ToString();
            }

            set
            {
                SettingDict.Clear();

                if (value != null)
                {
                    string[] items = value.Split('&');
                    string[] itemParts = null;

                    foreach (string item in items)
                    {
                        itemParts = item.Split(ItemPartSep, 2);
                        SettingDict[itemParts[0]] = itemParts.Length >= 2 ? itemParts[1] : "";
                    }
                }
            }
        }

        /*
        public override string ToString()
        {
            return PersistString;
        }
         */

        public void ReadFromFile(string FilePath, string Section)
        {
            StringBuilder retVal = new StringBuilder(1000);

            string[] Keys = new string[SettingDict.Keys.Count];
            SettingDict.Keys.CopyTo(Keys, 0);

            foreach(string key in Keys)
            {
                try
                {
                    GetPrivateProfileString(Section, key, "", retVal, 1000, FilePath);
                    SettingDict[key] = retVal.ToString().Trim();
                }
                catch { }
            }
        }

        public void SaveToFile(string FilePath, string Section)
        {
            string folderPath = null;

            foreach (KeyValuePair<string, string> kvp in SettingDict)
            {
                try
                {
                    folderPath = System.IO.Path.GetDirectoryName(FilePath);

                    if (!System.IO.Directory.Exists(folderPath))
                        System.IO.Directory.CreateDirectory(folderPath);
                    WritePrivateProfileString(Section, kvp.Key, kvp.Value != null ? kvp.Value.Trim() : "", FilePath);
                }
                catch { }
            }
        }

        public void SaveToFile(string FilePath, string Section, string Key)
        {
            string folderPath = null, value = null;

            if (SettingDict.TryGetValue(Key, out value))
            {
                try
                {
                    folderPath = System.IO.Path.GetDirectoryName(FilePath);

                    if (!System.IO.Directory.Exists(folderPath))
                        System.IO.Directory.CreateDirectory(folderPath);
                    WritePrivateProfileString(Section, Key, value != null ? value.Trim() : "", FilePath);
                }
                catch { }
            }
        }

        public void Clear()
        {
            SettingDict.Clear();
        }
    }
}
