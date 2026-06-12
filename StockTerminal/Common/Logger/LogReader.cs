using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Logger
{
    public class LogReader : IDisposable
    {
        private string pFolderPath = null;
        private string pFileNamePrefix = null;
        private string pFileNameSuffix = ".log";

        private string pCurrentFilePath = null;
        private StreamReader sr = null;

        private static readonly char[] LogLineSepCharArray = { ',' };


        #region Public Memebers

        public LogReader()
        {
        }

        ~LogReader()
        {
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

        public bool Read(out DateTime MessageTime, out string MessageType, out string Message)
        {
            MessageTime = DateTime.MinValue;
            MessageType = null;
            Message = null;

            string line = null;

            string fDate = DateTime.Today.ToString("yyyyMMdd");
            string newFilePath = GetFilePath(fDate);

            if (sr == null || pCurrentFilePath != newFilePath)
            {
                if (sr != null)
                {
                    try
                    {
                        sr.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.Print("!!! DoAppend close failed: " + e.Message);
                    }
                }
                pCurrentFilePath = newFilePath;

                try
                {
                    if (Directory.Exists(pFolderPath) == false)
                        Directory.CreateDirectory(pFolderPath);

                    sr = new StreamReader(
                        new FileStream(newFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                }
                catch (Exception e)
                {
                    Debug.Print("!!! Read failed: " + e.Message);
                    return false;
                }
            }

            try
            {
                line = sr.ReadLine();
            }
            catch (Exception e)
            {
                Debug.Print("!!! Read line failed: " + e.Message);
                return false;
            }

            string[] items = line.Split(LogLineSepCharArray, 3);
            if (items.Length >= 3 &&
                DateTime.TryParse(items[0], out MessageTime))
            {
                MessageType = items[1];
                Message = items[2];
                return true;
            }

            return false;
        }

        public Queue<LogEntry> ReadLast(int PositionFromEnd, string LogType)
        {
            Queue<LogEntry> logs = null;
            LogEntry logEntry = null;
            DateTime logDateTime;
            DateTime logDate = DateTime.Today;

            string line = null;
            string[] items = null;

            string fDate = logDate.ToString("yyyyMMdd");
            string newFilePath = GetFilePath(fDate);

            if (sr == null || pCurrentFilePath != newFilePath)
            {
                if (sr != null)
                {
                    try
                    {
                        sr.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.Print("!!! ReadLast close failed: " + e.Message);
                    }
                }
                pCurrentFilePath = newFilePath;

                try
                {
                    if (Directory.Exists(pFolderPath) == false)
                        Directory.CreateDirectory(pFolderPath);

                    sr = new StreamReader(
                        new FileStream(newFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                }
                catch (Exception e)
                {
                    Debug.Print("!!! ReadLast failed: " + e.Message);
                    return null;
                }
            }

            try
            {
                if (PositionFromEnd >= 0)
                    sr.BaseStream.Position = Math.Max(sr.BaseStream.Length - PositionFromEnd, 0);

                logs = new Queue<LogEntry>((int)Math.Ceiling(((double)sr.BaseStream.Length) / 150));

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    items = line.Split(LogLineSepCharArray, 3);
                    if (items.Length >= 3 &&
                        DateTime.TryParse(logDate.ToString("yyyy-MM-dd ") + items[0], out logDateTime) &&
                        (LogType == null || LogType == items[1]))
                    {
                        logEntry = new LogEntry();
                        logEntry.LogTime = logDateTime;
                        logEntry.LogType = items[1];
                        logEntry.LogMessage = items[2];
                        logs.Enqueue(logEntry);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("!!! ReadLast failed: " + e.Message);
                return null;
            }

            sr.Close();

            return logs;
        }

        public void Close()
        {
            if (sr != null)
            {
                try
                {
                    sr.Close();
                }
                catch { }
                sr.Dispose();
                sr = null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
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

        #endregion
    }
}
