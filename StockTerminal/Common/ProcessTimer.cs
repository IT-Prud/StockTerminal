using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Utils
{
    public static class ProcessTimer
    {
        private delegate long GetTimestampDelegate();
        private static readonly GetTimestampDelegate GetTimestamp;

        private const long NanoSec100 = 10000000L;
        private static readonly decimal CountToTick;

        private static readonly long ProcessStartTime;

        public static readonly long Frequency;

        static ProcessTimer()
        {
            Frequency = Stopwatch.Frequency;

            if (Frequency == NanoSec100)
            {
                CountToTick = 1.0m;
                ProcessStartTime = Stopwatch.GetTimestamp();

                GetTimestamp = delegate()
                {
                    return Stopwatch.GetTimestamp() - ProcessStartTime;
                };
            }
            else
            {
                CountToTick = ((decimal)NanoSec100) / (decimal)Frequency;
                ProcessStartTime = (long)(CountToTick * Stopwatch.GetTimestamp());

                GetTimestamp = delegate()
                {
                    return ((long)(CountToTick * Stopwatch.GetTimestamp())) - ProcessStartTime;
                };
            }
        }

        /// <summary>
        /// Returns the time elapsed since the start of the current process in millisecond (overflow after around 24.85 days)
        /// </summary>
        public static int TickCount
        {
            get
            {
                return (int)(0.0001m * GetTimestamp());
            }
        }
    }
}
