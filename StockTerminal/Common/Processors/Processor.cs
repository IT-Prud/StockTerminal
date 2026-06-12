using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Utils;

namespace Processors
{
    public class Processor : IProcessor
    {
        public enum ProcessState { Stopped, Started, MaxState };    // Must have a state <= 0 for stopping the thread
        private int ProcState = (int)ProcessState.Stopped;
        private int SubState = 0;

        private bool pMustStopBeforeAllowStart = false;
        protected bool MustStopBeforeAllowStart
        {
            get { return pMustStopBeforeAllowStart; }
        }

        private StateChangedDelegate pStateChanged = null;
        private long pStateChangedLastTime = HighResolutionClock.Ticks - 6048000000000L;

        protected enum ProcessCommand { Start, Stop };
        protected ProcessCommand Command = ProcessCommand.Stop;
        private readonly object CommandMutex = new object();

        public string Name;

        protected delegate void ProcessDelegate(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime);
        protected ProcessDelegate[] ProcessDelegates = null;

        private Thread ProcessThread = null;
        protected readonly AutoResetSpinEvent ProcessEvent = new AutoResetSpinEvent(false);
        private readonly ManualResetEvent ProcessStoppedEvent = new ManualResetEvent(true);

        private ThreadPriority pProcessThreadPriority = ThreadPriority.Normal;

        protected bool WaitSpin = false;

        protected static long GetMinWaitTime(params long[] WaitTimeArray)
        {
            long minWaitTime = -1;
            long nextWaitTime;

            if (WaitTimeArray != null)
            {
                for (int i = 0; i < WaitTimeArray.Length; i++)
                {
                    nextWaitTime = WaitTimeArray[i];
                    if (nextWaitTime >= 0 && (minWaitTime < 0 || minWaitTime > nextWaitTime))
                        minWaitTime = nextWaitTime;
                }
            }

            return minWaitTime;
        }

        protected ThreadPriority ProcessThreadPriority
        {
            get { return pProcessThreadPriority; }

            set
            {
                pProcessThreadPriority = value;

                if (ProcessThread != null)
                    ProcessThread.Priority = pProcessThreadPriority;
            }
        }

        public StateChangedDelegate StateChanged
        {
            get { return pStateChanged; }
            set { pStateChanged = value; }
        }

        public int State
        {
            get { return ProcState; }
        }

        public bool IsStarted
        {
            get { return Command == ProcessCommand.Start; }
        }

        public Processor()
        {
            InitializeProcessDelegates();
            Initialize();
        }

        ~Processor()
        {
            Stop(IProcessStopStyle.NoWait);
        }

        protected virtual void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
        }

        protected virtual void Initialize()
        {
        }

        public void Start()
        {
            ProcessStoppedEvent.Set();

            lock (CommandMutex)
            {
                Command = ProcessCommand.Start;
                ProcessStoppedEvent.Reset();
                StartProcess();
            }
        }

        public void Stop()
        {
            this.Stop(IProcessStopStyle.Wait);
        }

        public void Stop(IProcessStopStyle StopStyle)
        {
            lock (CommandMutex)
            {
                if (Command != ProcessCommand.Stop)
                {
                    Command = ProcessCommand.Stop;
                    ProcessEvent.Set();
                }
                if (StopStyle == IProcessStopStyle.Wait)
                {
                    ProcessStoppedEvent.WaitOne(Timeout.Infinite, true); //Timeout.Infinite, true);
                }
                else if (StopStyle == IProcessStopStyle.MustStopBeforeAllowStart)
                {
                    pMustStopBeforeAllowStart = true;
                }
            }
        }

        private void StartProcess()
        {
            if (ProcessThread == null)
            {
                ProcState = (int)ProcessState.Stopped;
                ProcessThread = new Thread(new ThreadStart(Process));
                ProcessThread.Priority = pProcessThreadPriority;
                Name = Name + (Name != null && Name.Length > 0 ? "-" : "") + this.GetType().ToString();
                ProcessThread.Name = Name;
                ProcessThread.Start();
            }
        }

        private void Process()
        {
            bool Run = true;
            long WaitTime = 0;
            long NewWaitTime = 0;
            int NextState;
            int NextSubState;
            bool stateChangedNeedInvoke = false;
            bool stateChangedPara = false;
            long stateChangedLastTime = HighResolutionClock.Ticks - 6048000000000L;

            pMustStopBeforeAllowStart = false;

            while (Run)
            {
                if (WaitTime != 0)
                    ProcessEvent.WaitOne(WaitTime, WaitSpin);
                /*
                else
                    ProcessEvent.Reset();
                 */

                WaitTime = Timeout.Infinite;

                NextState = ProcState;
                NextSubState = SubState;
                stateChangedNeedInvoke = false;
                NewWaitTime = WaitTime;

                Preprocess(stateChangedPara, stateChangedLastTime, ref NextState, ref NextSubState, ref NewWaitTime);

                if (NewWaitTime >= 0 && NewWaitTime < WaitTime)
                    WaitTime = NewWaitTime;

                if (NextState != ProcState)
                {
                    stateChangedPara = true;
                    stateChangedLastTime = HighResolutionClock.TickCount;
                    ProcState = NextState;
                    SubState = 0;
                    NextSubState = 0;
                    //if (pStateChanged != null) pStateChanged.Invoke(NextState);   // wait until the intended process delegate finished before informing others
                    stateChangedNeedInvoke = true;
                }
                /* Preprocess should not reset stateChangedPara
                else
                {
                    stateChangedPara = false;
                }
                 */

                NewWaitTime = WaitTime;
                ProcessDelegates[NextState].Invoke(stateChangedPara, stateChangedLastTime, ref NextState, ref NextSubState, ref NewWaitTime);

                if (WaitTime < 0 || NewWaitTime < WaitTime)
                    WaitTime = NewWaitTime;

                if (ProcState <= 0 && NextState == ProcState) Run = false;

                if (NextState != ProcState)
                {
                    stateChangedPara = true;
                    stateChangedLastTime = HighResolutionClock.TickCount;
                    ProcState = NextState;
                    SubState = 0;
                    NextSubState = 0;
                    WaitTime = 0;
                    if (pStateChanged != null) pStateChanged.Invoke(NextState); //pStateChanged.BeginInvoke(NextState, null, null);
                }
                else
                {
                    stateChangedPara = false;
                    if (NextSubState != SubState)
                    {
                        SubState = NextSubState;
                        WaitTime = 0;
                    }
                    if (stateChangedNeedInvoke && pStateChanged != null) pStateChanged.Invoke(NextState); //pStateChanged.BeginInvoke(NextState, null, null);
                }
            }

            WaitSpin = false;

            ProcessThread = null;

            ProcessStoppedEvent.Set();

            lock (CommandMutex)
            {
                if (Command == ProcessCommand.Start) StartProcess();
            }
        }

        protected virtual void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
        }

        protected virtual void ProcessStopped(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
        }
    }
}
