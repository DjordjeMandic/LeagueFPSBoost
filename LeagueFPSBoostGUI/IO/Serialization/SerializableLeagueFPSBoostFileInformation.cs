using LeagueFPSBoost.Extensions;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace LeagueFPSBoost.IO.Serialization
{
    [Serializable]
    public class SerializableLeagueFPSBoostFileInformation : SerializableBasicFileInformation, ILeagueFPSBoostFileInfo
    {
        public DateTime BuildTimeUtc { get; private set; }

        public bool IsDebugBuild { get; private set; }

        public SerializableLeagueFPSBoostFileInformation(Assembly assembly) : base(assembly.Location)
        {
            if (ProductName != "LeagueFPSBoost") throw new ArgumentException("Assembly product name doesn't match LeagueFPSBoost's product name.");
            BuildTimeUtc = assembly.GetLinkerTime().ToUniversalTime();
            IsDebugBuild = assembly.IsAssemblyDebugBuild();
        }

        public SerializableLeagueFPSBoostFileInformation(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            BuildTimeUtc = (DateTime)info.GetValue(nameof(BuildTimeUtc), typeof(DateTime));
            IsDebugBuild = (bool)info.GetValue(nameof(IsDebugBuild), typeof(bool));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(BuildTimeUtc), BuildTimeUtc);
            info.AddValue(nameof(IsDebugBuild), IsDebugBuild);
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Build Time Utc: " + BuildTimeUtc + Environment.NewLine +
                   "Is Debug Build: " + IsDebugBuild;
        }
    }
}
