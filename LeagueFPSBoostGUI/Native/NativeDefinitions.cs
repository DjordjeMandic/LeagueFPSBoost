using System;

namespace LeagueFPSBoost.Native
{
    public static class NativeStrings
    {
        public const string BALANCED_POWER_PLAN_GUID_STR = "381b4222-f694-41f0-9685-ff5bb260df2e";
        public const string HIGH_PERFORMANCE_POWER_PLAN_GUID_STR = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        public const string POWER_SAVER_POWER_PLAN_GUID_STR = "a1841308-3541-4fab-bc81-f71556f20b4a";
    }

    public static class NativeGUIDs
    {
        public static readonly Guid BALANCED_POWER_PLAN_GUID = new Guid(NativeStrings.BALANCED_POWER_PLAN_GUID_STR);
        public static readonly Guid HIGH_PERFORMANCE_POWER_PLAN_GUID = new Guid(NativeStrings.HIGH_PERFORMANCE_POWER_PLAN_GUID_STR);
        public static readonly Guid POWER_SAVER_POWER_PLAN_GUID = new Guid(NativeStrings.POWER_SAVER_POWER_PLAN_GUID_STR);
    }
}
