using LeagueFPSBoost.ProcessManagement;
using LeagueFPSBoost.Properties;
using NLog;
using System.Windows.Forms;

namespace LeagueFPSBoost.Updater.MessageBoxCollection
{
    class ManuallyDownloadLeagueFPSBoostMessageBox : MessageBoxData
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ManuallyDownloadLeagueFPSBoostMessageBox()
            : this("For some reason developer is asking to manually download LeagueFPSBoost. " +
                  "Its maybe because of bug with updater or something else. " +
                  "Do you want to download it now? If you click yes, latest realease on github will be opened in web browser.")
        {
            logger.Info("Created new instance without parameters.");
        }

        public ManuallyDownloadLeagueFPSBoostMessageBox(string message)
            : this(message, "LeagueFPSBoost: Manually Download")
        {

        }

        public ManuallyDownloadLeagueFPSBoostMessageBox(string message, string caption)
            : this(message,caption, MessageBoxButtons.YesNo)
        {

        }

        public ManuallyDownloadLeagueFPSBoostMessageBox(string message, string caption, MessageBoxButtons boxButtons)
            : this(message,caption,boxButtons, MessageBoxIcon.Information, true)
        {

        }

        public ManuallyDownloadLeagueFPSBoostMessageBox(string message, string caption, MessageBoxButtons boxButtons, MessageBoxIcon boxIcon, bool runOnce)
            : base(message, caption, boxButtons, boxIcon, true, runOnce)
        {

        }

        public override DialogResult ShowMessageBox()
        {
            if (RunOnce)
            {
                if (!UpdaterMessageBoxSettings.Default.ManuallyDownloadLeagueFPSBoostMessageBox_Ran)
                {
                    return Action();
                }
                else
                {
                    logger.Info("This action has already been ran.");
                    return DialogResult.None;
                }
            }
            else
            {
                return Action();
            }
        }

        private DialogResult Action()
        {
            var result = base.ShowMessageBox();
            UpdaterMessageBoxSettings.Default.ManuallyDownloadLeagueFPSBoostMessageBox_Ran = true;
            UpdaterMessageBoxSettings.Default.Save();
            logger.Debug("Saved ran state for manually download message box.");
            return result;
            
        }

        public static void OpenLatestReleaseURL()
        {
            logger.Info("Opening latest release (github).");
            OpenUrl.Open(@"https://github.com/DjordjeMandic/LeagueFPSBoost/releases/latest");
        }
    }
}
