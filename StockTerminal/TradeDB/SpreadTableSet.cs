using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class SpreadTableSet
    {
        public class SpreadTable
        {
            private class Range
            {
                public const int Order = 3;
                public const int Scale = 1000;
                public const decimal InvScale = 0.001m;
                public const string ScaleFormat = "000";

                public readonly int Lower;
                public readonly int Upper;
                public readonly int Delta;
                public readonly int Step;
                private readonly string DecimalFormat;

                public Range(int Lower, int Upper, int Delta)
                {
                    this.Lower = Lower;
                    this.Upper = Upper;
                    this.Delta = Delta;

                    Step = Delta != 0 ? (Upper - Lower) / Delta : 1;

                    string ds = Delta.ToString(ScaleFormat);
                    DecimalFormat = "{0:" + ("0." + new string('0', ds.Substring(ds.Length - Order).TrimEnd('0').Length)).TrimEnd('.') + "}";
                }

                public int GetSpread(int Index)
                {
                    return (Index > 0 ? Lower : Upper) + Index * Delta;
                }

                public int GetSpread(int Price, ref int Index)
                {
                    int newPrice = Price + Index * Delta;

                    if (Index > 0)
                    {
                        if (newPrice <= Upper)
                        {
                            Index = 0;
                            return newPrice;
                        }

                        Index -= ((Upper - Price) / Delta);
                        return 0;
                    }
                    else
                    {
                        if (newPrice >= Lower)
                        {
                            Index = 0;
                            return newPrice;
                        }

                        Index += ((Price - Lower) / Delta);
                        return 0;
                    }
                }

                public string Format(decimal Price)
                {
                    return string.Format(DecimalFormat, Price);
                }
            }

            public readonly int Code;
            private readonly SortedList<int, Range> RawRanges = new SortedList<int, Range>(20);
            private readonly List<int> Lower = new List<int>(20);
            private readonly List<Range> Ranges = new List<Range>(20);
            private int MinPriceInt = 0;
            private int MaxPriceInt = 0;
            private decimal MinPriceDec = 0;
            private decimal MaxPriceDec = 0;

            public SpreadTable(int Code)
            {
                this.Code = Code;
            }

            public void AddRange(int Lower, int Upper, int Delta)
            {
                if (Lower <= 0 || Upper <= Lower || Delta <= 0) return;

                try
                {
                    RawRanges.Add(Lower, new Range(Lower, Upper, Delta));
                }
                catch { }

                Compile();
            }

            public int GetDelta(int Price)
            {
                if (Price <= 0) return 0;

                int rngIdx = Lower.BinarySearch(Price);

                if (rngIdx < 0)
                    rngIdx = ~rngIdx - 1;
                else if (rngIdx >= Lower.Count)
                    return 0;

                return Ranges[rngIdx].Delta;
            }

            public decimal GetSpread(decimal Price, int Index, out int ResultIndex)
            {
                Range rng;
                int newPrice, orgPrice;
                ResultIndex = 0;

                if (Price == MinPriceDec && Index <= 0)
                    return MinPriceDec;
                else if (Price == MaxPriceDec && Index >= 0)
                    return MaxPriceDec;
                else if (Price < MinPriceDec || Price > MaxPriceDec)
                    return 0m;

                orgPrice = (int)(Price * Range.Scale);

                int rngIdx = Lower.BinarySearch(orgPrice);

                if (rngIdx < 0 || rngIdx == Lower.Count)
                    rngIdx = ~rngIdx - 1;

                ResultIndex = Index;

                if (Index > 0)
                {
                    rng = Ranges[rngIdx];
                    newPrice = rng.GetSpread(orgPrice, ref Index);

                    if (newPrice > 0)
                        return newPrice * Range.InvScale;

                    for (rngIdx++; rngIdx < Ranges.Count; rngIdx++)
                    {
                        rng = Ranges[rngIdx];

                        if (Index > rng.Step)
                            Index -= rng.Step;
                        else if (Index == rng.Step)
                            return rng.Upper * Range.InvScale;
                        else
                            return rng.GetSpread(Index) * Range.InvScale;
                    }

                    ResultIndex -= Index;
                    return rng.Upper * Range.InvScale;
                }
                else
                {
                    rng = Ranges[rngIdx];
                    newPrice = rng.GetSpread(orgPrice, ref Index);

                    if (newPrice > 0)
                        return newPrice * Range.InvScale;

                    for (rngIdx--; rngIdx >= 0; rngIdx--)
                    {
                        rng = Ranges[rngIdx];

                        if (Index > rng.Step)
                            Index += rng.Step;
                        else if (Index == rng.Step)
                            return rng.Lower * Range.InvScale;
                        else
                            return rng.GetSpread(Index) * Range.InvScale;
                    }

                    ResultIndex -= Index;
                    return rng.Lower * Range.InvScale;
                }
            }

            public string Format(decimal Price)
            {
                if (Price <= 0) return "";

                int rngIdx = Lower.BinarySearch((int)(Price * Range.Scale));

                if (rngIdx < -1)
                    rngIdx = ~rngIdx - 1;
                else if (rngIdx == -1 || rngIdx >= Lower.Count)
                    return Price.ToString("G0");

                return Ranges[rngIdx].Format(Price);
            }

            private void Compile()
            {
                Lower.Clear();
                Ranges.Clear();

                MinPriceInt = 0;
                MaxPriceInt = 0;
                MinPriceDec = 0;
                MaxPriceDec = 0;

                foreach (KeyValuePair<int, Range> kvp in RawRanges)
                {
                    if (kvp.Key > 0 && kvp.Value != null)
                    {
                        Lower.Add(kvp.Key);
                        Ranges.Add(kvp.Value);
                    }
                }

                if (Ranges.Count > 0)
                {
                    MinPriceInt = Ranges[0].Lower;
                    MaxPriceInt = Ranges[Ranges.Count - 1].Upper;
                    MinPriceDec = MinPriceInt * Range.InvScale;
                    MaxPriceDec = MaxPriceInt * Range.InvScale;
                }
            }

            /*
            public int GetSpread(int Price, int Index)
            {
                if (Ranges.Count <= 0 || Index == 0 || Price <= 0) return 0;

                int n = Ranges.Count;
                int a = 0;
                int b = n - 1;
                int i = -1;
                int result;
                int pPrice = 0;

                if (Price < Ranges[a].Lower || Price > Ranges[b].Upper) return 0;

                result = Ranges[a].InRange(Price);
                if (result < 0)
                    return 0;
                else if (result == 0)
                    i = a;
                else
                {
                    result = Ranges[b].InRange(Price);
                    if (result > 0)
                        return 0;
                    else if (result == 0)
                        i = b;
                    else
                    {
                        while ((b - a) > 1)
                        {
                            i = ((a + b) >> 1);
                            result = Ranges[i].InRange(Price);
                            switch (result)
                            {
                                case -2:    // Invalid Price, shouldn't happen
                                    return 0;

                                case -1:    // Lower than range
                                    b = i;
                                    break;

                                case 0:     // In range
                                    a = i;
                                    b = i;
                                    break;

                                case 1:     // Higher than range
                                    a = i;
                                    break;
                            }
                        }
                    }
                }

                if (i >= 0 && i < n)
                {
                    pPrice = Price;
                    if (Index > 0)
                    {
                        while (i < n)
                        {
                            result = Ranges[i].GetSpread(pPrice, ref Index);
                            if (result > 0) return result;
                            i++;
                            if (i < n)
                            {
                                pPrice = Ranges[i].Lower;
                            }
                            else
                            {
                                return Ranges[n - 1].Upper;
                            }
                        }
                    }
                    else
                    {
                        while (i >= 0)
                        {
                            result = Ranges[i].GetSpread(pPrice, ref Index);
                            if (result > 0) return result;
                            i--;
                            if (i > 0)
                            {
                                pPrice = Ranges[i].Upper;
                            }
                            else
                            {
                                return Ranges[0].Lower;
                            }
                        }
                    }
                }
                return 0;
            }

            public int GetDelta(int Price)
            {
                if (Ranges.Count <= 0 || Price <= 0) return 0;

                int n = Ranges.Count;
                int a = 0;
                int b = n - 1;
                int i = -1;
                int result;

                if (Price < Ranges[a].Lower || Price > Ranges[b].Upper) return 0;

                result = Ranges[a].InRange(Price);

                if (result < 0)
                    return 0;
                else if (result == 0)
                    i = a;
                else
                {
                    result = Ranges[b].InRange(Price);

                    if (result > 0)
                        return 0;
                    else if (result == 0)
                        i = b;
                    else
                    {
                        while ((b - a) > 1)
                        {
                            i = ((a + b) >> 1);
                            result = Ranges[i].InRange(Price);
                            switch (result)
                            {
                                case -2:    // Invalid Price, shouldn't happen
                                    return 0;

                                case -1:    // Lower than range
                                    b = i;
                                    if ((b - a) <= 1) return 0;
                                    break;

                                case 0:     // In range
                                    a = i;
                                    b = i;
                                    break;

                                case 1:     // Higher than range
                                    a = i;
                                    if ((b - a) <= 1) return 0;
                                    break;
                            }
                        }
                    }
                }

                if (i >= 0 && i < n)
                    return Ranges[i].Delta;

                return 0;
            }
             */
        }

        private readonly Dictionary<int, SpreadTable> SpreadTableDict = new Dictionary<int,SpreadTable>(10);
        private readonly SpreadTable DefaultTable = new SpreadTable(-1);

        public SpreadTableSet()
        {
            DefaultTable.AddRange(1, 999999000, 1);
        }

        public int Count
        {
            get { return SpreadTableDict.Count; }
        }

        public void Add(SpreadTable Table)
        {
            if (Table != null)
                SpreadTableDict[Table.Code] = Table;
        }

        public void Add(int Code, int Lower, int Upper, int Delta)
        {
            SpreadTable table;

            if (!SpreadTableDict.TryGetValue(Code, out table))
                SpreadTableDict.Add(Code, table = new SpreadTable(Code));

            table.AddRange(Lower, Upper, Delta);
        }

        public SpreadTable this[int Code]
        {
            get
            {
                SpreadTable table;

                if (!SpreadTableDict.TryGetValue(Code, out table))
                    table = DefaultTable;

                return table;
            }
        }

        public void Clear()
        {
            SpreadTableDict.Clear();
        }
   }
}
