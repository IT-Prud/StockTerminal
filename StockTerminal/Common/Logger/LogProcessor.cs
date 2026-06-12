using System;
using System.Collections.Generic;
using System.Text;
using Processors;
using Utils;

namespace Logger
{
    public partial class LogProcessor : Processor
    {
        private class LogPool
        {
            public LogProcessor TheLogProcessor = null;

            private readonly QueueLockFree<Log> LogQueueIdle = new QueueLockFree<Log>();
            private readonly QueueLockFree<Log> LogQueueAll = new QueueLockFree<Log>();

            public LogPool()
            {
                Log log;

                for (int i = 0; i < 500; i++)
                {
                    log = new Log();
                    LogQueueAll.Enqueue(log);
                    LogQueueIdle.Enqueue(log);
                }
            }

            public Log Pop(string FolderPath, string FileNamePrefix, string FileNameSuffix)
            {
                Log log = LogQueueIdle.Dequeue();

                if (log == null)
                {
                    log = new Log();
                    LogQueueAll.Enqueue(log);
                }

                log.TheLogProcessor = TheLogProcessor;
                log.FolderPath = FolderPath;
                log.FileNamePrefix = FileNamePrefix;
                log.FileNameSuffix = FileNameSuffix;

                return log;
            }

            public void Push(Log TheLog)
            {
                if (TheLog != null)
                    LogQueueIdle.Enqueue(TheLog);
            }

            public List<Log> Close()
            {
                Log log = null;
                List<Log> logList = new List<Log>(50);

                while (LogQueueIdle.Dequeue() != null) ;

                while ((log = LogQueueAll.Dequeue()) != null)
                    logList.Add(log);

                return logList;
            }
        }


        #region Define delegates for new ProcessState

        public new enum ProcessState { Stopped, Ready, Closing, MaxState };

        protected override void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
            ProcessDelegates[(int)ProcessState.Ready] = new ProcessDelegate(ProcessReady);
            ProcessDelegates[(int)ProcessState.Closing] = new ProcessDelegate(ProcessClosing);
        }

        #endregion


        private readonly LogPool TheLogPool = new LogPool();
        private readonly QueueLockFree<Log> LogQueue = new QueueLockFree<Log>();


        #region Public Member Functions

        public Log Open()
        {
            return TheLogPool.Pop(null, null, ".log");
        }

        public Log Open(string FolderPath, string FileNamePrefix, string FileNameSuffix)
        {
            return TheLogPool.Pop(FolderPath, FileNamePrefix, FileNameSuffix);
        }

        public Log Open(string Name, string FolderPath, string FileNamePrefix, string FileNameSuffix)
        {
            Log log = TheLogPool.Pop(FolderPath, FileNamePrefix, FileNameSuffix);

            log.Name = Name;

            return log;
        }

        public void Set(Log TheLog)
        {
            if (TheLog != null)
                LogQueue.Enqueue(TheLog);
        }

        #endregion


        #region Private member functions

        protected override void Initialize()
        {
            TheLogPool.TheLogProcessor = this;
        }

        #endregion


        #region Process delegates implementation

        protected override void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (NextState == (int)ProcessState.Stopped)
            {
                if (Command == ProcessCommand.Start) NextState = (int)ProcessState.Ready;
            }
            else if (Command == ProcessCommand.Stop)
            {
                NextState = (int)ProcessState.Closing;
            }
        }

        private void ProcessReady(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            Log log = null;

            while ((log = LogQueue.Dequeue()) != null)
                log.Do();

            WaitTime = 5000000L;
        }

        private void ProcessClosing(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            Log log = null;
            List<Log> logList = TheLogPool.Close();
            int logCount;

            if (logList != null)
            {
                logCount = logList.Count;

                for (int i = 0; i < logCount; i++)
                {
                    log = logList[i];

                    log.Close();
                    log.Do();
                }
            }

            NextState = (int)ProcessState.Stopped;
        }

        #endregion

    }
}
