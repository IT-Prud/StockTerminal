using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Processors;

namespace TradeDB.Net
{
    public partial class TradeDBNet : Processor, ITradeDB
    {
        private void ProcessMessageCCK(TradeMessage Msg)
        {
            switch (Msg.MessageType)
            {
                case "02":  // General Information, Balance, Stocks
                    pDPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.ParseData, Msg);
                    break;

                case "03":  // Account List
                    MsgInOthers.Enqueue(Msg);
                    break;

                case "05": // Currency Exchange Rates
                    MsgInOthers.Enqueue(Msg);
                    break;

                case "32":  // Transaction Charges
                case "34":  // Transaction Charges with exchange code
                    pDPTxnCharge.Execute(DataProcessor<TransactionCharge>.Operation.CodeEnum.ParseData, Msg);
                    break;

                case "06": // Discretion Product Type
                    MsgInOthers.Enqueue(Msg);
                    break;

                case "07": // Account Discretion 
                    MsgInOthers.Enqueue(Msg);
                    break;
            }
        }

        private void ProcessMessageCCKOthers(TradeMessage Msg)
        {
            switch (Msg.MessageType)
            {
                case "03":  // Account List
                    ProcessMessageCCK03(Msg);
                    break;

                case "05": // Currency Exchange Rates
                    ProcessMessageCCK05(Msg);
                    break;

                case "06": // Discretion Product Groups
                    ProcessMessageCCK06(Msg);
                    break;

                case "07": // Account Discretion 
                    ProcessMessageCCK07(Msg);
                    break;
            }
        }

        private void ParseMessageAccount(TradeMessage Msg)
        {
            string AccountNo = null, ExchangeCode = null, CurrencyCode = null;
            AccountBalance accountBalance = null;
            AccountStock accountStock = null;
            byte[] body = Msg.MessageBody;
            int bodyLength = 0;
            string subType = Encoding.ASCII.GetString(body, 0, 2);
            bool needUpdate = false;

            lock (accountMutex)
            {
                switch (subType)
                {
                    case "17":
                        //case "22":
                        bodyLength = body.Length;
                        if (subType == "17")
                            AccountNo = Encoding.ASCII.GetString(body, 2, 20).Trim().ToUpper();
                        else
                            AccountNo = Encoding.ASCII.GetString(body, 2, 6).Trim().ToUpper();

                        if (accountLastUpdate == null)
                            accountLastUpdate = new Account(AccountNo);
                        else if (accountLastUpdate.AccountNo != AccountNo)
                        {
                            pAccountBook.AddUpdate(accountLastUpdate);

                            DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);
                            accountLastUpdate = new Account(AccountNo);
                        }

                        if (subType == "17")
                        {
                            if (bodyLength >= 72) accountLastUpdate.AccountName = Encoding.GetEncoding(950).GetString(body, 22, 50).Trim();
                            if (bodyLength >= 102) accountLastUpdate.Phone = Encoding.GetEncoding(950).GetString(body, 72, 30).Trim();
                            if (bodyLength >= 152) accountLastUpdate.Email = Encoding.GetEncoding(950).GetString(body, 102, 50).Trim();
                            if (bodyLength >= 154)
                            {
                                bool isBuyBlocked = Encoding.ASCII.GetChars(body, 152, 1)[0] == 'Y';
                                bool isSellBlocked = Encoding.ASCII.GetChars(body, 153, 1)[0] == 'Y';
                                if (isBuyBlocked && isSellBlocked)
                                    accountLastUpdate.ActionBlocked = (int)Account.ActionBlockedEnum.Buy | (int)Account.ActionBlockedEnum.Sell;
                                else if (isBuyBlocked)
                                    accountLastUpdate.ActionBlocked = (int)Account.ActionBlockedEnum.Buy;
                                else if (isSellBlocked)
                                    accountLastUpdate.ActionBlocked = (int)Account.ActionBlockedEnum.Sell;
                                else
                                    accountLastUpdate.ActionBlocked = 0;
                            }
                            else
                                accountLastUpdate.ActionBlocked = 0;
                            if (bodyLength >= 174) accountLastUpdate.CreditClass = Encoding.ASCII.GetString(body, 154, 20).Trim();
                            if (bodyLength >= 225) accountLastUpdate.GemDerivTrade = Encoding.ASCII.GetString(body, 224, 1).Trim();
                            if (bodyLength >= 226) accountLastUpdate.StructProdExperienced = Encoding.ASCII.GetString(body, 225, 1).Trim();
                            if (bodyLength >= 227) accountLastUpdate.AllowCscSzeChiNext = Encoding.ASCII.GetString(body, 226, 1).Trim();
                        }
                        else
                        {
                            if (bodyLength >= 40) accountLastUpdate.AccountName = Encoding.GetEncoding(950).GetString(body, 8, 32).Trim();
                            if (bodyLength >= 60) accountLastUpdate.Phone = Encoding.GetEncoding(950).GetString(body, 40, 20).Trim();
                            if (bodyLength >= 110) accountLastUpdate.Email = Encoding.GetEncoding(950).GetString(body, 60, 50).Trim();
                            //accountLastUpdate.Stocks = null;  // clearing the stocks assumes stock message will always come after this, but this assumption is wrong, thus don't clear
                        }
                        break;

                    case "18":
                        AccountNo = Encoding.ASCII.GetString(body, 2, 20).Trim().ToUpper();
                        CurrencyCode = Encoding.ASCII.GetString(body, 278, 3);
                        if (CurrencyCode == "CON")
                            CurrencyCode = "***";

                        if (accountLastUpdate == null)
                        {
                            accountLastUpdate = new Account(AccountNo);
                            needUpdate = true;
                        }
                        else if (accountLastUpdate.AccountNo != AccountNo)
                        {
                            pAccountBook.AddUpdate(accountLastUpdate);

                            DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);

                            accountLastUpdate = new Account(AccountNo);
                            needUpdate = true;
                        }

                        if (!accountLastUpdate.Balances.TryGetValue(CurrencyCode, out accountBalance))
                        {
                            accountBalance = new AccountBalance();
                            accountLastUpdate.Balances.Add(CurrencyCode, accountBalance);
                        }
                        accountLastUpdate.AccountType = (char)body[22];
                        accountLastUpdate.AECode = Encoding.ASCII.GetString(body, 23, 4).Trim();
                        decimal.TryParse(Encoding.ASCII.GetString(body, 27, 16), out accountBalance.T0DayBal);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 43, 16), out accountBalance.T1DayBal);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 59, 16), out accountBalance.T2DayBal);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 75, 16), out accountBalance.T0DayOut);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 91, 16), out accountBalance.T1DayOut);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 107, 16), out accountBalance.T2DayOut);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 123, 16), out accountBalance.AcceptableMarketValue);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 139, 16), out accountBalance.MarginCall);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 155, 16), out accountBalance.FundHold);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 171, 16), out accountBalance.Interest);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 187, 16), out accountBalance.Dividend);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 203, 16), out accountBalance.CreditLimit);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 219, 3), out accountBalance.CreditIndex);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 222, 16), out accountBalance.AvailableCredit);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 238, 16), out accountBalance.SellQueue);
                        decimal.TryParse(Encoding.ASCII.GetString(body, 254, 16), out accountBalance.UnclearChequeAmount);
                        int.TryParse(Encoding.ASCII.GetString(body, 270, 4), out accountLastUpdate.TotalStockSpecified);
                        if (body.Length >= 284 && Encoding.ASCII.GetString(body, 281, 3) == "END")
                            needUpdate = true;

                        if (needUpdate)
                        {
                            pAccountBook.AddUpdate(accountLastUpdate);

                            DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);
                            accountLastUpdate = null;
                        }
                        break;

                    case "19":
                        AccountNo = Encoding.ASCII.GetString(body, 2, 20).Trim().ToUpper();

                        if (accountLastUpdate == null)
                        {
                            accountLastUpdate = new Account(AccountNo);
                            needUpdate = true;
                        }
                        else if (accountLastUpdate.AccountNo != AccountNo)
                        {
                            pAccountBook.AddUpdate(accountLastUpdate);

                            DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);

                            accountLastUpdate = new Account(AccountNo);
                            needUpdate = true;
                        }

                        ExchangeCode = Encoding.ASCII.GetString(body, 22, 10).Trim().ToUpper();
                        accountStock = new AccountStock(AccountNo, ExchangeCode, Encoding.ASCII.GetString(body, 32, 20).Trim());
                        if (accountStock != null)
                        {
                            decimal.TryParse(Encoding.ASCII.GetString(body, 52, 11), out accountStock.QtyOnHand);
                            decimal.TryParse(Encoding.ASCII.GetString(body, 63, 11), out accountStock.QtyBuying);
                            decimal.TryParse(Encoding.ASCII.GetString(body, 74, 11), out accountStock.QtySelling);
                            decimal.TryParse(Encoding.ASCII.GetString(body, 85, 11), out accountStock.QtyBought);
                            decimal.TryParse(Encoding.ASCII.GetString(body, 96, 11), out accountStock.QtySold);
                            decimal.TryParse(Encoding.ASCII.GetString(body, 107, 11), out accountStock.QtyReceivable);
                            decimal.TryParse(Encoding.ASCII.GetString(body, 118, 16), out accountStock.MarketValue);
                            accountStock.Currency = Encoding.ASCII.GetString(body, 134, 3).Trim().ToUpper();
                            accountStock.SuspensionFlag = Encoding.ASCII.GetString(body, 137, 3).Trim().ToUpper();
                            accountStock.QtyInTransit = -accountStock.QtySelling + accountStock.QtyBought - accountStock.QtySold;
                            accountStock.QtyInTransitSold = -accountStock.QtySelling - accountStock.QtySold;
                            accountLastUpdate.AddUpdateStock(accountStock);
                        }
                        if (body.Length >= 143 && Encoding.ASCII.GetString(body, 140, 3) == "END")
                            needUpdate = true;

                        if (needUpdate)
                        {
                            pAccountBook.AddUpdate(accountLastUpdate);

                            DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);
                            accountLastUpdate = null;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Process Account List message
        /// </summary>
        /// <param name="Msg">Message</param>
        private void ProcessMessageCCK03(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = null;

            if ((tags = Msg.Tags) != null)
            {
                string accountList = Msg.Tags["ACNO"].Value;
                AccountAE acAe;
                string[] accountArr, accountParts = null, aeParts = null;
                string lastAECode = null;

                if (accountList == "//")    // End of Account List
                {
                    pAccountBook.AddUpdateList(AccountListUpdateList);

                    if (AccountListUpdateList.Count > 0 && pAccountListDelegate != null) pAccountListDelegate.Invoke(AccountNoListUpdateList);
                    AccountListUpdateList.Clear();

                    pAEBook.AddUpdate(AEBookUpdateList);
                    if (AEBookUpdateList.Count > 0 && pAEDelegate != null) pAEDelegate.Invoke(AEBookUpdateList);
                }
                else
                {
                    accountArr = accountList.Split(' ');

                    for (int i = 0; i < accountArr.Length; i++)
                    {
                        accountParts = accountArr[i].Split('/');
                        switch (accountParts.Length)
                        {
                            case 1:
                                AccountListUpdateList.Add(acAe = new AccountAE { AccountNo = accountParts[0].ToUpper().Trim(), AECode = lastAECode });
                                AccountNoListUpdateList.Add(acAe.AccountNo);
                                break;

                            case 2:
                                aeParts = accountParts[0].Split('&');
                                lastAECode = aeParts[0].ToUpper().Trim();
                                AccountListUpdateList.Add(acAe = new AccountAE { AccountNo = accountParts[1].ToUpper().Trim(), AECode = lastAECode });
                                AccountNoListUpdateList.Add(acAe.AccountNo);
                                AEBookUpdateList.Add(new AccountExecutive(lastAECode, aeParts.Length >= 2 ? HttpUtility.UrlDecode(aeParts[1].Trim()) : null));
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process Currency Rate message
        /// </summary>
        /// <param name="Msg">Message</param>
        private void ProcessMessageCCK05(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = null;
            double curRate;

            if ((tags = Msg.Tags) != null)
            {
                foreach (KeyValuePair<string, TradeMessageTag> kvp in tags)
                {
                    if (double.TryParse(kvp.Value.Value, out curRate))
                    {
                        Currency.SetRate(kvp.Value.Id, curRate);
                    }
                }
            }
        }

        /// <summary>
        /// Process Discretion Product Groups message
        /// </summary>
        /// <param name="Msg">Message</param>
        private void ProcessMessageCCK06(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = null;

            if ((tags = Msg.Tags) != null)
            {
                DiscretionProductGroup dpg = new DiscretionProductGroup();
                foreach (KeyValuePair<string, TradeMessageTag> kvp in tags)
                {
                    switch (kvp.Value.Id)
                    {
                        case "GID": dpg.GroupID = kvp.Value.Value; break;
                        case "GN": dpg.GroupName = kvp.Value.Value; break;
                        case "EXC": dpg.ExchangeCode = kvp.Value.Value; break;
                        case "MKCD": dpg.MarketCode = kvp.Value.Value; break;
                        case "INST": dpg.InstrumentType = kvp.Value.Value; break;
                        case "STKC": dpg.StockCode = kvp.Value.Value; break;
                        case "PDT": dpg.ProductTypes = kvp.Value.Value; break;
                    }
                }
                DiscretionProductGroupsTable.AddUpdate(dpg);
            }
        }

        /// <summary>
        /// Process Discretion Product Groups message
        /// </summary>
        /// <param name="Msg">Message</param>
        private void ProcessMessageCCK07(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = null;
            bool needUpdate = false;

            lock (accountMutex)
            {
                if ((tags = Msg.Tags) != null)
                {
                    string AccountNo = Msg.Tags["ACNO"].Value;
                    if (accountLastUpdate == null)
                    {
                        accountLastUpdate = new Account(AccountNo);
                        needUpdate = true;
                    }
                    else if (accountLastUpdate.AccountNo != AccountNo)
                    {
                        pAccountBook.AddUpdate(accountLastUpdate);

                        DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);

                        accountLastUpdate = new Account(AccountNo);
                        needUpdate = true;
                    }

                    accountLastUpdate.DACategory = Msg.Tags["DACAT"].Value;
                    string dgID = Msg.Tags["DGID"].Value.Trim();
                    if (dgID != null && dgID.Length > 0)
                    {
                        string[] items = dgID.Split(',');
                        accountLastUpdate.DiscretionGrpIDDict = new Dictionary<string, byte>(items.Length);
                        foreach (string item in items)
                        {
                            accountLastUpdate.DiscretionGrpIDDict[item] = 1;
                        }
                    }

                    if (needUpdate)
                    {
                        pAccountBook.AddUpdate(accountLastUpdate);

                        DPAccount.Execute(DataProcessor<Account>.Operation.CodeEnum.DataArrival, null, accountLastUpdate.AccountNo, accountLastUpdate);
                        accountLastUpdate = null;
                    }
                }
            }
        }

        private void ParseMessageTxnCharge(TradeMessage Msg)
        {
            Dictionary<string, TradeMessageTag> tags = null;
            string AccountNo, StockCode = null, SideStr = null;
            ExchangeTypeEnum ExType;
            char Side;
            TransactionCharge tc = null;

            if ((tags = Msg.Tags) != null)
            {
                AccountNo = tags["ACNO"].Value;
                ExType = Msg.MessageType == "34" ? Stock.GetExchangeType(tags["EXC"].Value.Trim()) : ExchangeTypeEnum.HKG;
                StockCode = tags["STKC"].Value.Trim();
                SideStr = tags["SIDE"].Value;
                Side = (SideStr != null && SideStr.Length > 0) ? SideStr[0] : '\0';

                decimal quantity1, quantity2, quantity3, quantity4, quantity5;
                decimal price1, price2, price3, price4, price5, consideration, commission, rebate, stampDuty, levy, tradingTariff, ccassFee, netAmount, tradingFee;

                if (decimal.TryParse(tags["PRC1"].Value, out price1) &&
                    decimal.TryParse(tags["PRC2"].Value, out price2) &&
                    decimal.TryParse(tags["PRC3"].Value, out price3) &&
                    decimal.TryParse(tags["PRC4"].Value, out price4) &&
                    decimal.TryParse(tags["PRC5"].Value, out price5) &&
                    decimal.TryParse(tags["QTY1"].Value, out quantity1) &&
                    decimal.TryParse(tags["QTY2"].Value, out quantity2) &&
                    decimal.TryParse(tags["QTY3"].Value, out quantity3) &&
                    decimal.TryParse(tags["QTY4"].Value, out quantity4) &&
                    decimal.TryParse(tags["QTY5"].Value, out quantity5) &&
                    decimal.TryParse(tags["CNSD"].Value, out consideration) &&
                    decimal.TryParse(tags["CMMS"].Value, out commission) &&
                    decimal.TryParse(tags["RBAT"].Value, out rebate) &&
                    decimal.TryParse(tags["STAM"].Value, out stampDuty) &&
                    decimal.TryParse(tags["LEVY"].Value, out levy) &&
                    decimal.TryParse(tags["TARF"].Value, out tradingTariff) &&
                    decimal.TryParse(tags["CCAS"].Value, out ccassFee) &&
                    decimal.TryParse(tags["NAMT"].Value, out netAmount) &&
                    decimal.TryParse(tags["TFEE"].Value, out tradingFee))
                {
                    tc = new TransactionCharge(AccountNo, Side, ExType, StockCode,
                        new List<decimal> { price1, price2, price3, price4, price5 },
                        new List<decimal> { quantity1, quantity2, quantity3, quantity4, quantity5 });

                    tc.SetCharges(consideration, commission, rebate, stampDuty, levy, tradingTariff, ccassFee, netAmount, tradingFee);

                    pTransactionChargeBook.AddUpdate(tc);

                    DPTxnCharge.Execute(DataProcessor<TransactionCharge>.Operation.CodeEnum.DataArrival, null, tc.Hash, tc);
                }
            }
        }
    }
}
