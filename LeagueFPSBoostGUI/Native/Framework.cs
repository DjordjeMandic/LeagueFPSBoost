using Microsoft.Win32;
using System;

namespace LeagueFPSBoost.Native
{
    class Framework
    {
        private static readonly string NO_45PLUS_DETECTED = "No 4.5 or later version detected";
        private static readonly Exception NO_45PLUS_FRAMEWORK_VER_DETECTED = new Exception("No framework 4.5 or later version detected.");
        public static int Get45PlusReleaseKey()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return (int)ndpKey.GetValue("Release");
                }
                throw NO_45PLUS_FRAMEWORK_VER_DETECTED;
            }
        } 

        // Checking the version using >= will enable forward compatibility.
        public static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 461808)
                return "4.7.2 or later";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return NO_45PLUS_DETECTED;
        }

        public static string Get45PlusVersion()
        {
            var checkver = CheckFor45PlusVersion(Get45PlusReleaseKey());
            if (checkver == NO_45PLUS_DETECTED) throw NO_45PLUS_FRAMEWORK_VER_DETECTED;
            return checkver;
        }

        public static bool Net472OrLaterInstalled()
        {
            return Get45PlusReleaseKey() >= 461808;
        }
    }
}
