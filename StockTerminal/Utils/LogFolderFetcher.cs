using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TradeDB.Net;

namespace StockTerminal.Utils
{
    public class LogFolderFetcher
    {
        private string pBasePath = null;
        private string pFileNamePrefix = null;
        private string pFileNameSuffix = null;
        private Stack<string> dirStack = new Stack<string>(10);

        public LogFolderFetcher(string BasePath, string FileNamePrefix, string FileNameSuffix)
        {
            pBasePath = BasePath;
            pFileNamePrefix = FileNamePrefix;
            pFileNameSuffix = FileNameSuffix;
        }

        public List<TradeMessage> GetNext()
        {
            if (pBasePath == null) return null;

            List<TradeMessage> msgList = null;

            //string[] directories = Directory.GetDirectories(pBasePath + "ABC");


            return msgList;
        }
    }
}
