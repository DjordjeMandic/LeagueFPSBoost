namespace LeagueFPSBoost.Updater.PostUpdateAction
{
    static class ActionList
    {
        public static readonly RestartPostUpdateAction RestartPostUpdate_NoReason = new RestartPostUpdateAction(true);
        public static readonly RestartPostUpdateAction RestartPostUpdate_StabilityReason = new RestartPostUpdateAction("Restarts LeagueFPSBoost without any command line arguments.", "Stability", true);
    }
}
