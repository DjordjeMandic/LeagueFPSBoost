using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LeagueFPSBoost.Properties;
using NLog;

namespace LeagueFPSBoost.Updater.MessageBoxCollection
{
    class GameBarAndFullScrOptimMessageBox : MessageBoxData
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public GameBarAndFullScrOptimMessageBox()
            : this("New feature has been added for Windows 10 users. Disabling Game Bar and Full Screen Optimizations" +
                  " should give a little bit more fps gain.")
        {
            logger.Info("Created new instance without parameters.");
        }

        public GameBarAndFullScrOptimMessageBox(string message)
            : this(message, "LeagueFPSBoost: Game Bar And Full Screen Optimization Feature")
        {

        }

        public GameBarAndFullScrOptimMessageBox(string message, string caption)
            : this(message, caption, MessageBoxButtons.OK)
        {

        }

        public GameBarAndFullScrOptimMessageBox(string message, string caption, MessageBoxButtons boxButtons)
            : this(message, caption, boxButtons, MessageBoxIcon.Information, true)
        {

        }

        public GameBarAndFullScrOptimMessageBox(string message, string caption, MessageBoxButtons boxButtons, MessageBoxIcon boxIcon, bool runOnce)
            : base(message, caption, boxButtons, boxIcon, true, runOnce)
        {

        }

        public override DialogResult ShowMessageBox()
        {
            if (RunOnce)
            {
                if (!UpdaterMessageBoxSettings.Default.GameBarAndFullScrOptimMessageBox_Ran)
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
            UpdaterMessageBoxSettings.Default.GameBarAndFullScrOptimMessageBox_Ran = true;
            UpdaterMessageBoxSettings.Default.Save();
            logger.Debug("Saved ran state for game bar and full screen optimizations message box.");
            return result;
        }
    }
}
