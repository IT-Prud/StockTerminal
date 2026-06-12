using System;
using System.Collections.Generic;
using System.Text;

namespace TradeDB
{
    public class Currency
    {
        private static object RatesDictMutex = new object();
        private static Dictionary<string, double> FromRates = new Dictionary<string, double>(10);
        private static Dictionary<string, double> ToRates = new Dictionary<string, double>(10);

        static Currency()
        {
            // for initializing the rates to default values
            // to be removed when server can send currency code out

            //SetRate("CNY", 1.180);
            //SetRate("SGD", 6.179);
            //SetRate("USD", 7.780);
            //SetRate("EUR", 11.030);
            //SetRate("CAD", 7.556);
            //SetRate("JPY", 0.094);
            //SetRate("GBP", 12.556);
            //SetRate("AUD", 8.032);
        }

        /// <summary>
        /// Set Rate of exchange from foreign to base
        /// </summary>
        /// <param name="Code">Currency Code</param>
        /// <param name="Rate">Rate</param>
        public static void SetRate(string Code, double Rate)
        {
            lock (RatesDictMutex)
            {
                if (Code == "RMB") Code = "CNY";

                FromRates[Code] = Rate;
                if (Rate != 0.0)
                {
                    ToRates[Code] = 1.0 / Rate;
                }
                else
                {
                    ToRates[Code] = 0.0;
                }
            }
        }

        public static Decimal Exchange(string From, string To, decimal Value)
        {
            double fromRate, toRate;

            lock (RatesDictMutex)
            {
                if (From != null && To != null &&
                    FromRates.TryGetValue(From, out fromRate) &&
                    ToRates.TryGetValue(To, out toRate))
                {
                    return (Decimal)((double)Value * fromRate * toRate);
                }
            }

            return 0;
        }

        public static Decimal ExchangeToBase(string From, decimal Value)
        {
            double rate;

            lock (RatesDictMutex)
            {
                if (From != null &&
                    FromRates.TryGetValue(From, out rate))
                {
                    return (Decimal)((double)Value * rate);
                }
            }

            return 0;
        }

        public static Decimal ExchangeFromBase(string To, decimal Value)
        {
            double rate;

            lock (RatesDictMutex)
            {
                if (To != null &&
                    ToRates.TryGetValue(To, out rate))
                {
                    return (Decimal)((double)Value * rate);
                }
            }

            return 0;
        }
    }
}
