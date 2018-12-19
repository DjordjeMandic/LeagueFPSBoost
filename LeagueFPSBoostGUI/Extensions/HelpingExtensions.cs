using LeagueFPSBoost.Text;
using System;
using System.Diagnostics;
using System.IO;
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

        public static string GetTempFilePath(string extension, string type)
        {
            return GetTempFilePath(Path.GetTempPath(), extension, type);
        }

        public static string GetTempFilePath(this string path, string extension, string type)
        {
            return Path.Combine(path, "LeagueFPSBoost_" + type + "_" + Guid.NewGuid().ToString() + "_" + DateTime.UtcNow.ToString(Strings.logDateTimeFormat) + extension);
        }

        /// <summary>Indicates whether the specified array is null or has a length of zero.</summary>
        /// <param name="array">The array to test.</param>
        /// <returns>true if the array parameter is null or has a length of zero; otherwise, false.</returns>
        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }

        public static void ClearEventInvocations(this object obj, string eventName)
        {
            var fi = obj.GetType().GetEventField(eventName);
            if (fi == null) return;
            fi.SetValue(obj, null);
        }

        private static FieldInfo GetEventField(this Type type, string eventName)
        {
            FieldInfo field = null;
            while (type != null)
            {
                /* Find events defined as field */
                field = type.GetField(eventName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null && (field.FieldType == typeof(MulticastDelegate) || field.FieldType.IsSubclassOf(typeof(MulticastDelegate))))
                    break;

                /* Find events defined as property { add; remove; } */
                field = type.GetField("EVENT_" + eventName.ToUpper(), BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    break;
                type = type.BaseType;
            }
            return field;
        }
    }

    public sealed class WriteOnce<T>
    {
        private T value;
        private bool hasValue;
        public override string ToString()
        {
            return hasValue ? Convert.ToString(value) : "";
        }
        public T Value
        {
            get
            {
                if (!hasValue) throw new InvalidOperationException("Value not set");
                return value;
            }
            set
            {
                if (hasValue) throw new InvalidOperationException("Value already set");
                this.value = value;
                this.hasValue = true;
            }
        }
        public T ValueOrDefault { get { return value; } }

        public static implicit operator T(WriteOnce<T> value) { return value.Value; }
    }
}
