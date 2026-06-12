using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class HighResolutionClock
    {
        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long perfcount);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long freq);

        private static readonly DateTime TimeBase;
        private static readonly long PerfCounterBase;
        private static readonly bool HighRes;
        private static readonly long PerfFrequency;
        private static readonly double PerfFrequencyInv1000;
        private static readonly double PerfFrequencyInv10M;

        static HighResolutionClock()
        {
            TimeBase = DateTime.Now;

            QueryPerformanceCounter(out PerfCounterBase);

            HighRes = QueryPerformanceFrequency(out PerfFrequency) && PerfFrequency != 1000;
            PerfFrequencyInv1000 = 1000D / (double)PerfFrequency;
            PerfFrequencyInv10M = 10000000D / (double)PerfFrequency;
        }

        public static bool HighResSupported
        {
            get { return HighRes; }
        }

        public static long Frequency
        {
            get { return PerfFrequency; }
        }

        public static DateTime Now
        {
            get
            {
                if (PerfFrequency == 1000)
                    return DateTime.Now;
                else
                {
                    long perfCounterNow;
                    QueryPerformanceCounter(out perfCounterNow);

                    return TimeBase.AddTicks((long)((perfCounterNow - PerfCounterBase) * PerfFrequencyInv10M));
                }
            }
        }


        /// <summary>
        /// Gets the number of milliseconds elapsed since the system started.
        /// </summary>
        public static int TickCount
        {
            get
            {
                if (PerfFrequency == 1000)
                    return ProcessTimer.TickCount;
                else
                {
                    long perfCounterNow;
                    QueryPerformanceCounter(out perfCounterNow);

                    return (int)(perfCounterNow * PerfFrequencyInv1000);
                }
            }
        }

        /// <summary>
        /// Gets the number of ticks elapsed since the system started.
        /// </summary>
        public static long Ticks
        {
            get
            {
                if (PerfFrequency == 1000)
                    return ProcessTimer.TickCount * 10000L;
                else
                {
                    long perfCounterNow;
                    QueryPerformanceCounter(out perfCounterNow);

                    return (long)(perfCounterNow * PerfFrequencyInv10M);
                }
            }
        }

        public static void SetSecondaryBoundary(long Ticks)
        {
            long perfCounterNow;
            QueryPerformanceCounter(out perfCounterNow);

            long adjustment = (TimeBase.AddTicks((long)((perfCounterNow - PerfCounterBase) * PerfFrequencyInv10M)).Ticks + Ticks) % 10000000;

            if (adjustment <= 3000000)
                TimeBase.AddTicks(-adjustment);
            else if (adjustment >= 7000000)
                TimeBase.AddTicks(10000000L - adjustment);
        }
    }
}
