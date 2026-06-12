using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Processors;
using Utils;

namespace Logger
{
    public class Log : IDisposable
    {
        public string Name = null;      // for external naming purpose, does not have any effect inside the class


        public LogProcessor TheLogProcessor = null;

        private string pFolderPath = null;
        private string pFileNamePrefix = null;
        private string pFileNameSuffix = ".log";

        public bool FlushAfterAppend = false;
        public bool CloseAfterAppend = false;

        private string pCurrentFilePath = null;
        private StreamWriter sw = null;

        private readonly QueueLockFree<LogEntry> LogQueue = new QueueLockFree<LogEntry>();


        #region Public Memebers

        public Log()
        {
        }

        ~Log()
        {
            /* the following might not sucess in fflushing because file StreamWrite 
             * might have already been closed by the system if destruction is due to app closing.
             * So the application should explicit call Close.
             */
            Close();
        }

        public string FolderPath
        {
            get { return pFolderPath; }
            set { pFolderPath = value != null ? value.Trim() : null; }
        }

        public string FileNamePrefix
        {
            get { return pFileNamePrefix; }
            set { pFileNamePrefix = value != null ? value.TrimStart() : null; }
        }

        public string FileNameSuffix
        {
            get { return pFileNameSuffix; }
            set { pFileNameSuffix = value != null ? value.TrimEnd() : null; }
        }

        public string FileName
        {
            get { return Path.GetFileName(pCurrentFilePath); }
        }

        public string FilePath
        {
            get { return pCurrentFilePath; }
        }

        public void Append(string MessageType, string Message)
        {
            LogQueue.Enqueue(new LogEntry { ActType = LogEntry.ActionType.Log, LogTime = HighResolutionClock.Now, LogType = MessageType, LogMessage = Message });
            if (TheLogProcessor != null) TheLogProcessor.Set(this);
        }

        public void Append(string MessageType, byte[] MessageBytes)
        {
            LogQueue.Enqueue(new LogEntry { ActType = LogEntry.ActionType.Log, LogTime = HighResolutionClock.Now, LogType = MessageType, LogMessage = (MessageBytes != null ? Encoding.ASCII.GetString(MessageBytes) : "") });
            if (TheLogProcessor != null) TheLogProcessor.Set(this);
        }

        public void Purge(int TheRetainCount)
        {
            LogQueue.Enqueue(new LogEntry { ActType = LogEntry.ActionType.Purge, RetainCount = TheRetainCount });
            if (TheLogProcessor != null) TheLogProcessor.Set(this);
        }

        public void Flush()
        {
            LogQueue.Enqueue(new LogEntry { ActType = LogEntry.ActionType.Flush });
            if (TheLogProcessor != null) TheLogProcessor.Set(this);
        }

        public void Close()
        {
            LogQueue.Enqueue(new LogEntry { ActType = LogEntry.ActionType.Close });
            if (TheLogProcessor != null) TheLogProcessor.Set(this);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
        }

        #endregion


        #region Internal Memeber Functions

        internal void Do()
        {
            LogEntry log = null;

            while ((log = LogQueue.Dequeue()) != null)
            {
                switch (log.ActType)
                {
                    case LogEntry.ActionType.Log:
                        DoAppend(log);
                        break;

                    case LogEntry.ActionType.Purge:
                        DoPurge(log.RetainCount);
                        break;

                    case LogEntry.ActionType.Flush:
                        DoFlush();
                        break;

                    case LogEntry.ActionType.Close:
                        DoClose();
                        break;
                }
            }
        }

        #endregion


        #region Private Member Functions

        private string GetFileName(string FileNameDate)
        {
            return FileNameDate != null ? pFileNamePrefix + FileNameDate + pFileNameSuffix : null;
        }

        private string GetFilePath(string FileNameDate)
        {
            string fileName = GetFileName(FileNameDate);

            if (fileName != null)
            {
                if (pFolderPath != null)
                {
                    pFolderPath = pFolderPath.Replace("?", "");
                    return Path.Combine(pFolderPath, fileName);
                }
                else
                {
                    return fileName;
                }
            }

            return null;
        }


        private bool DoAppend(LogEntry TheLog)
        {
            string timeStr = TheLog.LogTime.ToString("HH:mm:ss.fffffff");
            string line = timeStr + "," + (TheLog.LogType ?? "") + "," + (TheLog.LogMessage ?? "");

            string fDate = TheLog.LogTime.ToString("yyyyMMdd");
            string newFilePath = GetFilePath(fDate);

            if (sw == null || pCurrentFilePath != newFilePath)
            {
                if (sw != null)
                {
                    try
                    {
                        sw.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.Print(timeStr + ": !!! DoAppend close failed: " + e.Message);
                    }
                }
                pCurrentFilePath = newFilePath;

                try
                {
                    if (Directory.Exists(pFolderPath) == false)
                        Directory.CreateDirectory(pFolderPath);

                    //sw = File.AppendText(newFilePath);
                    sw = new StreamWriter(
                        new FileStream(newFilePath, FileMode.Append, FileAccess.Write, FileShare.Read));
                }
                catch (Exception e)
                {
                    Debug.Print(timeStr + ": !!! DoAppend create directory failed: " + e.Message);
                    return false;
                }
            }

            try
            {
                if (line != null) sw.WriteLine(line);
            }
            catch (Exception e)
            {
                Debug.Print(timeStr + ": !!! DoAppend write line failed: " + e.Message);
                return false;
            }

            if (CloseAfterAppend) { DoClose(); }
            else if (FlushAfterAppend) { DoFlush(); }

            return true;
        }

        private bool DoFlush()
        {
            if (sw != null)
            {
                try
                {
                    sw.Flush();
                }
                catch { return false; }
            }

            return true;
        }

        private bool DoClose()
        {
            if (sw != null)
            {
                try
                {
                    sw.Flush();
                }
                catch { }

                try
                {
                    sw.Close();
                }
                catch { return false; }
                sw.Dispose();
                sw = null;
            }

            return true;
        }

        private bool DoPurge(int RetainCount)
        {
            if (RetainCount < 0) { RetainCount = 0; }
            else if (RetainCount > 9999) { RetainCount = 9999; }

            DateTime logDate = DateTime.Today - TimeSpan.FromDays(RetainCount);
            TimeSpan tsOneDay = TimeSpan.FromDays(1);

            try
            {
                for (int i = 0; i < 30; i++)
                {
                    string filePath = Path.Combine(pFolderPath, pFileNamePrefix + logDate.ToString("yyyyMMdd") + pFileNameSuffix);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    logDate -= tsOneDay;
                }
            }
            catch (Exception e)
            {
                Debug.Print(DateTime.Now.ToString() + ": !!! Purge: " + this.GetType().ToString() + ": " + e.Message);
                return false;
            }

            return true;
        }

        #endregion
    }
}
