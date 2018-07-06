using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace LeagueFPSBoost.Extensions
{
    public static class HelpingExtensions
    {
        public static bool IsAssemblyDebugBuild(this Assembly assembly)
        {
            return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        }
    }
}
