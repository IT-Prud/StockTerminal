using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Logger;

namespace Utils
{
    public class HealthMonitor
    {
        public enum HealthState { Unknown, Normal, Abnormal, Unregister };

        public class HealthReporter
        {
            public HealthMonitor TheHealthMonitor = null;
            public string Id = null;

            private HealthState LastState = HealthState.Unknown;
            private string LastMessage = null;
            private int LastReportTime = ProcessTimer.TickCount - 604800000;

            public void ReportHealth(HealthMonitor.HealthState State, string Message)
            {
                int nowTime = ProcessTimer.TickCount;

                if ((nowTime - LastReportTime) >= 1000 || State != LastState || Message != LastMessage)
                {
                    if (TheHealthMonitor != null && Id != null)
                    {
                        TheHealthMonitor.ReportHealth(Id, State, Message);

                        LastState = State;
                        LastMessage = Message;
                        LastReportTime = nowTime;
                    }
                }
            }
        }

        private class HealthReport
        {
            private static readonly QueueLockFree<HealthReport> Pool = new QueueLockFree<HealthReport>();

            private string pId;
            private HealthState pState;
            private string pMessage;

            private DateTime pTimeReport;
            private bool pChanged;

            public string Id
            {
                get { return pId; }
            }

            public HealthState State
            {
                get { return pState; }
            }

            public string Message
            {
                get { return pMessage; }
            }

            public bool IsExpired
            {
                get { return ((DateTime.Now - TimeReport).TotalSeconds > 10) ? true : false; }
            }

            public DateTime TimeReport
            {
                get { return pTimeReport; }
            }

            public void FetchChanged(ref bool Changed)
            {
                if (pChanged)
                {
                    Changed = true;
                    pChanged = false;
                }
            }

            private HealthReport() { }

            static HealthReport()
            {
                for (int i = 0; i < 100; i++)
                    Pool.Enqueue(new HealthReport());
            }

            public static HealthReport Get(string Id, HealthState State, string Message)
            {
                HealthReport report = Pool.Dequeue();

                if (report == null)
                    report = new HealthReport();

                report.pId = Id;
                report.pState = State;
                report.pMessage = Message;
                report.pTimeReport = DateTime.Now;
                report.pChanged = false;

                return report;
            }

            public void Recycle()
            {
                Pool.Enqueue(this);
            }

            public void DoCompare(HealthReport Report)
            {
                if (Report == null || Report.Id != Id || Report.pState != pState || Report.pMessage != pMessage)
                    pChanged = true;
            }
        }

        public IPEndPoint HostIPEndPoint = null;

        public string SourceID = null;
        public string DestID = null;

        public int NormalBeatInterval = 0;

        private readonly QueueLockFree<HealthReport> HealthReportQueue = new QueueLockFree<HealthReport>();
        private readonly Dictionary<string, HealthReport> HealthReportDict = new Dictionary<string, HealthReport>(20);

        private int pStarted = 0;
        private readonly AutoResetEvent ProcessHealthReportEvent = new AutoResetEvent(false);
        private volatile Thread ProcessThread = null;
        private int pTimeLastBeat = ProcessTimer.TickCount - 600000;

        private readonly UdpClient Udp = new UdpClient();

        private Log pTheLog = null;

        private static readonly byte[] Hex = new byte[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70 };

        public Log TheLog
        {
            get { return pTheLog; }

            set
            {
                pTheLog = value;

                if (pTheLog != null)
                {
                    pTheLog.FlushAfterAppend = true;
                }
            }
        }

        public bool Start()
        {
            if (Interlocked.CompareExchange(ref pStarted, 1, 0) == 0)
            {
                if (pTheLog != null)
                    pTheLog.Purge(14);

                ProcessThread = new Thread(new ThreadStart(ProcessHealthReport));
                ProcessThread.Start();
                return true;
            }
            else
                return ProcessThread != null ? true : false;
        }

        public void Stop()
        {
            ProcessThread = null;

            ProcessHealthReportEvent.Set();
        }

        private void ClearHealthReportQueue()
        {
            HealthReport report;

            while ((report = HealthReportQueue.Dequeue()) != null)
                if (report != null)
                    report.Recycle();
        }

        public void ReportHealth(string Id, HealthState State, string Message)
        {
            if (Id != null)
            {
                HealthReport report = HealthReport.Get(Id, State, Message);

                HealthReportQueue.Enqueue(report);
                ProcessHealthReportEvent.Set();
            }
        }

        private void ProcessHealthReport()
        {
            HealthReport report, reportNew, reportOld;
            bool changed = false;
            bool isHealthy;
            bool isReportExist = false;
            StringBuilder sb = new StringBuilder(2000);
            string msg;
            string sep;
            int nowTime;

            while (ProcessThread != null)
            {
                sb.Remove(0, sb.Length);
                sep = null;
                changed = false;
                isHealthy = true;

                while ((reportNew = HealthReportQueue.Dequeue()) != null)
                {
                    if (reportNew.State == HealthState.Unregister)
                    {
                        if (HealthReportDict.TryGetValue(reportNew.Id, out reportOld))
                        {
                            HealthReportDict.Remove(reportNew.Id);

                            if (reportOld != null)
                                reportOld.Recycle();
                        }
                    }
                    else
                    {
                        if (HealthReportDict.TryGetValue(reportNew.Id, out reportOld))
                        {
                            reportNew.DoCompare(reportOld);

                            if (reportOld != null)
                                reportOld.Recycle();
                        }
                        else
                            reportNew.DoCompare(reportOld);

                        HealthReportDict[reportNew.Id] = reportNew;
                    }
                }

                foreach (KeyValuePair<string, HealthReport> kvp in HealthReportDict)
                {
                    if ((report = kvp.Value) != null)
                    {
                        isReportExist = true;

                        msg = (report.Message ?? "").Trim();

                        if (report.State == HealthState.Normal)
                        {
                            if (report.IsExpired)
                            {
                                isHealthy = false;
                                sb.Append(sep + report.Id + "-Lost contact-" + report.TimeReport.ToString("mm:ss"));
                                sep = " | ";
                            }
                            else if (msg.Length > 0)
                            {
                                sb.Append(sep + report.Id + "-" + msg);
                                sep = " | ";
                            }
                        }
                        else
                        {
                            isHealthy = false;
                            sb.Append(sep + report.Id + "-" + (msg.Length > 0 ? msg : report.State.ToString()));
                            sep = " | ";
                        }

                        report.FetchChanged(ref changed);
                    }
                }

                nowTime = ProcessTimer.TickCount;

                if (isReportExist)
                {
                    if (isHealthy)
                    {
                        if (changed || (nowTime - pTimeLastBeat) >= NormalBeatInterval)
                        {
                            Beat("20", "Everything is normal" + sep + sb.ToString());
                            pTimeLastBeat = nowTime;
                        }
                    }
                    else if (changed || (nowTime - pTimeLastBeat) >= NormalBeatInterval)
                    {
                        msg = sb.ToString();

                        AppendLog("", msg);

                        Beat("24", msg);
                        pTimeLastBeat = nowTime;
                    }
                }
                else if ((nowTime - pTimeLastBeat) >= NormalBeatInterval)
                {
                    msg = "Missing health information.";

                    AppendLog("", msg);

                    Beat("24", msg);
                    pTimeLastBeat = nowTime;
                }

                ProcessHealthReportEvent.WaitOne(1000, true);
            }

            ClearHealthReportQueue();
            HealthReportDict.Clear();

            Interlocked.Exchange(ref pStarted, 0);
        }

        private byte[] GetMessage(string MessageType, string Message)
        {
            if (MessageType == null || SourceID == null || DestID == null) return null;
            if (Message == null)
            {
                Message = "";
            }
            else if (Message.Length > 9999)
            {
                Message = Message.Substring(0, 9999);
            }

            string msg = String.Format("LRE{0:0000}{1:0000}{2:0000}{3:00}{4}000", Message.Length + 12, SourceID.Substring(0, 4), DestID.Substring(0, 4), MessageType.Substring(0, 2), Message);
            byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
            msgBuffer[msgBuffer.Length - 1] = 3;

            int LRC = 0;
            int Index;
            for (Index = 8; Index < (Message.Length + 17); Index++)
            {
                LRC ^= msgBuffer[Index];
            }

            msgBuffer[Index] = Hex[LRC >> 4];
            msgBuffer[Index + 1] = Hex[LRC & 15];
            msgBuffer[Index + 2] = 3;

            return msgBuffer;
        }

        private void Beat(string MessageType, string Message)
        {
            if (HostIPEndPoint != null)
            {
                byte[] msg = GetMessage(MessageType, Message);

                if (msg != null && msg.Length > 0)
                    Udp.Send(msg, msg.Length, HostIPEndPoint);
            }
        }

        private void AppendLog(string MessageType, string Message)
        {
            if (pTheLog != null)
                pTheLog.Append(MessageType, Message);
        }
    }
}
