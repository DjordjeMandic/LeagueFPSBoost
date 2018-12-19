using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Diagnostics;
using System.IO;

namespace LeagueFPSBoost.ProcessManagement
{
    static class OpenFolder
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Open(string path)
        {
            if (Directory.Exists(path))
            {
                var startInfo = new ProcessStartInfo
                {
                    Arguments = path,
                    FileName = "explorer.exe",
                    Verb = "runas"
                };
                try
                {
                    Process.Start(startInfo);
                    logger.Info("Folder opened: " + path);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, Strings.exceptionThrown + " while opening folder: " + path);
                }
            }
            else
            {
                logger.Warn("Can't open folder because it doesn't exist: " + path);
            }
        }
    }
}
