using NLog;
using System;
using System.Diagnostics;

namespace LeagueFPSBoost.Logging
{
    public static class LeagueLogger
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static bool Running;

        public static event EventHandler Enabled = delegate { };
        public static event EventHandler Disabled = delegate { };

        public static void Always(string message)
        {
            WriteEntry("ALWAYS", message);
        }

        public static void Error(string message)
        {
            WriteEntry("ERROR", message);
        }

        public static void Info(string message)
        {
            WriteEntry("INFO", message);
        }

        public static void Okay(string message)
        {
            WriteEntry("OKAY", message);
        }

        public static void Warning(string message)
        {
            WriteEntry("WARN", message);
        }

        private static void WriteEntry(string type, string message)
        {
            if (Running)
            {
                var time = ((decimal)000000.000 + (decimal)(Program.Stopwatch.ElapsedMilliseconds / 1000.000)).ToString("N3").Replace(",", "").PadLeft(10, '0');
                Trace.WriteLine($"{time}| {type.PadLeft(6)}| {message}");
            }
        }

        public static void Enable() { Running = true; Enabled?.Invoke(null, EventArgs.Empty); logger.Debug("League logger has been enabled."); }
        public static void Disable() { Running = false; Disabled?.Invoke(null, EventArgs.Empty); logger.Debug("League logger has been disabled."); }
    }
}
