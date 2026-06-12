using System;
using System.Collections.Generic;
using System.Text;
using Utils;
using Processors;
using TradeDB.Net;

namespace TradeDB
{
    public class DataProcessor<D> : Processor
    {
        public class Operation
        {
            #region Statics

            public enum CodeEnum { NoAction, Get, Listen, Unlisten, ParseData, DataArrival }

            private static QueueLockFree<Operation> Pool = new QueueLockFree<Operation>();

            static Operation()
            {
                for (int i = 0; i < 2000; i++)
                    Pool.Enqueue(new Operation());
            }

            public static Operation Get(CodeEnum OpCode, DispatchDelegate TheDispatchDelegate, string DataId, D Data)
            {
                Operation op = Pool.Dequeue();

                if (op == null)
                    op = new Operation();

                op.OpCode = OpCode;
                op.TheDispatchDelegate = TheDispatchDelegate;
                op.DataId = DataId;
                op.Data = Data;
                op.TimeLastRequest = 0;
                op.RequestAttempted = 0;

                op.pInPool = false;

                return op;
            }

            public static Operation Get(CodeEnum OpCode, TradeMessage Message)
            {
                Operation op = Pool.Dequeue();

                if (op == null)
                    op = new Operation();

                op.OpCode = OpCode;
                op.Message = Message;

                op.pInPool = false;

                return op;
            }

            #endregion


            public CodeEnum OpCode;
            public DispatchDelegate TheDispatchDelegate;
            public string DataId;
            public D Data;
            public TradeMessage Message;

            public int TimeLastRequest;
            public int RequestAttempted;

            private bool pInPool = false;

            private Operation() { }

            public void SetNew(CodeEnum OpCode)
            {
                this.OpCode = OpCode;
                Data = default(D);

                TimeLastRequest = 0;
                RequestAttempted = 0;
            }

            public void Recycle()
            {
                if (!pInPool)
                {
                    Data = default(D);
                    Message = null;
                    pInPool = true;

                    Pool.Enqueue(this);
                }
            }
        }

        private class DataIdMap
        {
            private Dictionary<DispatchDelegate, Dictionary<string, bool>> DataIdDict = new Dictionary<DispatchDelegate, Dictionary<string, bool>>(100);

            public void Add(DispatchDelegate TheDispatchDelegate, string DataId)
            {
                Dictionary<string, bool> dataIdDict;

                if (TheDispatchDelegate != null && DataId != null)
                {
                    if (!DataIdDict.TryGetValue(TheDispatchDelegate, out dataIdDict))
                        DataIdDict.Add(TheDispatchDelegate, dataIdDict = new Dictionary<string, bool>(50));

                    dataIdDict[DataId] = true;
                }
            }

            public void Remove(DispatchDelegate TheDispatchDelegate, string DataId)
            {
                Dictionary<string, bool> dataIdDict;

                if (TheDispatchDelegate != null)
                {
                    if (DataIdDict.TryGetValue(TheDispatchDelegate, out dataIdDict))
                    {
                        dataIdDict.Remove(DataId);

                        if (dataIdDict.Count <= 0)
                            DataIdDict.Remove(TheDispatchDelegate);
                    }
                }
            }

            public string[] Remove(DispatchDelegate TheDispatchDelegate)
            {
                Dictionary<string, bool> dataIdDict;
                string[] dataIdArr = null;

                if (TheDispatchDelegate != null)
                {
                    if (DataIdDict.TryGetValue(TheDispatchDelegate, out dataIdDict))
                    {
                        DataIdDict.Remove(TheDispatchDelegate);

                        dataIdArr = new string[dataIdDict.Count];
                        dataIdDict.Keys.CopyTo(dataIdArr, 0);
                    }
                }

                return dataIdArr;
            }

            public void Remove(DispatchDelegate[] TheDispatchDelegates, string DataId)
            {
                Dictionary<string, bool> dataIdDict;
                DispatchDelegate dispatchDelegate;

                if (TheDispatchDelegates != null)
                {
                    for (int i = 0; i < TheDispatchDelegates.Length; i++)
                    {
                        if ((dispatchDelegate = TheDispatchDelegates[i]) != null && DataIdDict.TryGetValue(dispatchDelegate, out dataIdDict))
                        {
                            dataIdDict.Remove(DataId);

                            if (dataIdDict.Count <= 0)
                                DataIdDict.Remove(dispatchDelegate);
                        }
                    }
                }
            }

            public void Clear()
            {
                DataIdDict.Clear();
            }
        }

        private class DispatcherMap
        {
            private class Dispatcher
            {
                /// <summary>
                /// Dictionary for keeping delegates as well as their "Keep" status:
                ///     false: To be unkeep upon next data arrival.
                ///     true: Delegate need to be kept until further notice.
                /// </summary>
                private readonly Dictionary<DispatchDelegate, bool> DelegateDict = new Dictionary<DispatchDelegate, bool>(100);

                private int pTickLastDispatch = ProcessTimer.TickCount - 10000000;

                private int pKeepCount = 0;  // the number of delegates need to be kept
                private bool KeepForOnceOnly = false;

                public Dictionary<DispatchDelegate, bool>.KeyCollection Delegates
                {
                    get { return DelegateDict.Keys; }
                }

                public int TickLastDispatch
                {
                    get { return pTickLastDispatch; }
                }

                public int KeepCount
                {
                    get { return pKeepCount; }
                }

                /// <summary>
                /// Add dispatch delegate to the dispatcher.
                /// </summary>
                /// <param name="TheDispatchDelegate">The delegate to be added.</param>
                /// <param name="Keep">true if delegate need to be kept the until calling the Remove() member function, otherwise kept until first data arrived.</param>
                /// <returns>true if outstand action needed, otherwise false.</returns>
                public bool Add(DispatchDelegate TheDispatchDelegate, bool Keep)
                {
                    bool oldKeep;

                    if (TheDispatchDelegate != null)
                    {
                        if (DelegateDict.TryGetValue(TheDispatchDelegate, out oldKeep))
                        {
                            if (!oldKeep && Keep)
                            {
                                DelegateDict[TheDispatchDelegate] = Keep;

                                pKeepCount++;
                                KeepForOnceOnly = false;
                                return pKeepCount == 1; // outstand needed only when there is no keep before this "Add".
                            }
                        }
                        else
                        {
                            DelegateDict.Add(TheDispatchDelegate, Keep);

                            if (Keep)
                            {
                                pKeepCount++;
                                KeepForOnceOnly = false;
                                return pKeepCount == 1; // outstand needed only when there is no keep before this "Add".
                            }

                            return DelegateDict.Count == 1; // outstand needed only when there is no delegate before this "Add".
                        }
                    }

                    return false;
                }

                /// <summary>
                /// Remove the dispatch delegate from the dispatcher.
                /// </summary>
                /// <param name="TheDispatchDelegate">The delegate to be removed.</param>
                /// <returns>true if this dispatcher no longer needed, otherwise false.</returns>
                public bool Remove(DispatchDelegate TheDispatchDelegate)
                {
                    bool oldKeep;

                    if (TheDispatchDelegate != null)
                    {
                        if (DelegateDict.TryGetValue(TheDispatchDelegate, out oldKeep))
                        {
                            DelegateDict.Remove(TheDispatchDelegate);

                            if (oldKeep)
                            {
                                pKeepCount--;

                                if (pKeepCount <= 0)
                                {
                                    if (DelegateDict.Count > 0)
                                    {
                                        KeepForOnceOnly = true;
                                        return false;
                                    }
                                    else
                                        return true;
                                }
                                else
                                    return false;
                            }
                            else
                                return DelegateDict.Count <= 0;
                        }
                    }

                    return false;
                }

                /// <summary>
                /// Dispatch the data to the delegate(s).
                /// </summary>
                /// <param name="Data">The Data to be dispatched.</param>
                /// <param name="Delegates">The delegates that has been removed dispatch dictionary.</param>
                /// <returns>2 if this dispatcher is no longer needed and need to request unlisten; 1 if this dispatcher is no longer needed but no need to request unlisten; 0 if this dispatcher is still needed.</returns>
                public int Dispatch(D Data, out DispatchDelegate[] Delegates)
                {
                    int idx = -1;

                    if (DelegateDict.Count > pKeepCount)
                        Delegates = new DispatchDelegate[DelegateDict.Count - pKeepCount];
                    else
                        Delegates = null;

                    foreach (KeyValuePair<DispatchDelegate, bool> kvp in DelegateDict)
                    {
                        kvp.Key.Invoke(Data);

                        if (!kvp.Value) // Keep track of delegates that only want data once
                        {
                            idx++;
                            Delegates[idx] = kvp.Key;
                        }
                    }

                    pTickLastDispatch = ProcessTimer.TickCount;

                    // Remove delegates that only want data once
                    for (; idx >= 0; idx--)
                        DelegateDict.Remove(Delegates[idx]);

                    if (DelegateDict.Count > 0)
                        return 0;
                    else if (KeepForOnceOnly)
                        return 2;

                    return 1;
                }
            }

            private readonly Dictionary<string, Dispatcher> DispatchDict = new Dictionary<string, Dispatcher>(100);

            public void GetDataId(out List<string> DataIdKeep, out List<string> DataIdNotKeep)
            {
                DataIdKeep = new List<string>(DispatchDict.Count);
                DataIdNotKeep = new List<string>(DispatchDict.Count);

                foreach (KeyValuePair<string, Dispatcher> kvp in DispatchDict)
                {
                    if (kvp.Value.KeepCount > 0)
                        DataIdKeep.Add(kvp.Key);
                    else
                        DataIdNotKeep.Add(kvp.Key);
                }
            }

            public void GetDataId(out List<string> DataIdKeep, out List<string> DataIdNotKeep, int MinTickLastDispatch)
            {
                DataIdKeep = new List<string>(DispatchDict.Count);
                DataIdNotKeep = new List<string>(DispatchDict.Count);

                foreach (KeyValuePair<string, Dispatcher> kvp in DispatchDict)
                {
                    if (kvp.Value.TickLastDispatch < MinTickLastDispatch)
                    {
                        if (kvp.Value.KeepCount > 0)
                            DataIdKeep.Add(kvp.Key);
                        else
                            DataIdNotKeep.Add(kvp.Key);
                    }
                }
            }

            public bool Add(string DataId, bool Keep, DispatchDelegate TheDispatchDelegate)
            {
                Dispatcher dispatcher;

                if (!DispatchDict.TryGetValue(DataId, out dispatcher))
                    DispatchDict.Add(DataId, dispatcher = new Dispatcher());

                return dispatcher.Add(TheDispatchDelegate, Keep);
            }

            public void Remove(string DataId)
            {
                DispatchDict.Remove(DataId);
            }

            public bool Remove(string DataId, DispatchDelegate TheDispatchDelegate)
            {
                Dispatcher dispatcher;

                if (DispatchDict.TryGetValue(DataId, out dispatcher))
                {
                    if (dispatcher.Remove(TheDispatchDelegate))
                    {
                        DispatchDict.Remove(DataId);
                        return true;
                    }
                }

                return false;
            }

            public int Dispatch(string DataId, D Data, out DispatchDelegate[] Delegates)
            {
                Dispatcher dispatcher;

                Delegates = null;

                if (DispatchDict.TryGetValue(DataId, out dispatcher))
                    return dispatcher.Dispatch(Data, out Delegates);

                return -1;
            }

            public void Clear()
            {
                DispatchDict.Clear();
            }
        }


        #region Define delegates for new ProcessState

        public new enum ProcessState { Stopped, Ready, MaxState };

        protected override void InitializeProcessDelegates()
        {
            ProcessDelegates = new ProcessDelegate[(int)ProcessState.MaxState];
            ProcessDelegates[(int)ProcessState.Stopped] = new ProcessDelegate(ProcessStopped);
            ProcessDelegates[(int)ProcessState.Ready] = new ProcessDelegate(ProcessReady);
        }

        #endregion


        #region Public members

        public delegate D GetDataDelegate(string DataId);
        public delegate void RequestDataDelegate(List<string> DataId, bool Listen);
        public delegate void UnrequestDataDelegate(List<string> DataId);
        public delegate void DispatchDelegate(D Data);
        public delegate void ParseDataDelegate(TradeMessage Message);

        public GetDataDelegate GetData = null;
        public RequestDataDelegate RequestData = null;
        public UnrequestDataDelegate UnrequestData = null;
        public ParseDataDelegate ParseData = null;

        #endregion


        #region Private members

        private readonly QueueLockFree<Operation> OpQueue = new QueueLockFree<Operation>();

        private readonly DispatcherMap TheDispatcherMap = new DispatcherMap();
        private readonly DataIdMap TheDataIdMap = new DataIdMap();
        private readonly Dictionary<string, Operation> OutstandDict = new Dictionary<string, Operation>(100);

        private List<Operation> OutstandRemove = new List<Operation>(100);

        private List<string> OutstandGetDataId = new List<string>(100);
        private List<string> OutstandListenDataId = new List<string>(100);
        private List<string> OutstandUnlistenDataId = new List<string>(100);

        private int TicksNextProcessOutstand = ProcessTimer.TickCount - 10000000;

        private const int OutstandRetryTicks = 3000;
        private const int OutstandMaxRetry = 3;
        private const int OutstandDispatcherKeepAliveTicks = 60000;

        private int OutstandDispatcherLastScan = ProcessTimer.TickCount - 10000000;

        #endregion


        #region Public member functions

        public void Execute(Operation.CodeEnum OpCode, DispatchDelegate TheDispatchDelegate, string DataId, D Data)
        {
            OpQueue.Enqueue(Operation.Get(OpCode, TheDispatchDelegate, DataId, Data));

            ProcessEvent.Set();
        }

        public void Execute(Operation.CodeEnum OpCode, TradeMessage Message)
        {
            OpQueue.Enqueue(Operation.Get(OpCode, Message));

            ProcessEvent.Set();
        }

        #endregion


        #region Private member functions

        /// <summary>
        /// Add an operation to the Outstand dictionary, or replace the OpCode and SetNew if an operation of same DataId already exist.
        /// </summary>
        /// <param name="OpCode">The OpCode of the operation to be added or replaced.</param>
        /// <param name="DataId">The DataId of the operation.</param>
        /// <param name="NewOutstand">Outstand dictionary has been modified.</param>
        private void AddReplaceOutstand(Operation.CodeEnum OpCode, string DataId, ref bool NewOutstand)
        {
            Operation oldOp;

            if (OutstandDict.TryGetValue(DataId, out oldOp))
            {
                if (oldOp.OpCode != OpCode)
                {
                    oldOp.SetNew(OpCode);
                    NewOutstand = true;
                }
            }
            else
            {
                OutstandDict.Add(DataId, Operation.Get(OpCode, null, DataId, default(D)));
                NewOutstand = true;
            }
        }

        /// <summary>
        /// Add an operation to the Outstand dictionary, or do nothing if an operation with same DataId already exists.
        /// </summary>
        /// <param name="OpCode">The OpCode of the operation to be added.</param>
        /// <param name="DataId">The DataId of the operation.</param>
        private void AddOutstand(Operation.CodeEnum OpCode, string DataId)
        {
            if (!OutstandDict.ContainsKey(DataId))
                OutstandDict.Add(DataId, Operation.Get(OpCode, null, DataId, default(D)));
        }

        private int GetAndDispatch(string DataId, out DispatchDelegate[] Delegates)
        {
            D data;

            Delegates = null;

            if (DataId != null && GetData != null)
            {
                data = GetData.Invoke(DataId);

                if (!EqualityComparer<D>.Default.Equals(data, default(D)))
                    return TheDispatcherMap.Dispatch(DataId, data, out Delegates);
            }

            return 0;
        }

        private int ProcessOutstandings()
        {
            Operation op;
            int ticksWait, minTicksWait = int.MaxValue;
            int ticksNow = ProcessTimer.TickCount;
            int ticksDispatchMin = ticksNow - OutstandDispatcherKeepAliveTicks;
            List<string> dataIdKeep, dataIdNotKeep;

            if (RequestData != null && UnrequestData != null)
            {
                minTicksWait = OutstandDispatcherKeepAliveTicks - (ticksNow - OutstandDispatcherLastScan);

                if (minTicksWait <= 0)
                {
                    TheDispatcherMap.GetDataId(out dataIdKeep, out dataIdNotKeep, ticksDispatchMin);

                    for (int i = 0; i < dataIdKeep.Count; i++)
                        AddOutstand(Operation.CodeEnum.Listen, dataIdKeep[i]);

                    for (int i = 0; i < dataIdNotKeep.Count; i++)
                        AddOutstand(Operation.CodeEnum.Get, dataIdNotKeep[i]);

                    OutstandDispatcherLastScan = ticksNow;
                    minTicksWait = OutstandDispatcherKeepAliveTicks;
                }

                OutstandRemove.Clear();

                OutstandGetDataId.Clear();
                OutstandListenDataId.Clear();
                OutstandUnlistenDataId.Clear();

                foreach (KeyValuePair<string, Operation> kvp in OutstandDict)
                {
                    op = kvp.Value;

                    switch (op.OpCode)
                    {
                        case Operation.CodeEnum.Get:
                            if ((ticksWait = ticksNow - op.TimeLastRequest) >= OutstandRetryTicks)
                            {
                                if (op.RequestAttempted < OutstandMaxRetry)
                                {
                                    OutstandGetDataId.Add(op.DataId);
                                    op.RequestAttempted++;
                                    op.TimeLastRequest = ticksNow;
                                    ticksWait = OutstandRetryTicks;
                                }
                                else
                                    OutstandRemove.Add(op);
                            }
                            minTicksWait = Math.Min(minTicksWait, ticksWait);
                            break;

                        case Operation.CodeEnum.Listen:
                            if ((ticksWait = ticksNow - op.TimeLastRequest) >= OutstandRetryTicks)
                            {
                                if (op.RequestAttempted < OutstandMaxRetry)
                                {
                                    OutstandListenDataId.Add(op.DataId);
                                    op.RequestAttempted++;
                                    op.TimeLastRequest = ticksNow;
                                    ticksWait = OutstandRetryTicks;
                                }
                                else
                                    OutstandRemove.Add(op);
                            }
                            minTicksWait = Math.Min(minTicksWait, ticksWait);
                            break;

                        case Operation.CodeEnum.Unlisten:
                            OutstandRemove.Add(op);
                            OutstandUnlistenDataId.Add(op.DataId);
                            break;
                    }
                }

                if (OutstandGetDataId.Count > 0)
                    RequestData.Invoke(OutstandGetDataId, false);

                if (OutstandListenDataId.Count > 0)
                    RequestData.Invoke(OutstandListenDataId, true);

                if (OutstandUnlistenDataId.Count > 0)
                    UnrequestData.Invoke(OutstandUnlistenDataId);

                for (int i = 0; i < OutstandRemove.Count; i++)
                {
                    OutstandDict.Remove(OutstandRemove[i].DataId);
                    OutstandRemove[i].Recycle();
                }
            }

            return minTicksWait;
        }

        #endregion


        #region Processor delegates implementation

        protected override void Preprocess(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            if (Command == ProcessCommand.Start)
            {
                if (NextState == (int)ProcessState.Stopped)
                {
                    NextState = (int)ProcessState.Ready;
                }
            }
            else
            {
                if (NextState != (int)ProcessState.Stopped)
                {
                    NextState = (int)ProcessState.Stopped;
                }
            }
        }

        protected override void ProcessStopped(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            OpQueue.Clear();

            TheDispatcherMap.Clear();
            TheDataIdMap.Clear();
            OutstandDict.Clear();
        }

        protected void ProcessReady(bool StateChanged, long StateChangedLastTime, ref int NextState, ref int NextSubState, ref long WaitTime)
        {
            Operation op;
            string[] dataIdArr;
            List<string> dataIdKeep, dataIdNotKeep;
            bool newOutstand = false;
            int waitTimeOutstand;
            int dispatchResult;
            DispatchDelegate[] dispatchDelegates;

            while ((op = OpQueue.Dequeue()) != null)
            {
                switch (op.OpCode)
                {
                    case Operation.CodeEnum.Get:
                        if (op.TheDispatchDelegate != null && op.DataId != null)
                        {
                            TheDataIdMap.Add(op.TheDispatchDelegate, op.DataId);

                            if (TheDispatcherMap.Add(op.DataId, false, op.TheDispatchDelegate))
                                AddReplaceOutstand(op.OpCode, op.DataId, ref newOutstand);
                            else if (!OutstandDict.ContainsKey(op.DataId))
                            {
                                dispatchResult = GetAndDispatch(op.DataId, out dispatchDelegates);

                                if (dispatchResult == 2)
                                {
                                    AddReplaceOutstand(Operation.CodeEnum.Unlisten, op.DataId, ref newOutstand);
                                    TheDispatcherMap.Remove(op.DataId);
                                    TheDataIdMap.Remove(dispatchDelegates, op.DataId);
                                }
                                else if (dispatchResult == 1)
                                {
                                    TheDispatcherMap.Remove(op.DataId);
                                    TheDataIdMap.Remove(dispatchDelegates, op.DataId);
                                }
                            }
                        }
                        break;

                    case Operation.CodeEnum.Listen:
                        if (op.TheDispatchDelegate != null && op.DataId != null)
                        {
                            TheDataIdMap.Add(op.TheDispatchDelegate, op.DataId);

                            if (TheDispatcherMap.Add(op.DataId, true, op.TheDispatchDelegate))
                                AddReplaceOutstand(op.OpCode, op.DataId, ref newOutstand);
                            else if (!OutstandDict.ContainsKey(op.DataId))
                                GetAndDispatch(op.DataId, out dispatchDelegates);   // dispatchResult must be zero since the request is a "Listen" request that should remain effective no matter how the GetAndDispatch results
                        }
                        else if (op.TheDispatchDelegate == null && op.DataId == null)
                        {   // Relisten all
                            TheDispatcherMap.GetDataId(out dataIdKeep, out dataIdNotKeep);

                            if (dataIdKeep.Count > 0)
                            {
                                newOutstand = true;
                                for (int i = 0; i < dataIdKeep.Count; i++)
                                    AddOutstand(Operation.CodeEnum.Listen, dataIdKeep[i]);
                            }

                            if (dataIdNotKeep.Count > 0)
                            {
                                newOutstand = true;
                                for (int i = 0; i < dataIdNotKeep.Count; i++)
                                    AddOutstand(Operation.CodeEnum.Get, dataIdNotKeep[i]);
                            }
                        }
                        break;

                    case Operation.CodeEnum.Unlisten:
                        if (op.TheDispatchDelegate != null)
                        {
                            if (op.DataId == null)
                            {
                                if ((dataIdArr = TheDataIdMap.Remove(op.TheDispatchDelegate)) != null)
                                {
                                    foreach (string dataId in dataIdArr)
                                    {
                                        if (TheDispatcherMap.Remove(dataId, op.TheDispatchDelegate))
                                            AddReplaceOutstand(op.OpCode, dataId, ref newOutstand);
                                    }
                                }
                            }
                            else
                            {
                                if (TheDispatcherMap.Remove(op.DataId, op.TheDispatchDelegate))
                                    AddReplaceOutstand(op.OpCode, op.DataId, ref newOutstand);

                                TheDataIdMap.Remove(op.TheDispatchDelegate, op.DataId);
                            }
                        }
                        break;

                    case Operation.CodeEnum.ParseData:
                        ParseData(op.Message);
                        break;

                    case Operation.CodeEnum.DataArrival:
                        if ((dispatchResult = TheDispatcherMap.Dispatch(op.DataId, op.Data, out dispatchDelegates)) >= 0)
                        {
                            OutstandDict.Remove(op.DataId);

                            if (dispatchResult == 2)
                            {
                                AddReplaceOutstand(Operation.CodeEnum.Unlisten, op.DataId, ref newOutstand);
                                TheDispatcherMap.Remove(op.DataId);
                                TheDataIdMap.Remove(dispatchDelegates, op.DataId);
                            }
                            else if (dispatchResult == 1)
                            {
                                TheDispatcherMap.Remove(op.DataId);
                                TheDataIdMap.Remove(dispatchDelegates, op.DataId);
                            }
                        }
                        else    // no dispatcher found
                            AddReplaceOutstand(Operation.CodeEnum.Unlisten, op.DataId, ref newOutstand);
                        break;
                }

                op.Recycle();
            }

            if (newOutstand || (waitTimeOutstand = (TicksNextProcessOutstand - ProcessTimer.TickCount)) <= 0)
            {
                waitTimeOutstand = ProcessOutstandings();
                TicksNextProcessOutstand = ProcessTimer.TickCount + waitTimeOutstand;
            }

            WaitTime = waitTimeOutstand * 10000L;
        }

        #endregion
    }
}
