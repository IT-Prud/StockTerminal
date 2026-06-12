using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class SettingKeyValue
    {
        public string Key = null;
        public string Value = null;

        public SettingKeyValue()
        {
        }

        public SettingKeyValue(string Key, string Value)
        {
            this.Key = Key;
            this.Value = Value;
        }
    }
}
