using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Utils
{
    public class AutoResetSpinEvent
    {
        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long perfcount);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long freq);

        private static readonly bool HighRes;
        private static readonly long PerfFrequency;
        private static readonly double PerfFrequency01m;

        private readonly AutoResetEvent pEvent;

        static AutoResetSpinEvent()
        {
            HighRes = QueryPerformanceFrequency(out PerfFrequency) && PerfFrequency != 1000;
            PerfFrequency01m = (double)PerfFrequency * 0.0000001D;
        }

        public AutoResetSpinEvent(bool initialState)
        {
            pEvent = new AutoResetEvent(initialState);
        }

        public void Set()
        {
            pEvent.Set();
        }

        public void Reset()
        {
            pEvent.Reset();
        }

        /// <summary>
        /// Blocks the current thread until the current WaitHandle receives a signal, using 64-bit signed integer to specify the time interval.
        /// </summary>
        /// <param name="timeout">The number of ticks to wait, or Timeout.Infinite (-1) to wait indefinitely.</param>
        /// <param name="spin">true use spin wait, otherwise use non-spin wait.</param>
        /// <returns></returns>
        public bool WaitOne(long timeout, bool spin)
        {
            if (spin && HighRes)
            {
                if (timeout >= 0)
                {
                    long pcBegin, pcEnd, pcNow;

                    QueryPerformanceCounter(out pcBegin);
                    pcEnd = pcBegin + (long)(timeout * PerfFrequency01m);   // the time unit of timeout is tick when spinning

                    do
                    {
                        if (pEvent.WaitOne(0, false))
                            return true;

                        QueryPerformanceCounter(out pcNow);

                    } while (pcNow < pcEnd);

                    return false;
                }
                else
                {
                    while (!pEvent.WaitOne(0, false));

                    return true;
                }
            }
            else
            {
                if (timeout >= 0)
                    return pEvent.WaitOne((int)Math.Round(timeout * 0.0001D, MidpointRounding.AwayFromZero), true);   // the time unit of timeout is millisecond when not spinning
                else
                    return pEvent.WaitOne(Timeout.Infinite, true);
            }
        }
    }
}
