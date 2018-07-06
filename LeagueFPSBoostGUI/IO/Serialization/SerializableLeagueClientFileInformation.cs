using System;
using System.Runtime.Serialization;

namespace LeagueFPSBoost.IO.Serialization
{
    [Serializable]
    public class SerializableLeagueClientFileInformation : SerializableBasicFileInformation, ILeagueClientFileInfo
    {
        public string PatchVersion { get; private set; }

        public SerializableLeagueClientFileInformation(string leagueClientFileName) : base(leagueClientFileName)
        {
            var patchVerTemp = FileVersion;
            patchVerTemp = patchVerTemp.Remove(patchVerTemp.LastIndexOf(".", StringComparison.Ordinal));
            patchVerTemp = patchVerTemp.Remove(patchVerTemp.LastIndexOf(".", StringComparison.Ordinal));
            PatchVersion = patchVerTemp;
        }

        public SerializableLeagueClientFileInformation(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            PatchVersion = (string)info.GetValue(nameof(PatchVersion), typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(PatchVersion), PatchVersion);
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Patch Version: " + PatchVersion;
        }
    }
}
