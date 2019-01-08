using LeagueFPSBoost.Properties;
using NLog;
using System;
using System.Windows.Forms;

namespace LeagueFPSBoost.Updater.MessageBoxCollection
{
    class FailedUpdateSorryMessageBox : MessageBoxData
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public FailedUpdateSorryMessageBox()
            : base("I'm sorry for the failed update that may have broke yours LeagueFPSBoost installation." + Environment.NewLine +
                            "I have removed installer and reverted all back to normal updater like it was before." + Environment.NewLine +
                            "If you have any problems with LeagueFPSBoost contact me: leaguefpsboost@gmail.com", "LeagueFPSBoost: Developer Message", MessageBoxButtons.OK, MessageBoxIcon.Information, false, true)
        {

        }

        public override DialogResult ShowMessageBox()
        {
            if (RunOnce)
            {
                if (!UpdaterMessageBoxSettings.Default.FailedUpdateSorryMessageBox_Ran)
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
            UpdaterMessageBoxSettings.Default.FailedUpdateSorryMessageBox_Ran = true;
            UpdaterMessageBoxSettings.Default.Save();
            logger.Debug("Saved ran state for failed update sorry message box.");
            return result;
        }
    }
}
