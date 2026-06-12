using System;
using System.Collections.Generic;
using System.Text;

namespace Logger
{
    public class LogEntry
    {
        public enum ActionType { Log, Purge, Flush, Close }

        public ActionType ActType;
        public DateTime LogTime;
        public string LogType;
        public string LogMessage;
        public int RetainCount;
    }

}
