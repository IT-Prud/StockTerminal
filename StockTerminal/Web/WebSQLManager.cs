using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Threading;
using System.Data;
using TradeDB;

namespace StockTerminal.Web
{
    public delegate void DataEventHandler(object source, DataEvent e);

    class WebSQLManager
    {
        public event DataEventHandler DataEvent = null;
        public ITradeDB TradeDB;

        private bool pStarted = false;
        private object DataWriterControlMutex = new object();

        private Thread WriterThread = null;
        private Thread WebOrdersReaderThread = null;
        private AutoResetEvent WriterEvent = new AutoResetEvent(false);
        private AutoResetEvent ReaderEvent = new AutoResetEvent(false);

        public List<WebOrder> ReadOrderList = new List<WebOrder>();
        public List<Action> ReadActionList = null;
        private object ReadOrderListMutex = new object();
        private object ReadActionListMutex = new object();

        private List<List<string>> WriteOrderList = new List<List<string>>(100);
        private List<List<string>> WriteDealList = new List<List<string>>(100);
        private List<List<string>> WriteFirstActionRefList = new List<List<string>>(100);
        private string LastGUIDPlacedNotReplied = "";
        private object WriteOrderListMutex = new object();
        private object WriteDealListMutex = new object();
        private object WriteFirstActionRefListMutex = new object();

        private object ConnMutex = new object();
        private const string ConnStr = "Persist Security Info=False;Data Source={0};Initial Catalog={1};User ID={2};PWD={3};";
        private SqlConnection sqlConn = null;
        private int SqlConsecutiveErrCnt = 0;

        //private static string LastGUID = "";

        public string SQLHost = null;
        public string SQLName = null;
        public string SQLUserID = null;
        public string SQLPassword = null;

        public bool Started
        {
            get
            {
                lock (DataWriterControlMutex)
                {
                    return pStarted;
                }
            }
        }

        public void Write(List<string> OrderXMLList, List<string> DealXMLList, List<string> FirstActionRefList)
        {
            if (OrderXMLList != null && OrderXMLList.Count > 0)
            {
                lock (WriteOrderListMutex)
                {
                    WriteOrderList.Add(OrderXMLList);
                }

                if (DealXMLList != null && DealXMLList.Count > 0)
                {
                    lock (WriteDealListMutex)
                    {
                        WriteDealList.Add(DealXMLList);
                    }
                }
            }

            if (FirstActionRefList != null && FirstActionRefList.Count > 0)
            {
                lock (WriteFirstActionRefListMutex)
                {
                    WriteFirstActionRefList.Add(FirstActionRefList);
                    if (FirstActionRefList.Contains(LastGUIDPlacedNotReplied))
                        LastGUIDPlacedNotReplied = "";
                }
            }

            WriterEvent.Set();
        }

        private void WebOrdersReader()
        {
            while (true)
            {
                WriterEvent.WaitOne(500, true);

                if (pStarted)
                {
                    List<WebOrder> tempWebOrderList = ReadWebOrdersFromWebDB();

                    if (tempWebOrderList == null || tempWebOrderList.Count <= 0)
                        continue;

                    lock (ReadOrderListMutex)
                    {
                        ReadOrderList.AddRange(tempWebOrderList);
                        if (ReadOrderList == null || ReadOrderList.Count <= 0)
                            continue;

                        foreach (WebOrder webOrd in ReadOrderList)
                        {
                            WebOrder webOrder = webOrd;
                            if (webOrder.AccountNo == null || webOrder.AccountNo == "" || webOrder.Side.ToString() == "" || webOrder.StockCode == null || webOrder.StockCode == "" ||
                                webOrder.Quantity <= 0 || webOrder.OrderType.ToString() == "" || webOrder.shortSell == null || webOrder.shortSell == "")
                            {
                                if (DataEvent != null)
                                    DataEvent.BeginInvoke(this, new DataEvent("Warning", "Some field is empty!"), null, null);
                                continue;
                            }

                            string Message = "";
                            bool success = false;
                            if (webOrd.LastAction == "Submit" && webOrd.Status == (int)Order.OrderStatusEnum.Pending && LastGUIDPlacedNotReplied == "")
                            {
                                string actRef;
                                success = TradeDB.OrderPlace(webOrder.AccountNo.Trim(), webOrder.Side, webOrder.StockCode, webOrder.Price, webOrder.Quantity, webOrder.OrderType, false, false, 0, ExchangeTypeEnum.HKG, out Message, out actRef);
                                //success = TradeDB.OrderPlace(webOrder.GUID, webOrder.OrderNo, webOrder.AccountNo, webOrder.Side, webOrder.StockCode, webOrder.Price, webOrder.Quantity, webOrder.OrderType, null, false, false, out Message);
                                //LastGUIDPlacedNotReplied = webOrder.GUID;
                            }
                            else if (webOrd.LastAction == "Amend" && webOrd.Status == (int)Order.OrderStatusEnum.Queue)
                            {
                                success = TradeDB.OrderAmend(webOrder.OrderNo, webOrder.Price, webOrder.Quantity, 0, out Message);
                            }
                            else if (webOrd.LastAction == "Cancel" && webOrd.Status == (int)Order.OrderStatusEnum.Queue)
                            {
                                success = TradeDB.OrderCancel(webOrder.OrderNo, out Message);
                            }

                            if (success == false && DataEvent != null)
                                DataEvent.BeginInvoke(this, new DataEvent("Warning", "Error send order to stock server! Order no.: " + webOrder.OrderNo + "\n" + Message), null, null);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if (sqlConn != null)
            {
                sqlConn.Close();
                sqlConn.Dispose();
                sqlConn = null;
            }

            WebOrdersReaderThread = null;
        }

        private List<WebOrder> ReadWebOrdersFromWebDB()
        {
            List<WebOrder> WebOrderList = null;
            if (!ConnectDB())
                return WebOrderList;

            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.StoredProcedure;
            sqlCmd.Parameters.Clear();
            sqlCmd.Parameters.Add("AccountNo", SqlDbType.NVarChar);
            sqlCmd.Parameters["AccountNo"].Value = "NoNeed";
            sqlCmd.Parameters.Add("Status", SqlDbType.Int);
            sqlCmd.Parameters["Status"].Value = 4;
            sqlCmd.CommandText = "spWebOrderSelect";
            sqlCmd.Connection = sqlConn;

            //string SQL = "SELECT O.[Account No], O.Order_Side, O.StockCode, O.Order_Price, O.Order_Qty, O.[Order Type], O.TradeClassShortsell, * FROM [Action] as A, Orders as O where A.OrderNo = O.OrderNo and A.OG_ID = 'NEW'";
            //SqlCommand sqlCommand = new SqlCommand(SQL, sqlConn);
            SqlDataReader dataReader = null;

            try
            {
                lock (ConnMutex)
                {
                    dataReader = sqlCmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        if (dataReader["AccountNo"].ToString() == "" || dataReader["OrderNo"].ToString() == "" || dataReader["OrderSide"].ToString() == "" || dataReader["StockCode"].ToString() == "" ||
                            dataReader["Price"].ToString() == "" || dataReader["Quantity"].ToString() == "" || dataReader["OrderType"].ToString() == "" || dataReader["OrderStatus"].ToString() == "" ||
                            dataReader["LastAction"].ToString() == "") // || dataReader["GUID"].ToString() == "") //|| dataReader["TradeClassShortsell"].ToString() == "")
                        {
                            if (DataEvent != null)
                                DataEvent.BeginInvoke(this, new DataEvent("Warning", "Some field is empty."), null, null);
                            return null;
                        }

                        WebOrder order = new WebOrder();
                        int.TryParse (dataReader["OrderNo"].ToString(), out order.OrderNo);
                        order.AccountNo = dataReader["AccountNo"].ToString();
                        char.TryParse(dataReader["OrderSide"].ToString(), out order.Side);
                        order.StockCode = dataReader["StockCode"].ToString();
                        decimal.TryParse(dataReader["Price"].ToString(), out order.Price);
                        int.TryParse(dataReader["Quantity"].ToString(), out order.Quantity);
                        char.TryParse(dataReader["OrderType"].ToString(), out order.OrderType);
                        int.TryParse(dataReader["OrderStatus"].ToString(), out order.Status);
                        order.LastAction = dataReader["LastAction"].ToString();
                        //order.GUID = dataReader["GUID"].ToString();
                        //order.shortSell = dataReader["TradeClassShortsell"].ToString();
                        order.shortSell = "N";

                        if (WebOrderList == null)
                            WebOrderList = new List<WebOrder>();
                        WebOrderList.Add(order);
                    }
                }
            }
            catch (Exception excep)
            {
                if (DataEvent != null)
                    DataEvent.BeginInvoke(this, new DataEvent("Error", "Read WebOrders From WebDB: " + (excep != null ? " " + excep.Message : "")), null, null);
            }

            //if (sqlConn != null)
            //{
            //    sqlConn.Close();
            //    sqlConn = null;
            //}

            //if (WebOrderList == null && DataEvent != null)
            //    DataEvent.BeginInvoke(this, new DataEvent("Error", "Previous index = 0. : " + SQL), null, null);

            return WebOrderList;
        }

        private void OrdersWriterThread()
        {
            List<string>[] OrderListArray = null;
            List<string>[] DealListArray = null;
            int WaitTime = Timeout.Infinite;

            while (true)
            {
                WriterEvent.WaitOne(WaitTime, true);
                WaitTime = Timeout.Infinite;

                if (pStarted)
                {
                    // if previous ListArray is not yet cleared then 
                    // try the previous ListArray before getting new data
                    if (OrderListArray == null || OrderListArray.Length <= 0)
                    {
                        if (WriteOrderList.Count > 0)
                        {
                            lock (WriteOrderListMutex)
                            {
                                OrderListArray = WriteOrderList.ToArray();
                                WriteOrderList.Clear();
                            }
                        }
                    }
                    if (DealListArray == null || DealListArray.Length <= 0)
                    {
                        if (WriteDealList.Count > 0)
                        {
                            lock (WriteDealListMutex)
                            {
                                DealListArray = WriteDealList.ToArray();
                                WriteDealList.Clear();
                            }
                        }
                    }

                    if (!PassXMLToStoredProc(ref OrderListArray, "OrderXML", "spWebServiceOrdersAddUpdate"))
                        WaitTime = 0;
                    else
                        PassXMLToStoredProc(ref DealListArray, "DealXML", "spWebServiceDealsAddUpdate");
                }
                else
                {
                    break;
                }
            }

            if (sqlConn != null)
            {
                sqlConn.Close();
                sqlConn.Dispose();
                sqlConn = null;
            }

            WriterThread = null;
        }

        private bool PassXMLToStoredProc(ref List<string>[] OrderListArray, string xmlName, string storedProcName)
        {
            int recordsAffected = 0, sqlResult = 0;
            int retryCount = 0;
            Exception excep = null;
            SqlCommand sqlCmd = new SqlCommand();

            sqlCmd.Parameters.Clear();
            sqlCmd.CommandType = CommandType.StoredProcedure;
            sqlCmd.Parameters.Add(xmlName, SqlDbType.NText);
            sqlCmd.Parameters.Add("Result", SqlDbType.Int);
            sqlCmd.Parameters["Result"].Direction = ParameterDirection.Output;
            sqlCmd.CommandText = storedProcName;

            if (OrderListArray != null && OrderListArray.Length > 0)
            {
                if (ConnectDB())
                {
                    foreach (List<string> dataList in OrderListArray)
                    {
                        if (dataList != null)
                        {
                            foreach (string s in dataList)
                            {
                                sqlCmd.Connection = sqlConn;
                                sqlCmd.Parameters[xmlName].Value = s;

                                recordsAffected = -1;
                                retryCount = 0;

                                while (retryCount < 5)
                                {
                                    excep = null;
                                    try
                                    {
                                        lock (ConnMutex)
                                        {
                                            if ((recordsAffected = sqlCmd.ExecuteNonQuery()) > 0 && (sqlResult = (int)sqlCmd.Parameters["Result"].Value) == 0)
                                            {
                                                if (DataEvent != null)
                                                    DataEvent.BeginInvoke(this, new DataEvent("Writing " + xmlName + " to SQL ", recordsAffected + " rows effected."), null, null);
                                                break;
                                            }
                                            else
                                            {
                                                if (DataEvent != null)
                                                    DataEvent.BeginInvoke(this, new DataEvent("Error", "Write Fail != 0 " + retryCount), null, null);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex.Message.IndexOf("XML parsing error") >= 0)
                                        {
                                            ConnectDB();
                                            if (DataEvent != null)
                                                DataEvent.BeginInvoke(this, new DataEvent("Error", "XML parsing error "), null, null);
                                            break;
                                        }
                                        else
                                            excep = ex;
                                    }

                                    SqlConsecutiveErrCnt++;
                                    ConnectDB();
                                    retryCount++;
                                }

                                if (retryCount >= 5)
                                {
                                    if (DataEvent != null)
                                        DataEvent.BeginInvoke(this, new DataEvent("Error", "Failed to write to db." + (excep != null ? " " + excep.Message : "")), null, null);
                                    Thread.Sleep(200);
                                }
                            }
                        }
                    }

                    OrderListArray = null;
                }
                else
                {
                    if (DataEvent != null)
                        DataEvent.BeginInvoke(this, new DataEvent("Error", "Failed to connect db."), null, null);
                    Thread.Sleep(300);
                    return false;
                }
            }
            return true;
        }

        public bool Start()
        {
            lock (DataWriterControlMutex)
            {
                if (pStarted == true) return true;
                if (WriterThread != null || WebOrdersReaderThread != null) return false;

                WriterThread = new Thread(new ThreadStart(OrdersWriterThread));
                WriterThread.Start();
                WriterEvent.Set();

                WebOrdersReaderThread = new Thread(new ThreadStart(WebOrdersReader));
                WebOrdersReaderThread.Start();
                ReaderEvent.Set();

                pStarted = true;
            }

            return true;
        }

        public void Stop()
        {
            lock (DataWriterControlMutex)
            {
                if (pStarted == true)
                {
                    pStarted = false;
                    WriterEvent.Set();
                }
            }
        }

        public void Clear()
        {
            lock (WriteOrderListMutex)
            {
                WriteOrderList.Clear();
            }
            lock (WriteDealListMutex)
            {
                WriteDealList.Clear();
            }
        }

        private bool ConnectDB()
        {
            if (sqlConn != null)
            {
                if (sqlConn.State == ConnectionState.Open)
                {
                    if (SqlConsecutiveErrCnt < 3) return true;
                }

                try
                {
                    lock (ConnMutex)
                    {
                        sqlConn.Close();
                    }
                }
                catch { }
                sqlConn = null;
            }

            SqlConsecutiveErrCnt = 0;

            if (SQLHost == null || SQLName == null || SQLUserID == null || SQLPassword == null) return false;

            lock (ConnMutex)
            {
                sqlConn = new SqlConnection(String.Format(ConnStr, SQLHost, SQLName, SQLUserID, SQLPassword));

                try
                {
                    sqlConn.Open();
                    return true;
                }
                catch (Exception)
                {
                    try
                    {
                        sqlConn.Close();
                    }
                    catch { }
                    sqlConn = null;
                }
            }

            return false;
        }

    }
}
