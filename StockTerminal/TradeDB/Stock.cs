using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TradeDB
{
    public class Stock
    {
        private const string SignatureSep = "^~<^}`~";
        private const string SignatureSepShort = "~";

        private static readonly char[] unit = new char[3] { 'k', 'M', 'B' };

        private struct QuantityScale
        {
            public char ShortForm;
            public decimal Scale;
            public decimal InvScale;
            public int NumOfDecimals;
        }

        private static readonly QuantityScale[] QtyScale = {
            new QuantityScale() {ShortForm = '\0', Scale = 1m, InvScale = 1m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = '\0', Scale = 1m, InvScale = 1m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = '\0', Scale = 1m, InvScale = 1m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = '\0', Scale = 1m, InvScale = 1m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = '\0', Scale = 1m, InvScale = 1m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'k', Scale = 1000m, InvScale = 0.001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'k', Scale = 1000m, InvScale = 0.001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'M', Scale = 1000000m, InvScale = 0.000001m, NumOfDecimals = 1},
            new QuantityScale() {ShortForm = 'M', Scale = 1000000m, InvScale = 0.000001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'M', Scale = 1000000m, InvScale = 0.000001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'B', Scale = 1000000000m, InvScale = 0.000000001m, NumOfDecimals = 1},
            new QuantityScale() {ShortForm = 'B', Scale = 1000000000m, InvScale = 0.000000001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'B', Scale = 1000000000m, InvScale = 0.000000001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'T', Scale = 1000000000000m, InvScale = 0.000000000001m, NumOfDecimals = 1},
            new QuantityScale() {ShortForm = 'T', Scale = 1000000000000m, InvScale = 0.000000000001m, NumOfDecimals = 0},
            new QuantityScale() {ShortForm = 'T', Scale = 1000000000000m, InvScale = 0.000000000001m, NumOfDecimals = 0}
        };

        public static ExchangeTypeEnum GetExchangeType(string ExchangeCode)
        {
            switch (ExchangeCode)
            {
                case "HKG": return ExchangeTypeEnum.HKG;
                case "SHG": return ExchangeTypeEnum.SHG;
                case "SZE": return ExchangeTypeEnum.SZE;
                case "PM-HKG": return ExchangeTypeEnum.PMHKG;
                case "FT-HKG": return ExchangeTypeEnum.FTHKG;
                default: return ExchangeTypeEnum.Unassigned;
            }
        }

        public static string GetExchangeCode(ExchangeTypeEnum ExchangeType)
        {
            switch (ExchangeType)
            {
                case ExchangeTypeEnum.HKG: return "HKG";
                case ExchangeTypeEnum.SHG: return "SHG";
                case ExchangeTypeEnum.SZE: return "SZE";
                case ExchangeTypeEnum.PMHKG: return "PM-HKG";
                case ExchangeTypeEnum.FTHKG: return "FT-HKG";
                default: return "";
            }
        }

        public static string FormatStockCode(ExchangeTypeEnum ExchangeType, string StockCode)
        {
            if (StockCode != null)
            {
                StockCode = StockCode.Trim().TrimStart('0').ToUpper();

                if (ExchangeType == ExchangeTypeEnum.HKG)
                    StockCode = StockCode.PadLeft(5, '0');
                else if (ExchangeType == ExchangeTypeEnum.PMHKG)
                    StockCode = StockCode.PadLeft(4, '0');
            }

            return StockCode;
        }

        public static string FormatStockCode(string ExchangeCode, string StockCode)
        {
            if (StockCode != null)
            {
                StockCode = StockCode.Trim().TrimStart('0').ToUpper();

                if (ExchangeCode == "HKG")
                    StockCode = StockCode.PadLeft(5, '0');
                else if (ExchangeCode == "PM-HKG")
                    StockCode = StockCode.PadLeft(4, '0');
            }

            return StockCode;
        }

        public static string GetSignature(ExchangeTypeEnum ExchangeType, string Code)
        {
            return GetExchangeCode(ExchangeType) + SignatureSep + FormatStockCode(ExchangeType, Code);
        }

        public static string GetSignature(string ExchangeCode, string Code)
        {
            return (ExchangeCode ?? "") + SignatureSep + FormatStockCode(ExchangeCode, Code);
        }

        public static void GetExchangeCodeStockCode(string Signature, out string ExchangeCode, out string Code)
        {
            ExchangeTypeEnum exType = ExchangeTypeEnum.Unassigned;
            ExchangeCode = null;
            Code = null;

            if (Signature != null)
            {
                int idx = Signature.IndexOf(SignatureSep);
                if (idx >= 0)
                {
                    ExchangeCode = Signature.Substring(0, idx);
                    exType = GetExchangeType(ExchangeCode);
                    Code = FormatStockCode(exType, Signature.Substring(idx + SignatureSep.Length));
                }
            }
        }

        public static void GetExchangeCodeStockCodeShort(string Signature, out string ExchangeCode, out string Code)
        {
            ExchangeTypeEnum exType = ExchangeTypeEnum.Unassigned;
            ExchangeCode = null;
            Code = null;

            if (Signature != null)
            {
                int idx = Signature.IndexOf(SignatureSepShort);
                if (idx >= 0)
                {
                    ExchangeCode = Signature.Substring(0, idx);
                    exType = GetExchangeType(ExchangeCode);
                    Code = FormatStockCode(exType, Signature.Substring(idx + SignatureSepShort.Length));
                }
            }
        }

        public static void GetAltExchangeCodeStockCodeShort(string Signature, out string AltExchangeCode, out string MainExchangeCode, out string ExCode, out string Code)
        {
            ExchangeTypeEnum exType = ExchangeTypeEnum.Unassigned;
            AltExchangeCode = null;
            MainExchangeCode = null;
            ExCode = null;
            Code = null;

            if (Signature != null)
            {
                int idx = Signature.IndexOf(SignatureSepShort);
                if (idx >= 0)
                {
                    ExCode = Signature.Substring(0, idx);
                    MainExchangeCode = ExCode;

                    if (ExCode.Length > 0)
                    {
                        int altIdx = ExCode.IndexOf('-');

                        if (altIdx > 0 && altIdx < (ExCode.Length - 1))
                        {
                            AltExchangeCode = ExCode.Substring(0, altIdx);
                            MainExchangeCode = ExCode.Substring(altIdx + 1);
                        }
                    }

                    exType = GetExchangeType(ExCode);
                    Code = FormatStockCode(exType, Signature.Substring(idx + SignatureSepShort.Length));
                }
            }
        }

        public static void GetExchangeTypeStockCode(string Signature, out ExchangeTypeEnum ExchangeType, out string Code)
        {
            ExchangeType = ExchangeTypeEnum.Unassigned;
            Code = null;

            if (Signature != null)
            {
                int idx = Signature.IndexOf(SignatureSep);
                if (idx >= 0)
                {
                    ExchangeType = GetExchangeType(Signature.Substring(0, idx));
                    Code = FormatStockCode(ExchangeType, Signature.Substring(idx + SignatureSep.Length));
                }
            }
        }

        public static string GetMarketSignature(ExchangeTypeEnum ExchangeType, string MarketCode)
        {
            return GetExchangeCode(ExchangeType) + SignatureSep + (MarketCode ?? "");
        }

        public Stock(ExchangeTypeEnum ExchangeType, string Code)
        {
            this.ExchangeType = ExchangeType;
            this.ExchangeCode = GetExchangeCode(ExchangeType);
            this.Code = Code;
            this.StockSignature = Stock.GetSignature(ExchangeType, Code);
        }

        private int pStaticVersion = 0;
        public int StaticVersion
        {
            get { return pStaticVersion; }
        }

        private int pDynamicVersion = 0;
        public int DynamicVersion
        {
            get { return pDynamicVersion; }
        }

        public readonly ExchangeTypeEnum ExchangeType;
        public readonly string ExchangeCode;
        public readonly string Code = null;
        public readonly string StockSignature;

        public string StockSignatureAlternative;    // transient only, used by TradeDBNet.ParseMessageStock(), not for cloning.

        public string NameEN = null;
        public string NameENShort = null;

        //private int EncodingPageCode;

        //public byte[] NameCHTChars = new byte[16];
        //public string pNameCHT = null;
        public string NameCHT;
        /*
        {
            get
            {
                if (pNameCHT == null)
                {
                    pNameCHT = Encoding.GetEncoding(EncodingPageCode).GetString(NameCHTChars).Trim().Replace("\0", "");
                }
                return pNameCHT;
            }
        }
         */

        //public byte[] NameCHSChars = new byte[16];
        //public string pNameCHS = null;
        public string NameCHS;
        /*
        {
            get
            {
                if (pNameCHS == null)
                {
                    if (this.ExchangeType == ExchangeTypeEnum.HKG)
                        pNameCHS = Encoding.GetEncoding(EncodingPageCode).GetString(NameCHTChars).Trim().Replace("\0", ""); // better use trad chinese since the simplified chinese from HKEx got many characters missing from standard Windows fonts.
                    else
                        pNameCHS = Encoding.GetEncoding(EncodingPageCode).GetString(NameCHSChars).Trim().Replace("\0", "");
                }
                return pNameCHS;
            }
        }
         */

        public decimal Open = -1;
        public decimal High = -1;
        public decimal Low = -1;
        public decimal PrevClose = -1;
        public decimal Nominal;

        private decimal pChange;
        public decimal Change
        {
            get { return pChange; }
        }

        private decimal pChangePC;
        public decimal ChangePC
        {
            get { return pChangePC; }
        }

        public UInt64 Volume = 0;
        public UInt64 Turnover = 0;
        public int LotSize;
        public string Currency = null;
        public string InstrumentType = null;
        public int SpreadTableCode;
        public string SuspensionFlag = null;
        public string FusingFlag = null;
        public Market MarketBelong = null;
        public decimal Bid = -1;
        public decimal Ask = -1;
        public readonly UInt64[] BidVol = new UInt64[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public readonly UInt64[] AskVol = new UInt64[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public readonly int[] BidCount = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public readonly int[] AskCount = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public readonly StockTicker[] Ticker = new StockTicker[15] {
            new StockTicker(), new StockTicker(), new StockTicker(), new StockTicker(), new StockTicker(),
            new StockTicker(), new StockTicker(), new StockTicker(), new StockTicker(), new StockTicker(),
            new StockTicker(), new StockTicker(), new StockTicker(), new StockTicker(), new StockTicker()
            };
        public readonly string[] BrokerBid = new string[40];
        public readonly string[] BrokerAsk = new string[40];
        public decimal VCMReferencePrice, VCMLowerPrice, VCMUpperPrice, CASReferencePrice, CASLowerPrice, CASUpperPrice;
        public string VCMCoolOffStartTime = null, VCMCoolOffEndTime = null;
        public DateTime dtVCMCoolOffEndTime;
        public string OrderImbalanceDirection = null;
        public UInt64 OrderImbalanceQuantity;
        public int ProductType;
        public string MarketCode = null;

        public string GetBestName(CultureInfo Culture)
        {
            string name = null;

            if (Culture != null)
            {
                switch (Culture.Name)
                {
                    case "zh-CHT":
                        name = NameCHT;
                        if (name == null || name.Length <= 0) name = NameCHS;
                        if (name == null || name.Length <= 0) name = NameENShort;
                        if (name == null || name.Length <= 0) name = "Stock " + Code;
                        break;

                    case "zh-CHS":
                        name = NameCHS;
                        if (name == null || name.Length <= 0) name = NameCHT;
                        if (name == null || name.Length <= 0) name = NameENShort;
                        if (name == null || name.Length <= 0) name = "Stock " + Code;
                        break;

                    default:
                        name = NameENShort;
                        if (name == null || name.Length <= 0) name = "Stock " + Code;
                        break;
                }
            }
            else
            {
                name = NameENShort;
                if (name == null || name.Length <= 0) name = "Stock " + Code;
            }

            return name;
        }

        public static string FormatQtyValue(decimal Value)
        {
            int valueLength;
            string strValue = Value.ToString();

            int i = strValue.IndexOf('.');
            if (i >= 0) strValue = strValue.Substring(0, i);

            valueLength = strValue.Length;

            if (valueLength >= 0 && valueLength < QtyScale.Length)
            {
                strValue = ((Int64)(Math.Round(Value * QtyScale[valueLength].InvScale, QtyScale[valueLength].NumOfDecimals, MidpointRounding.AwayFromZero) * QtyScale[valueLength].Scale)).ToString();
                valueLength = strValue.Length;

                strValue = (Math.Round(Value * QtyScale[valueLength].InvScale, QtyScale[valueLength].NumOfDecimals, MidpointRounding.AwayFromZero)).ToString() + (QtyScale[valueLength].ShortForm != '\0' ? QtyScale[valueLength].ShortForm.ToString() : "");
            }

            return strValue;

            /*
            switch (Value.Length)
            {
                case 5:
                case 6: decValue = Math.Round(decValue * 0.001m, MidpointRounding.AwayFromZero) * 1000; break;
                case 7: decValue = Math.Round(decValue * 0.000001m, 1, MidpointRounding.AwayFromZero) * 1000000; break;
                case 8:
                case 9: decValue = Math.Round(decValue * 0.000001m, MidpointRounding.AwayFromZero) * 1000000; break;
                case 10: decValue = Math.Round(decValue * 0.000000001m, 1, MidpointRounding.AwayFromZero) * 1000000000; break;
                case 11:
                case 12: decValue = Math.Round(decValue * 0.000000001m, MidpointRounding.AwayFromZero) * 1000000000; break;
                case 13: decValue = Math.Round(decValue * 0.000000000001m, 1, MidpointRounding.AwayFromZero) * 1000000000000; break;
                case 14:
                case 15: decValue = Math.Round(decValue * 1000000000000, MidpointRounding.AwayFromZero) * 1000000000000; break;
            }
            long iDecValue = (long)decValue;
            Value = iDecValue.ToString();
            switch (Value.Length)
            {
                case 5:
                case 6: Value = Math.Round(decValue * 0.001m, MidpointRounding.AwayFromZero).ToString() + "k"; break;
                case 7: Value = Math.Round(decValue * 0.000001m, 1, MidpointRounding.AwayFromZero).ToString() + "M"; break;
                case 8:
                case 9: Value = Math.Round(decValue * 0.000001m, MidpointRounding.AwayFromZero).ToString() + "M"; break;
                case 10: Value = Math.Round(decValue * 0.000000001m, 1, MidpointRounding.AwayFromZero).ToString() + "B"; break;
                case 11:
                case 12: Value = Math.Round(decValue * 0.000000001m, MidpointRounding.AwayFromZero).ToString() + "B"; break;
                case 13: Value = Math.Round(decValue * 0.000000000001m, 1, MidpointRounding.AwayFromZero).ToString() + "T"; break;
                case 14:
                case 15: Value = Math.Round(decValue * 0.000000000001m, MidpointRounding.AwayFromZero).ToString() + "T"; break;
            }

            return Value;*/
        }

        public static string GetShortValue(decimal Value, int MaxDigitPlaces, int MaxDecPlace)
        {
            int unitIdx = -1;

            if (Value >= 1000)
            {
                while (Value >= 1000)
                {
                    Value = Decimal.Multiply(Value, 0.001m);
                    unitIdx++;
                }

                if (unitIdx == 1 && Value < 10 && MaxDecPlace == 0 && MaxDecPlace + 1 <= MaxDigitPlaces)    // show decimal point for Million e.g. 4.1M, but not 15.1M
                    MaxDecPlace++;
                return GetMaxDigitPlaceValue(Value, MaxDigitPlaces, MaxDecPlace) +
                    (unitIdx >= 0 && unitIdx < unit.Length ? unit[unitIdx].ToString() : "");
            }
            return Value.ToString();
        }

        /*
        public static string GetMaxDecPlaceValue(decimal Value, int MaxDecimalPlaces)
        {
            if (MaxDecimalPlaces < 0) MaxDecimalPlaces = 0;
            string valueStr = Value.ToString("N" + MaxDecimalPlaces);

            int decIdx = valueStr.IndexOf('.');

            if (decIdx >= 0)
            {
                int idx = valueStr.Length-1;
                for (; idx >= decIdx; idx--)
                {
                    if (valueStr[idx] != '0') break;
                }
                if (idx == decIdx) idx--;   // remove the decimal if all are zeros
                if (idx >= 0 && idx < (valueStr.Length - 1))
                {
                    valueStr = valueStr.Remove(idx + 1);
                }
            }

            return valueStr;
        }
         */

        public static string GetMaxDigitPlaceValue(decimal Value, int MaxDigitPlace, int MaxDecPlace)
        {
            string vStr = Value.ToString();
            int decIdx = vStr.IndexOf('.');
            if (decIdx >= 0)
            {
                MaxDigitPlace++;    // decimal is not counted in MaxDigitPlace
                if (decIdx >= (MaxDigitPlace - 1))
                {
                    vStr = vStr.Substring(0, decIdx);
                }
                else
                {
                    int maxPlace = MaxDecPlace + decIdx + 1;
                    if (maxPlace > MaxDigitPlace) maxPlace = MaxDigitPlace;
                    if (maxPlace < vStr.Length) vStr = vStr.Substring(0, maxPlace);
                    int i = vStr.Length - 1;
                    for (; i > decIdx; i--)
                    {
                        if (vStr[i] != '0') break;
                    }
                    if (i <= (vStr.Length - 1))
                    {
                        vStr = vStr.Substring(0, i > decIdx ? i + 1 : i);
                    }
                }
            }

            return vStr;
        }

        public static string GetDeltaDecPlaceValue(decimal Value, int Delta)
        {
            string v = null;

            int decPlace = Delta.ToString().Length;
            if (decPlace == 1)
            {
                v = Value.ToString("0.000");
            }
            else if (decPlace == 2)
            {
                v = Value.ToString("0.00");
            }
            else if (decPlace == 3)
            {
                v = Value.ToString("0.0");
            }
            else
            {
                v = Value.ToString();
            }

            return v;
        }

        public static string GetDispChange(decimal Value)
        {
            if (Value > 0)
                return "+" + Value.ToString("G0");

            return Value.ToString("G0");
        }

        public static string GetDispChangePC(decimal Value)
        {
            if (Value > 0)
            {
                //return "+" + GetMaxDigitPlaceValue(Value, 4, 3);
                return "+" + Value.ToString("G3");
            }
            //return GetMaxDigitPlaceValue(Value, 5, 3);
            return Value.ToString("G3");
        }

        public void UpdateStaticVersion()
        {
            pStaticVersion += 2;
            pStaticVersion |= 1;
        }

        public bool UpdateStatic(Stock NewStock)
        {
            bool significantChanged = false;

            if (NewStock != null)
            {
                pStaticVersion += 2;
                pStaticVersion |= 1;

                NewStock.pStaticVersion = pStaticVersion;
                NewStock.pDynamicVersion = pDynamicVersion;

                if (NewStock.SuspensionFlag != SuspensionFlag ||
                    NewStock.VCMCoolOffStartTime != VCMCoolOffStartTime || NewStock.VCMCoolOffEndTime != VCMCoolOffEndTime || NewStock.dtVCMCoolOffEndTime != dtVCMCoolOffEndTime ||
                    NewStock.VCMReferencePrice != VCMReferencePrice || NewStock.VCMLowerPrice != VCMLowerPrice || NewStock.VCMUpperPrice != VCMUpperPrice ||
                    NewStock.CASReferencePrice != CASReferencePrice || NewStock.CASLowerPrice != CASLowerPrice || NewStock.CASUpperPrice != CASUpperPrice ||
                    NewStock.OrderImbalanceDirection != OrderImbalanceDirection || NewStock.OrderImbalanceQuantity != OrderImbalanceQuantity ||
                    NewStock.NameEN != NameEN || NewStock.NameCHT != NameCHT || NewStock.LotSize != LotSize)
                {
                    significantChanged = true;
                }

                NameEN = NewStock.NameEN;
                NameENShort = NewStock.NameENShort;
                NameCHT = NewStock.NameCHT;
                NameCHS = NewStock.NameCHS;
                LotSize = NewStock.LotSize;
                Open = NewStock.Open;
                PrevClose = NewStock.PrevClose;

                if (PrevClose > 0 && Nominal > 0)
                {
                    NewStock.pChange = pChange = Nominal - PrevClose;
                    NewStock.pChangePC = pChangePC = Math.Round(pChange * 100 / PrevClose, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    NewStock.pChange = pChange = 0;
                    NewStock.pChangePC = pChangePC = 0;
                }

                Currency = NewStock.Currency;
                InstrumentType = NewStock.InstrumentType;
                MarketBelong = NewStock.MarketBelong.Clone();
                SpreadTableCode = NewStock.SpreadTableCode;
                SuspensionFlag = NewStock.SuspensionFlag;
                FusingFlag = NewStock.FusingFlag;
                ProductType = NewStock.ProductType;
                MarketCode = NewStock.MarketCode;

                NewStock.Nominal = Nominal; NewStock.Bid = Bid; NewStock.Ask = Ask;

                Buffer.BlockCopy(BidCount, 0, NewStock.BidCount, 0, BidCount.Length * sizeof(int));
                Buffer.BlockCopy(AskCount, 0, NewStock.AskCount, 0, AskCount.Length * sizeof(int));
                Buffer.BlockCopy(BidVol, 0, NewStock.BidVol, 0, BidVol.Length * sizeof(ulong));
                Buffer.BlockCopy(AskVol, 0, NewStock.AskVol, 0, AskVol.Length * sizeof(ulong));

                NewStock.Volume = Volume; NewStock.Turnover = Turnover; NewStock.High = High; NewStock.Low = Low;
                NewStock.InstrumentType = InstrumentType;

                NewStock.Ticker[0].Time = Ticker[0].Time; NewStock.Ticker[0].Remarks = Ticker[0].Remarks; NewStock.Ticker[0].Quantity = Ticker[0].Quantity; NewStock.Ticker[0].Price = Ticker[0].Price;
                NewStock.Ticker[1].Time = Ticker[1].Time; NewStock.Ticker[1].Remarks = Ticker[1].Remarks; NewStock.Ticker[1].Quantity = Ticker[1].Quantity; NewStock.Ticker[1].Price = Ticker[1].Price;
                NewStock.Ticker[2].Time = Ticker[2].Time; NewStock.Ticker[2].Remarks = Ticker[2].Remarks; NewStock.Ticker[2].Quantity = Ticker[2].Quantity; NewStock.Ticker[2].Price = Ticker[2].Price;
                NewStock.Ticker[3].Time = Ticker[3].Time; NewStock.Ticker[3].Remarks = Ticker[3].Remarks; NewStock.Ticker[3].Quantity = Ticker[3].Quantity; NewStock.Ticker[3].Price = Ticker[3].Price;
                NewStock.Ticker[4].Time = Ticker[4].Time; NewStock.Ticker[4].Remarks = Ticker[4].Remarks; NewStock.Ticker[4].Quantity = Ticker[4].Quantity; NewStock.Ticker[4].Price = Ticker[4].Price;
                NewStock.Ticker[5].Time = Ticker[5].Time; NewStock.Ticker[5].Remarks = Ticker[5].Remarks; NewStock.Ticker[5].Quantity = Ticker[5].Quantity; NewStock.Ticker[5].Price = Ticker[5].Price;
                NewStock.Ticker[6].Time = Ticker[6].Time; NewStock.Ticker[6].Remarks = Ticker[6].Remarks; NewStock.Ticker[6].Quantity = Ticker[6].Quantity; NewStock.Ticker[6].Price = Ticker[6].Price;
                NewStock.Ticker[7].Time = Ticker[7].Time; NewStock.Ticker[7].Remarks = Ticker[7].Remarks; NewStock.Ticker[7].Quantity = Ticker[7].Quantity; NewStock.Ticker[7].Price = Ticker[7].Price;
                NewStock.Ticker[8].Time = Ticker[8].Time; NewStock.Ticker[8].Remarks = Ticker[8].Remarks; NewStock.Ticker[8].Quantity = Ticker[8].Quantity; NewStock.Ticker[8].Price = Ticker[8].Price;
                NewStock.Ticker[9].Time = Ticker[9].Time; NewStock.Ticker[9].Remarks = Ticker[9].Remarks; NewStock.Ticker[9].Quantity = Ticker[9].Quantity; NewStock.Ticker[9].Price = Ticker[9].Price;
                NewStock.Ticker[10].Time = Ticker[10].Time; NewStock.Ticker[10].Remarks = Ticker[10].Remarks; NewStock.Ticker[10].Quantity = Ticker[10].Quantity; NewStock.Ticker[10].Price = Ticker[10].Price;
                NewStock.Ticker[11].Time = Ticker[11].Time; NewStock.Ticker[11].Remarks = Ticker[11].Remarks; NewStock.Ticker[11].Quantity = Ticker[11].Quantity; NewStock.Ticker[11].Price = Ticker[11].Price;
                NewStock.Ticker[12].Time = Ticker[12].Time; NewStock.Ticker[12].Remarks = Ticker[12].Remarks; NewStock.Ticker[12].Quantity = Ticker[12].Quantity; NewStock.Ticker[12].Price = Ticker[12].Price;
                NewStock.Ticker[13].Time = Ticker[13].Time; NewStock.Ticker[13].Remarks = Ticker[13].Remarks; NewStock.Ticker[13].Quantity = Ticker[13].Quantity; NewStock.Ticker[13].Price = Ticker[13].Price;
                NewStock.Ticker[14].Time = Ticker[14].Time; NewStock.Ticker[14].Remarks = Ticker[14].Remarks; NewStock.Ticker[14].Quantity = Ticker[14].Quantity; NewStock.Ticker[14].Price = Ticker[14].Price;

                NewStock.BrokerBid[0] = BrokerBid[0]; NewStock.BrokerBid[1] = BrokerBid[1]; NewStock.BrokerBid[2] = BrokerBid[2]; NewStock.BrokerBid[3] = BrokerBid[3]; NewStock.BrokerBid[4] = BrokerBid[4];
                NewStock.BrokerBid[5] = BrokerBid[5]; NewStock.BrokerBid[6] = BrokerBid[6]; NewStock.BrokerBid[7] = BrokerBid[7]; NewStock.BrokerBid[8] = BrokerBid[8]; NewStock.BrokerBid[9] = BrokerBid[9];
                NewStock.BrokerBid[10] = BrokerBid[10]; NewStock.BrokerBid[11] = BrokerBid[11]; NewStock.BrokerBid[12] = BrokerBid[12]; NewStock.BrokerBid[13] = BrokerBid[13]; NewStock.BrokerBid[14] = BrokerBid[14];
                NewStock.BrokerBid[15] = BrokerBid[15]; NewStock.BrokerBid[16] = BrokerBid[16]; NewStock.BrokerBid[17] = BrokerBid[17]; NewStock.BrokerBid[18] = BrokerBid[18]; NewStock.BrokerBid[19] = BrokerBid[19];
                NewStock.BrokerBid[20] = BrokerBid[20]; NewStock.BrokerBid[21] = BrokerBid[21]; NewStock.BrokerBid[22] = BrokerBid[22]; NewStock.BrokerBid[23] = BrokerBid[23]; NewStock.BrokerBid[24] = BrokerBid[24];
                NewStock.BrokerBid[25] = BrokerBid[25]; NewStock.BrokerBid[26] = BrokerBid[26]; NewStock.BrokerBid[27] = BrokerBid[27]; NewStock.BrokerBid[28] = BrokerBid[28]; NewStock.BrokerBid[29] = BrokerBid[29];
                NewStock.BrokerBid[30] = BrokerBid[30]; NewStock.BrokerBid[31] = BrokerBid[31]; NewStock.BrokerBid[32] = BrokerBid[32]; NewStock.BrokerBid[33] = BrokerBid[33]; NewStock.BrokerBid[34] = BrokerBid[34];
                NewStock.BrokerBid[35] = BrokerBid[35]; NewStock.BrokerBid[36] = BrokerBid[36]; NewStock.BrokerBid[37] = BrokerBid[37]; NewStock.BrokerBid[38] = BrokerBid[38]; NewStock.BrokerBid[39] = BrokerBid[39];

                NewStock.BrokerAsk[0] = BrokerAsk[0]; NewStock.BrokerAsk[1] = BrokerAsk[1]; NewStock.BrokerAsk[2] = BrokerAsk[2]; NewStock.BrokerAsk[3] = BrokerAsk[3]; NewStock.BrokerAsk[4] = BrokerAsk[4];
                NewStock.BrokerAsk[5] = BrokerAsk[5]; NewStock.BrokerAsk[6] = BrokerAsk[6]; NewStock.BrokerAsk[7] = BrokerAsk[7]; NewStock.BrokerAsk[8] = BrokerAsk[8]; NewStock.BrokerAsk[9] = BrokerAsk[9];
                NewStock.BrokerAsk[10] = BrokerAsk[10]; NewStock.BrokerAsk[11] = BrokerAsk[11]; NewStock.BrokerAsk[12] = BrokerAsk[12]; NewStock.BrokerAsk[13] = BrokerAsk[13]; NewStock.BrokerAsk[14] = BrokerAsk[14];
                NewStock.BrokerAsk[15] = BrokerAsk[15]; NewStock.BrokerAsk[16] = BrokerAsk[16]; NewStock.BrokerAsk[17] = BrokerAsk[17]; NewStock.BrokerAsk[18] = BrokerAsk[18]; NewStock.BrokerAsk[19] = BrokerAsk[19];
                NewStock.BrokerAsk[20] = BrokerAsk[20]; NewStock.BrokerAsk[21] = BrokerAsk[21]; NewStock.BrokerAsk[22] = BrokerAsk[22]; NewStock.BrokerAsk[23] = BrokerAsk[23]; NewStock.BrokerAsk[24] = BrokerAsk[24];
                NewStock.BrokerAsk[25] = BrokerAsk[25]; NewStock.BrokerAsk[26] = BrokerAsk[26]; NewStock.BrokerAsk[27] = BrokerAsk[27]; NewStock.BrokerAsk[28] = BrokerAsk[28]; NewStock.BrokerAsk[29] = BrokerAsk[29];
                NewStock.BrokerAsk[30] = BrokerAsk[30]; NewStock.BrokerAsk[31] = BrokerAsk[31]; NewStock.BrokerAsk[32] = BrokerAsk[32]; NewStock.BrokerAsk[33] = BrokerAsk[33]; NewStock.BrokerAsk[34] = BrokerAsk[34];
                NewStock.BrokerAsk[35] = BrokerAsk[35]; NewStock.BrokerAsk[36] = BrokerAsk[36]; NewStock.BrokerAsk[37] = BrokerAsk[37]; NewStock.BrokerAsk[38] = BrokerAsk[38]; NewStock.BrokerAsk[39] = BrokerAsk[39];

                NewStock.VCMCoolOffStartTime = VCMCoolOffStartTime; NewStock.VCMCoolOffEndTime = VCMCoolOffEndTime; NewStock.dtVCMCoolOffEndTime = dtVCMCoolOffEndTime;
                NewStock.VCMReferencePrice = VCMReferencePrice; NewStock.VCMLowerPrice = VCMLowerPrice; NewStock.VCMUpperPrice = VCMUpperPrice;
                NewStock.CASReferencePrice = CASReferencePrice; NewStock.CASLowerPrice = CASLowerPrice; NewStock.CASUpperPrice = CASUpperPrice;
                NewStock.OrderImbalanceDirection = OrderImbalanceDirection; NewStock.OrderImbalanceQuantity = OrderImbalanceQuantity;
            }

            return significantChanged;
        }

        public void UpdateDynamicVersion()
        {
            pDynamicVersion += 2;
            pDynamicVersion |= 1;
        }

        public void UpdateDynamic(Stock NewStock)
        {
            if (NewStock != null)
            {
                pDynamicVersion += 2;
                pDynamicVersion |= 1;

                NewStock.pStaticVersion = pStaticVersion;
                NewStock.pDynamicVersion = pDynamicVersion;

                Nominal = NewStock.Nominal; Bid = NewStock.Bid; Ask = NewStock.Ask;

                if (PrevClose > 0 && Nominal > 0)
                {
                    NewStock.pChange = pChange = Nominal - PrevClose;
                    NewStock.pChangePC = pChangePC = Math.Round(pChange * 100 / PrevClose, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    NewStock.pChange = pChange = 0;
                    NewStock.pChangePC = pChangePC = 0;
                }

                Buffer.BlockCopy(NewStock.BidCount, 0, BidCount, 0, BidCount.Length * sizeof(int));
                Buffer.BlockCopy(NewStock.AskCount, 0, AskCount, 0, AskCount.Length * sizeof(int));
                Buffer.BlockCopy(NewStock.BidVol, 0, BidVol, 0, BidVol.Length * sizeof(ulong));
                Buffer.BlockCopy(NewStock.AskVol, 0, AskVol, 0, AskVol.Length * sizeof(ulong));

                Volume = NewStock.Volume; Turnover = NewStock.Turnover; High = NewStock.High; Low = NewStock.Low;
                InstrumentType = NewStock.InstrumentType;

                Ticker[0].Time = NewStock.Ticker[0].Time; Ticker[0].Remarks = NewStock.Ticker[0].Remarks; Ticker[0].Quantity = NewStock.Ticker[0].Quantity; Ticker[0].Price = NewStock.Ticker[0].Price;
                Ticker[1].Time = NewStock.Ticker[1].Time; Ticker[1].Remarks = NewStock.Ticker[1].Remarks; Ticker[1].Quantity = NewStock.Ticker[1].Quantity; Ticker[1].Price = NewStock.Ticker[1].Price;
                Ticker[2].Time = NewStock.Ticker[2].Time; Ticker[2].Remarks = NewStock.Ticker[2].Remarks; Ticker[2].Quantity = NewStock.Ticker[2].Quantity; Ticker[2].Price = NewStock.Ticker[2].Price;
                Ticker[3].Time = NewStock.Ticker[3].Time; Ticker[3].Remarks = NewStock.Ticker[3].Remarks; Ticker[3].Quantity = NewStock.Ticker[3].Quantity; Ticker[3].Price = NewStock.Ticker[3].Price;
                Ticker[4].Time = NewStock.Ticker[4].Time; Ticker[4].Remarks = NewStock.Ticker[4].Remarks; Ticker[4].Quantity = NewStock.Ticker[4].Quantity; Ticker[4].Price = NewStock.Ticker[4].Price;
                Ticker[5].Time = NewStock.Ticker[5].Time; Ticker[5].Remarks = NewStock.Ticker[5].Remarks; Ticker[5].Quantity = NewStock.Ticker[5].Quantity; Ticker[5].Price = NewStock.Ticker[5].Price;
                Ticker[6].Time = NewStock.Ticker[6].Time; Ticker[6].Remarks = NewStock.Ticker[6].Remarks; Ticker[6].Quantity = NewStock.Ticker[6].Quantity; Ticker[6].Price = NewStock.Ticker[6].Price;
                Ticker[7].Time = NewStock.Ticker[7].Time; Ticker[7].Remarks = NewStock.Ticker[7].Remarks; Ticker[7].Quantity = NewStock.Ticker[7].Quantity; Ticker[7].Price = NewStock.Ticker[7].Price;
                Ticker[8].Time = NewStock.Ticker[8].Time; Ticker[8].Remarks = NewStock.Ticker[8].Remarks; Ticker[8].Quantity = NewStock.Ticker[8].Quantity; Ticker[8].Price = NewStock.Ticker[8].Price;
                Ticker[9].Time = NewStock.Ticker[9].Time; Ticker[9].Remarks = NewStock.Ticker[9].Remarks; Ticker[9].Quantity = NewStock.Ticker[9].Quantity; Ticker[9].Price = NewStock.Ticker[9].Price;
                Ticker[10].Time = NewStock.Ticker[10].Time; Ticker[10].Remarks = NewStock.Ticker[10].Remarks; Ticker[10].Quantity = NewStock.Ticker[10].Quantity; Ticker[10].Price = NewStock.Ticker[10].Price;
                Ticker[11].Time = NewStock.Ticker[11].Time; Ticker[11].Remarks = NewStock.Ticker[11].Remarks; Ticker[11].Quantity = NewStock.Ticker[11].Quantity; Ticker[11].Price = NewStock.Ticker[11].Price;
                Ticker[12].Time = NewStock.Ticker[12].Time; Ticker[12].Remarks = NewStock.Ticker[12].Remarks; Ticker[12].Quantity = NewStock.Ticker[12].Quantity; Ticker[12].Price = NewStock.Ticker[12].Price;
                Ticker[13].Time = NewStock.Ticker[13].Time; Ticker[13].Remarks = NewStock.Ticker[13].Remarks; Ticker[13].Quantity = NewStock.Ticker[13].Quantity; Ticker[13].Price = NewStock.Ticker[13].Price;
                Ticker[14].Time = NewStock.Ticker[14].Time; Ticker[14].Remarks = NewStock.Ticker[14].Remarks; Ticker[14].Quantity = NewStock.Ticker[14].Quantity; Ticker[14].Price = NewStock.Ticker[14].Price;

                BrokerBid[0] = NewStock.BrokerBid[0]; BrokerBid[1] = NewStock.BrokerBid[1]; BrokerBid[2] = NewStock.BrokerBid[2]; BrokerBid[3] = NewStock.BrokerBid[3]; BrokerBid[4] = NewStock.BrokerBid[4];
                BrokerBid[5] = NewStock.BrokerBid[5]; BrokerBid[6] = NewStock.BrokerBid[6]; BrokerBid[7] = NewStock.BrokerBid[7]; BrokerBid[8] = NewStock.BrokerBid[8]; BrokerBid[9] = NewStock.BrokerBid[9];
                BrokerBid[10] = NewStock.BrokerBid[10]; BrokerBid[11] = NewStock.BrokerBid[11]; BrokerBid[12] = NewStock.BrokerBid[12]; BrokerBid[13] = NewStock.BrokerBid[13]; BrokerBid[14] = NewStock.BrokerBid[14];
                BrokerBid[15] = NewStock.BrokerBid[15]; BrokerBid[16] = NewStock.BrokerBid[16]; BrokerBid[17] = NewStock.BrokerBid[17]; BrokerBid[18] = NewStock.BrokerBid[18]; BrokerBid[19] = NewStock.BrokerBid[19];
                BrokerBid[20] = NewStock.BrokerBid[20]; BrokerBid[21] = NewStock.BrokerBid[21]; BrokerBid[22] = NewStock.BrokerBid[22]; BrokerBid[23] = NewStock.BrokerBid[23]; BrokerBid[24] = NewStock.BrokerBid[24];
                BrokerBid[25] = NewStock.BrokerBid[25]; BrokerBid[26] = NewStock.BrokerBid[26]; BrokerBid[27] = NewStock.BrokerBid[27]; BrokerBid[28] = NewStock.BrokerBid[28]; BrokerBid[29] = NewStock.BrokerBid[29];
                BrokerBid[30] = NewStock.BrokerBid[30]; BrokerBid[31] = NewStock.BrokerBid[31]; BrokerBid[32] = NewStock.BrokerBid[32]; BrokerBid[33] = NewStock.BrokerBid[33]; BrokerBid[34] = NewStock.BrokerBid[34];
                BrokerBid[35] = NewStock.BrokerBid[35]; BrokerBid[36] = NewStock.BrokerBid[36]; BrokerBid[37] = NewStock.BrokerBid[37]; BrokerBid[38] = NewStock.BrokerBid[38]; BrokerBid[39] = NewStock.BrokerBid[39];

                BrokerAsk[0] = NewStock.BrokerAsk[0]; BrokerAsk[1] = NewStock.BrokerAsk[1]; BrokerAsk[2] = NewStock.BrokerAsk[2]; BrokerAsk[3] = NewStock.BrokerAsk[3]; BrokerAsk[4] = NewStock.BrokerAsk[4];
                BrokerAsk[5] = NewStock.BrokerAsk[5]; BrokerAsk[6] = NewStock.BrokerAsk[6]; BrokerAsk[7] = NewStock.BrokerAsk[7]; BrokerAsk[8] = NewStock.BrokerAsk[8]; BrokerAsk[9] = NewStock.BrokerAsk[9];
                BrokerAsk[10] = NewStock.BrokerAsk[10]; BrokerAsk[11] = NewStock.BrokerAsk[11]; BrokerAsk[12] = NewStock.BrokerAsk[12]; BrokerAsk[13] = NewStock.BrokerAsk[13]; BrokerAsk[14] = NewStock.BrokerAsk[14];
                BrokerAsk[15] = NewStock.BrokerAsk[15]; BrokerAsk[16] = NewStock.BrokerAsk[16]; BrokerAsk[17] = NewStock.BrokerAsk[17]; BrokerAsk[18] = NewStock.BrokerAsk[18]; BrokerAsk[19] = NewStock.BrokerAsk[19];
                BrokerAsk[20] = NewStock.BrokerAsk[20]; BrokerAsk[21] = NewStock.BrokerAsk[21]; BrokerAsk[22] = NewStock.BrokerAsk[22]; BrokerAsk[23] = NewStock.BrokerAsk[23]; BrokerAsk[24] = NewStock.BrokerAsk[24];
                BrokerAsk[25] = NewStock.BrokerAsk[25]; BrokerAsk[26] = NewStock.BrokerAsk[26]; BrokerAsk[27] = NewStock.BrokerAsk[27]; BrokerAsk[28] = NewStock.BrokerAsk[28]; BrokerAsk[29] = NewStock.BrokerAsk[29];
                BrokerAsk[30] = NewStock.BrokerAsk[30]; BrokerAsk[31] = NewStock.BrokerAsk[31]; BrokerAsk[32] = NewStock.BrokerAsk[32]; BrokerAsk[33] = NewStock.BrokerAsk[33]; BrokerAsk[34] = NewStock.BrokerAsk[34];
                BrokerAsk[35] = NewStock.BrokerAsk[35]; BrokerAsk[36] = NewStock.BrokerAsk[36]; BrokerAsk[37] = NewStock.BrokerAsk[37]; BrokerAsk[38] = NewStock.BrokerAsk[38]; BrokerAsk[39] = NewStock.BrokerAsk[39];

                VCMCoolOffStartTime = NewStock.VCMCoolOffStartTime; VCMCoolOffEndTime = NewStock.VCMCoolOffEndTime; dtVCMCoolOffEndTime = NewStock.dtVCMCoolOffEndTime;
                VCMReferencePrice = NewStock.VCMReferencePrice; VCMLowerPrice = NewStock.VCMLowerPrice; VCMUpperPrice = NewStock.VCMUpperPrice;
                CASReferencePrice = NewStock.CASReferencePrice; CASLowerPrice = NewStock.CASLowerPrice; CASUpperPrice = NewStock.CASUpperPrice;
                OrderImbalanceDirection = NewStock.OrderImbalanceDirection; OrderImbalanceQuantity = NewStock.OrderImbalanceQuantity; 

                NewStock.NameEN = NameEN;
                NewStock.NameENShort = NameENShort;
                NewStock.NameCHT = NameCHT;
                NewStock.NameCHS = NameCHS;
                NewStock.LotSize = LotSize;
                NewStock.Open = Open;
                NewStock.PrevClose = PrevClose;
                NewStock.Currency = Currency;
                NewStock.InstrumentType = InstrumentType;
                NewStock.MarketBelong = MarketBelong;
                NewStock.SpreadTableCode = SpreadTableCode;
                NewStock.SuspensionFlag = SuspensionFlag;
                NewStock.FusingFlag = FusingFlag;
                NewStock.ProductType = ProductType;
                NewStock.MarketCode = MarketCode;
            }
        }

        public Stock Clone()
        {
            int i, n;
            Stock clone = new Stock(ExchangeType, Code);

            clone.pStaticVersion = pStaticVersion;
            clone.pDynamicVersion = pDynamicVersion;

            clone.NameEN = NameEN;

            //clone.NameCHTChars = (byte[])NameCHTChars.Clone();
            clone.NameCHT = NameCHT;

            //clone.NameCHSChars = (byte[])NameCHSChars.Clone();
            clone.NameCHS = NameCHS;

            clone.NameENShort = NameENShort;
            clone.Open = Open;
            clone.High = High;
            clone.Low = Low;
            clone.PrevClose = PrevClose;
            clone.Nominal = Nominal;
            clone.Volume = Volume;
            clone.Turnover = Turnover;
            clone.LotSize = LotSize;
            clone.Currency = Currency;
            clone.InstrumentType = InstrumentType;
            clone.SpreadTableCode = SpreadTableCode;
            clone.SuspensionFlag = SuspensionFlag;
            clone.FusingFlag = FusingFlag;
            clone.MarketBelong = MarketBelong;
            clone.Bid = Bid;
            clone.Ask = Ask;

            for (i = 0, n = BidVol.Length; i < n; i++) clone.BidVol[i] = BidVol[i];
            for (i = 0, n = AskVol.Length; i < n; i++) clone.AskVol[i] = AskVol[i];
            for (i = 0, n = BidCount.Length; i < n; i++) clone.BidCount[i] = BidCount[i];
            for (i = 0, n = AskCount.Length; i < n; i++) clone.AskCount[i] = AskCount[i];
            for (i = 0, n = Ticker.Length; i < n; i++) if (Ticker[i] != null) Ticker[i].CopyTo(clone.Ticker[i]);
            for (i = 0, n = BrokerBid.Length; i < n; i++) clone.BrokerBid[i] = BrokerBid[i];
            for (i = 0, n = BrokerAsk.Length; i < n; i++) clone.BrokerAsk[i] = BrokerAsk[i];

            clone.VCMReferencePrice = VCMReferencePrice;
            clone.VCMLowerPrice = VCMLowerPrice;
            clone.VCMUpperPrice = VCMUpperPrice;
            clone.CASReferencePrice = CASReferencePrice;
            clone.CASLowerPrice = CASLowerPrice;
            clone.CASUpperPrice = CASUpperPrice;
            clone.VCMCoolOffStartTime = VCMCoolOffStartTime;
            clone.VCMCoolOffEndTime = VCMCoolOffEndTime;
            clone.dtVCMCoolOffEndTime = dtVCMCoolOffEndTime;
            clone.OrderImbalanceDirection = OrderImbalanceDirection;
            clone.OrderImbalanceQuantity = OrderImbalanceQuantity;
            clone.ProductType = ProductType;
            clone.MarketCode = MarketCode;

            return clone;
        }
    }

    public class StockTicker : ICloneable
    {
        public string Time;
        public string Remarks;
        public int Quantity;
        public decimal Price;

        public object Clone()
        {
            StockTicker clone = new StockTicker();

            clone.Time = Time;
            clone.Remarks = Remarks;
            clone.Quantity = Quantity;
            clone.Price = Price;

            return clone;
        }

        public void CopyTo(StockTicker Ticker)
        {
            if (Ticker != null)
            {
                Ticker.Time = Time;
                Ticker.Remarks = Remarks;
                Ticker.Quantity = Quantity;
                Ticker.Price = Price;
            }
        }
    }
}
