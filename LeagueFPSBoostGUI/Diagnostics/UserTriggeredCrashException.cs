using System;
using LeagueFPSBoost.Text;

namespace LeagueFPSBoost.Diagnostics
{
    class UserTriggeredCrashException : Exception
    {
        public UserTriggeredCrashException()
            : base(Strings.UserTriggeredExceptionMessage + Environment.NewLine + $"DateTime: {DateTime.Now.ToString(Strings.startTimeFormat)}" )
        {
        }
    }
}
