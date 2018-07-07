using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Diagnostics;

namespace LeagueFPSBoost.ProcessManagement
{
    public static class OpenUrl
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static bool Open(string url)
        {
            try
            {
                logger.Debug("Trying to open url: " + url);
                Process.Start(url);
                logger.Debug("Url successfully opened: " + url);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, Strings.exceptionThrown + " while opening url: " + url + Environment.NewLine);
                return false;
            }
        }
    }
}
