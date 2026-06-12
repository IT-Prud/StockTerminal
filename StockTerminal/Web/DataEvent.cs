using System;
using System.Collections.Generic;
using System.Text;

namespace StockTerminal.Web
{
    public class DataEvent
    {
        public string Type = null;
        public string Message = null;

        public DataEvent()
        {
        }

        public DataEvent(string Type, string Message)
        {
            this.Type = Type;
            this.Message = Message;
        }

        public override string ToString()
        {
            return DateTime.Now.ToString("dd HH:mm:ss") + " - " + (Type != null ? Type : "") + ": " + (Message != null ? Message : "");
        }
    }
}
