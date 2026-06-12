using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class EnvironmentTickCounter
    {
        private int pValue;
        private int pTimesUp;

        /// <summary>
        /// Construct a EnvironmentTickCounter object with TimesUp default to zero meaning it can never times up.
        /// </summary>
        public EnvironmentTickCounter()
        {
            pTimesUp = 0;
            Set();
        }

        /// <summary>
        /// Construct a EnvironmentTickCounter object with specific TimesUp value.
        /// </summary>
        /// <param name="TimesUp">The tick count number to determine if times up.</param>
        public EnvironmentTickCounter(int TimesUp)
        {
            pTimesUp = TimesUp;
            Set();
        }

        /// <summary>
        /// Construct a EnvironmentTickCounter object with specific TimesUp value..
        /// </summary>
        /// <param name="TimesUp">The tick count number to determine if times up.</param>
        /// <param name="ImmediateTimesUp">if true the counter value will be set so that it times up immediately.</param>
        public EnvironmentTickCounter(int TimesUp, bool ImmediateTimesUp)
        {
            pTimesUp = TimesUp;
            Set(ImmediateTimesUp);
        }

        /// <summary>
        /// Construct a EnvironmentTickCounter object with specific Offset to current ProcessTimer.TickCount value and TimesUp value.
        /// </summary>
        /// <param name="TimesUp">The tick count number to determine if times up.</param>
        /// <param name="Offset">The offset to the current ProcessTimer.TickCount value.</param>
        public EnvironmentTickCounter(int TimesUp, int Offset)
        {
            pTimesUp = TimesUp;
            Set(Offset);
        }

        /// <summary>
        /// Set the counter value to current ProcessTimer.TickCount value.
        /// </summary>
        public void Set()
        {
            pValue = ProcessTimer.TickCount;
        }

        /// <summary>
        /// Set the counter value in a manner determined by the ImmediateTimesUp parameter.
        /// </summary>
        /// <param name="ImmediateTimesUp">if true the counter value will be set so that it times up immediately, otherwise the current value of ProcessTimer.TickCount.</param>
        public void Set(bool ImmediateTimesUp)
        {
            pValue = ProcessTimer.TickCount + (ImmediateTimesUp ? -(pTimesUp + 1) : 0);
        }

        /// <summary>
        /// Set the counter value to current ProcessTimer.TickCount value + Offset.
        /// </summary>
        /// <param name="Offset">The offset to the current ProcessTimer.TickCount value.</param>
        public void Set(int Offset)
        {
            pValue = ProcessTimer.TickCount + Offset;
        }

        /// <summary>
        /// The tick count number to determine if times up. Default is zero which means it can never times up.
        /// </summary>
        public int TimesUp
        {
            get { return pTimesUp; }
            set { pTimesUp = value; }
        }

        /// <summary>
        /// The elapsed time since last Set() member was called, or IsTimesUp property was called and returned true.
        /// </summary>
        public int Elapsed
        {
            get
            {
                int elapsed = ProcessTimer.TickCount - pValue;
                return (elapsed >= 0 ? elapsed : int.MaxValue);
            }
        }

        /// <summary>
        /// The remain time before times up. If times up is zero, return -1.
        /// </summary>
        public int Remain
        {
            get
            {
                if (pTimesUp <= 0) return -1;

                int remain = pTimesUp - Elapsed;
                return (remain >= 0 ? remain : 0);
            }
        }

        /// <summary>
        /// true if elapsed time >= TimesUp and TimesUp > 0, otherwise false. If true, counter will be updated to current tick count.
        /// </summary>
        public bool IsTimesUp
        {
            get
            {
                if (pTimesUp > 0)
                {
                    int currentTick = ProcessTimer.TickCount;
                    int elapsed = currentTick - pValue;

                    if (elapsed >= pTimesUp || elapsed < 0)
                    {
                        pValue = currentTick;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
