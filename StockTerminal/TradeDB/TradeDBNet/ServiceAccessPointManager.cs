using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TradeDB.Net
{
    public class ServiceAccessPointManager
    {
        private readonly List<HostEndPoint> SessionHostList = new List<HostEndPoint>(10);
        private int CurrentSessionHostIdx = 0;
        private int OriginSessionHostIdx = -1;
        private readonly Dictionary<string, int> AttemptedServer = new Dictionary<string, int>(20);

        public HostEndPoint CurrentSessionHost
        {
            get
            {
                if (CurrentSessionHostIdx >= 0 && CurrentSessionHostIdx < SessionHostList.Count)
                    return SessionHostList[CurrentSessionHostIdx];

                return null;
            }
        }

        public int SessionHostCount
        {
            get { return SessionHostList.Count; }
        }

        public void AddSessionHost(HostEndPoint HostEP)
        {
            if (HostEP != null)
                SessionHostList.Add(HostEP);
        }

        public void ClearSessionHost()
        {
            SessionHostList.Clear();
        }

        public void AddAttemptedServer(IPEndPoint ServerEP)
        {
            if (ServerEP != null)
                AttemptedServer[ServerEP.ToString()] = 1;
        }

        public void ClearAttemptedServer()
        {
            AttemptedServer.Clear();
        }

        public bool IsServerAttempted(IPEndPoint ServerEP)
        {
            return ServerEP != null && 
                AttemptedServer.ContainsKey(ServerEP.ToString());
        }

        public void SetOrigin()
        {
            OriginSessionHostIdx = CurrentSessionHostIdx;
        }

        public bool GoNextSessionHost()
        {
            AttemptedServer.Clear();

            CurrentSessionHostIdx++;

            if (CurrentSessionHostIdx >= SessionHostList.Count)
                CurrentSessionHostIdx = 0;

            return CurrentSessionHostIdx == OriginSessionHostIdx;
        }
    }

}
