using System;
using System.Collections.Generic;
using System.Text;

namespace Processors
{
    public delegate void StateChangedDelegate(int State);    // can't use enum directly because enum don't support overriding
    public enum IProcessStopStyle { NoWait, Wait, MustStopBeforeAllowStart };

    public interface IProcessor
    {
        StateChangedDelegate StateChanged { get; set; }

        int State { get; }
        bool IsStarted { get; }

        void Start();
        void Stop();
        void Stop(IProcessStopStyle StopStyle);
    }
}
