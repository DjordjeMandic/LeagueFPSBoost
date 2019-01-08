using LeagueFPSBoost.ProcessManagement;
using LeagueFPSBoost.Properties;
using NLog;
using System;

namespace LeagueFPSBoost.Updater.PostUpdateAction
{
    class RestartPostUpdateAction : PostUpdateActionData, IPostUpdateAction
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static readonly long ActionID = 0;
        
        public RestartPostUpdateAction(bool runOnce)
            : this("Restarts LeagueFPSBoost.", runOnce)
        {

        }

        public RestartPostUpdateAction(string desctiption, bool runOnce)
            : this(desctiption, "No reason available.", runOnce)
        {

        }

        public RestartPostUpdateAction(string desctiption, string reason, bool runOnce)
            : this(desctiption, reason, new Version(), runOnce)
        {

        }

        public RestartPostUpdateAction(string desctiption, string reason, Version startAtVersion, bool runOnce)
            : this(desctiption,reason, startAtVersion, new Version(), runOnce)
        {

        }

        public RestartPostUpdateAction(string desctiption, string reason, Version startAtVersion, Version stopAtVersion, bool runOnce)
            : base(ActionID, desctiption,reason,startAtVersion,stopAtVersion, runOnce)
        {

        }

        public bool Run()
        {
            if(RunOnce)
            {
                if(!UpdaterActionsSettings.Default.RestartPostUpdateAction_Ran)
                {
                    return Action();
                }
                else
                {
                    logger.Info("This action has already been ran.");
                    return false;
                }
            }
            else
            {
                return Action();
            }
        }

        private bool Action()
        {
            logger.Info("Checking for user permission before running action.");
            if (GetRunPermission())
            {
                logger.Info("Got permission. Running restart task.");
                UpdaterActionsSettings.Default.RestartPostUpdateAction_Ran = true;
                UpdaterActionsSettings.Default.Save();
                logger.Info("Saved ran state into settings.");
                return Restart.RestartNow();
            }
            logger.Warn("No permission given to run the restart task.");
            return false;
        }
    }
}
