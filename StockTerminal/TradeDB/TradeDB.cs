using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Net;
using TradeDB.Net;
using Processors;
using Utils;

namespace TradeDB
{
    public class HostEndPoint
    {
        public HostEndPoint(string Name, int Port)
        {
            this.Name = Name;
            this.Port = Port;
        }

        public readonly string Name;
        public readonly int Port;
    }

//    public delegate void ConnectionStatusDelegate(int Status);
    public delegate void SystemEventDelegate(SystemEvent TheSystemEvent);
    public delegate void CentralUserSettingDelegate(List<SettingKeyValue> KeyValues);
    public delegate void OrderStatusDelegate(List<Order> Orders);
    public delegate void OddLotOrderDelegate(List<OddLotOrder> Orders);
    public delegate void AccountDelegate(List<Account> Accounts);
    public delegate void AccountListDelegate(List<string> AccountList);
    public delegate void AEDelegate(List<AccountExecutive> AEs);
    public delegate void StockDelegate(Stock TheStock);
    public delegate void IndexDelegate(List<Index> Indexes);
    public delegate void TransactionChargeDelegate(List<TransactionCharge> TransactionCharges);

    public enum ProcessState { Stopped, Login, Connecting, Ready, Disconnecting, MaxState };
    public enum UserTypeEnum { Client, AE, Quote };
    public enum ExchangeTypeEnum { HKG, PMHKG, FTHKG, SHG, SZE, Gray, Unassigned };
    public enum GetRefreshTypeEnum { NoRefresh, RefreshIfNotExist, ForceRefresh };
    public enum GetStockInfoType { Basic, BasicAndPriceDepth };
    public enum LastErrorEnum { NoError, LoginNoResponse, InvalidPassword, SendingVerificationCode, InvalidVerificationCode, PasswordForceChange, ServiceUnavailable, MaxLoginAttemptReached, ForceLogout, LoadDataFailed };
    public enum ConnectionTypeEnum { Unknown, Local, Internet };

    public interface ITradeDB
    {
        string UserId { get; set; }
        string Password { get; set; }
        int PasswordForceChange { get; }
        string VerificationCode { get; set; }
        string VerificationCodeSendMethod { get; set; }
        string UserSystemName { get; set; }
        string UserSystemVersion { get; set; }
        string IPType { get; }
        string DeploymentGroupName { get; set; }
        string Email { get; }
        string SMSNo { get; }
        UserTypeEnum UserType { get; }
        HostEndPoint CurrentLoginHostTrade { get; }
        HostEndPoint CurrentLoginHostQuote { get; }
        IPEndPoint StockUdpDataEP { get; set; }
        bool StockUdpDataEnable { get; set; }
        bool MessageLogEnabled { get; set; }
        bool OrderBookSynced { get; }
        bool RequireOTP { get; set; }
        string LogFolder { get; set; }
        CultureInfo Culture { get; set; }
        int QuoteState { get; }
        int TradeState { get; }
        ConnectionTypeEnum ConnectionType { get; }

        SpreadTableSet SpreadTables { get; }

        StockRelationBook StockRelationBook { get; }

        LastErrorEnum SessionLastTradeError { get; }
        LastErrorEnum SessionLastQuoteError { get; }

        DateTime ServerTime { get; }

        DataProcessor<SettingKeyValue> DPCentralUserSetting { get; set; }
        DataProcessor<Order> DPOrder { get; set; }
        DataProcessor<Account> DPAccount { get; set; }
        DataProcessor<Stock> DPStock { get; set; }
        DataProcessor<Index> DPIndex { get; set; }
        DataProcessor<MarketTurnover> DPMarketTurnover { get; set; }
        DataProcessor<TransactionCharge> DPTxnCharge { get; set; }
        DataProcessor<OddLotOrder> DPOddLotOrder { get; set; }

        void ListenSystemEvent(SystemEventDelegate systemEventDelegate);
        void UnListenSystemEvent(SystemEventDelegate systemEventDelegate);
        void ListenConnectionStatus(StateChangedDelegate connectionStatusDelegate);
        void UnListenConnectionStatus(StateChangedDelegate connectionStatusDelegate);
        void ListenQuoteConnectionStatus(StateChangedDelegate quoteConnectionStatusDelegate);
        void UnListenQuoteConnectionStatus(StateChangedDelegate quoteConnectionStatusDelegate);
        //void ListenCentralUserSetting(CentralUserSettingDelegate centralUserSettingDelegate);
        //void UnListenCentralUserSetting(CentralUserSettingDelegate centralUserSettingDelegate);
        //void ListenOrderStatus(OrderStatusDelegate orderStatusDelegate);
        //void UnListenOrderStatus(OrderStatusDelegate orderStatusDelegate);
        //void ListenOddLotOrder(OddLotOrderDelegate oddLotOrderDelegate);
        //void UnListenOddLotOrder(OddLotOrderDelegate oddLotOrderDelegate);
        void ListenAccountList(AccountListDelegate accountListDelegate);
        void UnListenAccountList(AccountListDelegate accountListDelegate);
        //void ListenAccount(AccountDelegate accountDelegate);
        //void UnListenAccount(AccountDelegate accountDelegate);
        void ListenAE(AEDelegate aeDelegate);
        void UnListenAE(AEDelegate aeDelegate);
        //void ListenStock(StockDelegate stockDelegate);
        //void UnListenStock(StockDelegate stockDelegate);
        //void ListenIndex(IndexDelegate indexDelegate);
        //void UnListenIndex(IndexDelegate indexDelegate);
        //void ListenTransactionCharge(TransactionChargeDelegate transactionChargeDelegate);
        //void UnListenTransactionCharge(TransactionChargeDelegate transactionChargeDelegate);

        void AddLoginHostTrade(HostEndPoint HostEP);
        void ClearLoginHostTrade();
        void AddLoginHostQuote(HostEndPoint HostEP);
        void ClearLoginHostQuote();

        void Connect();
        void Disconnect();

        SettingKeyValue GetSetting(string Key);
        void RequestSetting(List<string> SettingKeyList, bool Listen);
        void UnrequestSetting(List<string> SettingKeyList);
        //List<SettingKeyValue> GetSettingByKey(List<string> KeyList, out List<string> KeyListNotFound, GetRefreshTypeEnum RefreshType);
        bool SetSetting(SettingKeyValue KeyValue);

        List<Order> GetOrder();
        List<Order> GetOrderByOrderNo(List<int> OrderNoList);
        List<Order> GetOrderByAECode(string AECode);
        List<Order> GetOrderByAccountNo(string AccountNo);
        //List<Order> GetOrderByStockCode(string StockCode);
        List<Order> GetOrderByStatus(int Status);
        List<Order> GetOrderByReplied(char Replied);
        void RequestOrder(List<string> OrderNoList);
        void UnRequestOrder(List<string> OrderNoList);

        List<OddLotOrder> GetOddLotOrder();
        List<OddLotOrder> GetOddLotOrderByStockCode(List<string> StockCode);

        List<string> GetAccountList();
        string[] GetAccountList(string SearchText);
        List<string> GetAccountListByAECode(string AECode);
        //List<string> GetAccountListByStockCode(string StockCode);
        void RequestAccountList();

        Account GetAccount(string AccountNo);
        //List<Account> GetAccountByAccountNo(List<string> AccountNo, out List<string> AccountNoNotFound, GetRefreshTypeEnum RefreshType);
        List<Account> GetAccountByAECode(string AECode);
        //List<Account> GetAccountByStockCode(string StockCode);
        //void RequestAccount(List<string> AccountNoList);
        //void UnRequestAccount(List<string> AccountNoList);
        void RequestAccount(List<string> AccountNoList, bool Listen);
        void UnrequestAccount(List<string> AccountNoList);

        List<AccountExecutive> GetAEByCode(string Code);
        int GetAECount();
        List<AccountExecutive> GetAEList();

        void RequestMarket();

        //List<Stock> GetStock(List<string> StockSignatureList, out List<string> StockSignatureNotFound, GetRefreshTypeEnum RefreshType, GetStockInfoType InfoType, out List<string> StockSignatureFound);
        Stock GetStock(string StockSignature);
        Dictionary<string, Stock> GetStock(ICollection<string> StockSignatures);
        //List<Stock> GetStockByMarketCode(string MarketCode);
        //void RequestStock(List<string> StockSignatureList);
        //void UnRequestStock(List<string> StockSignatureList);
        void RequestStock(List<string> StockSignatureList, bool Listen);
        void UnrequestStock(List<string> StockSignatureList);

        //OddLotOrder GetOddLotOrder(string OddLotOrderNo);
        void RequestServerOddLotOder(List<string> StockSignatureList, bool Listen);
        void UnRequestServerOddLotOder(List<string> StockSignatureList);

        //List<Index> GetIndex(List<string> IndexCodeList, out List<string> IndexCodeNotFound, GetRefreshTypeEnum RefreshType);
        Index GetIndex(string IndexCode);
        //void RequestIndex(List<string> IndexCodeList);
        //void UnRequestIndex(List<string> IndexCodeList);
        void RequestIndex(List<string> IndexCodeList, bool Listen);
        void UnrequestIndex(List<string> IndexCodeList);

        //List<TransactionCharge> GetTransactionCharge(List<string> TransactionChargeCodeList, out List<string> TransactionChargeCodeNotFound, GetRefreshTypeEnum RefreshType);
        TransactionCharge GetTransactionCharge(string TxnChargeHashList);
        void RequestTransactionCharge(List<string> TxnChargeHashList, bool Listen);
        void UnrequestTransactionCharge(List<string> TxnChargeHashList);

        bool OrderPlace(string AccountNo, char Side, string StockCode, decimal Price, int Quantity, char OrderType, bool AllOrNothing, bool ShortSell, int CreditCheckByPass, ExchangeTypeEnum ExType, char DPGW, bool AGGO, string OTP, out string ErrorMessage, out string ActionRefNo);
        bool OrderPlace(string AccountNo, char Side, string StockCode, decimal Price, int Quantity, char OrderType, bool AllOrNothing, bool ShortSell, int CreditCheckByPass, ExchangeTypeEnum ExType, out string ErrorMessage, out string ActionRefNo);
        bool OrderAmend(int OrderNo, decimal NewPrice, int NewQuantity, int CreditCheckByPass, out string ErrorMessage);
        bool OrderAmend(int OrderNo, decimal NewPrice, int NewQuantity, int CreditCheckByPass, string OTP, out string ErrorMessage);
        bool OrderCancel(int OrderNo, out string ErrorMessage);
        bool OrderCancel(List<int> OrderNoList, out string ErrorMessage, out List<int> OrderCancelNotAccepted);
        bool OrderReplied(int OrderNo, out string ErrorMessage);
        bool OrderChangeAccount(int OrderNo, string AccountNo, out string ErrorMessage);
    }
}

