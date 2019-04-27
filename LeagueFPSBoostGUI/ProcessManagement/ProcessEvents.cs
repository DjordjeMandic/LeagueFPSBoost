using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Management;
using System.Text;

namespace LeagueFPSBoost.ProcessManagement
{
    public static class ProcessEvents
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void StopCheckWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var pname = e.NewEvent.Properties["ProcessName"].Value;
                if (pname.ToString().ToLower().Contains("league"))
                {
                    try
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine(Strings.tabWithLine + $"Process id: {e.NewEvent.Properties["ProcessID"].Value}");
                        sb.AppendLine(Strings.doubleTabWithLine + $"Name: {pname}");
                        var timecreated = DateTime.FromFileTime(Convert.ToInt64(e.NewEvent.Properties["TIME_CREATED"].Value));
                        sb.AppendLine(Strings.doubleTabWithLine + $"Stop time: {timecreated.ToString(Strings.startTimeFormat)}");

                        var exitCode = -1;
                        try
                        {
                            exitCode = Convert.ToInt32(e.NewEvent.Properties["ExitStatus"].Value);
                        }
                        catch(Exception ex)
                        {
                            logger.Warn(ex, Strings.exceptionThrown + " while converting exit status to Int32: " + Environment.NewLine);
                        }
                        sb.Append(Strings.doubleTabWithLine + $"Exit code: {exitCode} ");
                        if (Strings.TryGetErrorName(exitCode, out string errorName))
                        {
                            sb.Append($"({errorName})");
                        }
                        logger.Debug("Stopped process: " + Environment.NewLine + sb);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, Strings.exceptionThrown + " while reading stop event information." + Environment.NewLine);
                    }
                    LeaguePriority.CheckAndBoost(Program.NoClient);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while executing stop check watch event. " + Environment.NewLine);
            }
            
        }

        public static void StartCheckWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var pid = e.NewEvent.Properties["ProcessID"].Value;
                if (pid.ProcessIDExists())
                {
                    var proc = pid.GetProcessById();
                    if (proc.ProcessName.ToLower().Contains("league"))
                    {
                        logger.Debug("Started process: " + Environment.NewLine + proc.GetProcessInfoForLogging(Program.PrintProcessModules));
                        LeaguePriority.CheckForFreeMemory();
                        LeaguePriority.CheckAndBoost(Program.NoClient);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while executing start check watch event. " + Environment.NewLine);
            }
        }
    }
}
