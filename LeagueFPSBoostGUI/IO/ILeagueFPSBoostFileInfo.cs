using System;

namespace LeagueFPSBoost.IO
{
    interface ILeagueFPSBoostFileInfo
    {
        DateTime BuildTimeUtc { get; }
        bool IsDebugBuild { get; }
    }
}
