using NLog;
using System;
using System.Threading;

namespace LeagueFPSBoost.Diagnostics.Debugger
{
    public class DebugEventArgs : EventArgs
    {
        public bool Attached { get; set; }
    }

    class DebuggerWatcher
    {
        public event EventHandler<DebugEventArgs> DebuggerChanged;
        public event EventHandler<DebugEventArgs> DebuggerChecked;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public DebuggerWatcher(int threadSleepTime = 250)
        {
            new Thread(() => {
                while (true)
                {
                    var last = System.Diagnostics.Debugger.IsAttached;
                    while (last == System.Diagnostics.Debugger.IsAttached)
                    {
                        Thread.Sleep(threadSleepTime);
                    }
                    OnDebuggerChanged();
                }
            })
            { IsBackground = true }.Start();
        }

        public void CheckNow()
        {
            var _Attached = System.Diagnostics.Debugger.IsAttached;
            if (_Attached)
            {
                logger.Debug("Debugger is attached.");
            }
            else
            {
                logger.Debug("Debugger is not attached.");
            }
            DebuggerChecked?.Invoke(this, new DebugEventArgs { Attached = _Attached });
        }

        protected void OnDebuggerChanged()
        {
            var _Attached = System.Diagnostics.Debugger.IsAttached;
            if (_Attached)
            {
                logger.Debug("Debugger has been attached.");
            }
            else
            {
                logger.Debug("Debugger has been detached.");
            }
            DebuggerChanged?.Invoke(this, new DebugEventArgs { Attached = _Attached });
        }
    }
}
