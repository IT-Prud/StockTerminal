using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB.Net
{
    public class TradeMessageTag
    {
        private string pId = "";
        private string pValue = "";

        public TradeMessageTag() { }

        public TradeMessageTag(string Raw)
        {
            Parse(Raw);
        }

        public TradeMessageTag(string Id, string Value)
        {
            this.Id = Id;
            this.Value = Value;
        }

        public string Id
        {
            get
            {
                return pId;
            }

            set
            {
                if (value == null)
                {
                    pId = "";
                }
                else
                {
                    pId = value.Trim();
                }
            }
        }

        public string Value
        {
            get
            {
                return pValue;
            }

            set
            {
                if (value == null)
                {
                    pValue = "";
                }
                else
                {
                    pValue = value.Trim();
                }
            }
        }

        public void Parse(string Raw)
        {
            if (Raw == null || Raw.Length <= 0)
            {
                pId = "";
                pValue = "";
            }
            else
            {
                string[] parts = Raw.Split('=');
                pId = parts[0].Trim();
                if (parts.Length >= 2)
                {
                    pValue = parts[1].Trim();
                }
                else
                {
                    pValue = "";
                }
            }
        }

        public override string ToString()
        {
            if (pId == null || pId.Length <= 0) return "";
            return pId + "=" + pValue;
        }
    }
}
