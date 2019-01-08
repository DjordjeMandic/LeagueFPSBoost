using LeagueFPSBoost.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Windows.Forms;

namespace LeagueFPSBoost.Updater.PostUpdateAction
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PostUpdateActionData : IEquatable<PostUpdateActionData>
    {
        [JsonProperty]
        public long ID { get; private set; }

        [JsonProperty]
        public string Description { get; private set; }

        [JsonProperty]
        public string Reason { get; private set; }

        [JsonProperty]
        [JsonConverter(typeof(VersionConverter))]
        public Version StartAtVersion { get; private set; }

        [JsonProperty]
        [JsonConverter(typeof(VersionConverter))]
        public Version StopAtVersion { get; private set; }

        [JsonProperty]
        public bool RunOnce { get; private set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public PostUpdateActionData(long id)
            : this(id, "No description available.")
        {

        }

        public PostUpdateActionData(long id, string description)
            : this(id, description, "No reason available.")
        {

        }

        public PostUpdateActionData(long id, string description, string reason)
            : this(id, description, reason, new Version())
        {

        }

        public PostUpdateActionData(long id, string description, string reason, Version startAtVersion)
            : this(id, description, reason, startAtVersion, new Version(), true)
        {

        }

        [JsonConstructor]
        public PostUpdateActionData(long id, string description, string reason, Version startAtVersion, Version stopAtVersion, bool runOnce)
        {
            ID = id;
            Description = description;
            Reason = reason;
            StartAtVersion = startAtVersion;
            StopAtVersion = stopAtVersion;
            RunOnce = runOnce;

            logger.Info("Created new instance:" + Environment.NewLine + 
                Strings.tabWithLine + "ID: " + ID + Environment.NewLine +
                Strings.tabWithLine + "Description: " + Description + Environment.NewLine +
                Strings.tabWithLine + "Reason: " + Reason + Environment.NewLine +
                Strings.tabWithLine + "StartAtVersion: " + StartAtVersion + Environment.NewLine +
                Strings.tabWithLine + "StopAtVersion: " + StopAtVersion + Environment.NewLine + 
                Strings.tabWithLine + "RunOnce: " + RunOnce);
        }

        public bool Equals(PostUpdateActionData other)
        {
            //Check whether the compared object is null. 
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data. 
            if (Object.ReferenceEquals(this, other)) return true;

            return ID == other.ID && Description == other.Description && Reason == other.Reason && StartAtVersion == other.StartAtVersion && StopAtVersion == other.StopAtVersion;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PostUpdateActionData);
        }

        public override int GetHashCode()
        {
            var result = 0;
            result = (result * 397) ^ ID.GetHashCode();
            result = (result * 397) ^ (Description == null ? 0 : Description.GetHashCode());
            result = (result * 397) ^ (Reason == null ? 0 : Reason.GetHashCode());
            result = (result * 397) ^ (StartAtVersion == null ? 0 : StartAtVersion.GetHashCode());
            result = (result * 397) ^ (StopAtVersion == null ? 0 : StopAtVersion.GetHashCode());
            return result;
        }

        public static bool operator ==(PostUpdateActionData x, PostUpdateActionData y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(PostUpdateActionData x, PostUpdateActionData y)
        {
            return !x.Equals(y);
        }

        public override string ToString()
        {
            return "ID: " + ID + Environment.NewLine +
                "Description: " + Description + Environment.NewLine +
                "Reason: " + Reason + Environment.NewLine +
                "StartAtVersion: " + StartAtVersion + Environment.NewLine +
                "StopAtVersion: " + StopAtVersion;
        }

        public string ToStringTabbed()
        {
            return Strings.tabWithLine + "ID: " + ID + Environment.NewLine +
                Strings.tabWithLine + "Description: " + Description + Environment.NewLine +
                Strings.tabWithLine + "Reason: " + Reason + Environment.NewLine +
                Strings.tabWithLine + "StartAtVersion: " + StartAtVersion + Environment.NewLine +
                Strings.tabWithLine + "StopAtVersion: " + StopAtVersion + Environment.NewLine +
                Strings.tabWithLine + "RunOnce: " + RunOnce;
        }

        public bool GetRunPermission()
        {
            if (StartAtVersion == null || StopAtVersion == null) return false;

            return (StartAtVersion == new Version() && StopAtVersion == new Version()) ||
                (StartAtVersion != new Version() && StopAtVersion == new Version() && Program.Version >= StartAtVersion) ||
                (StartAtVersion == new Version() && StopAtVersion != new Version() && Program.Version < StopAtVersion) ||
                (StartAtVersion != new Version() && StopAtVersion != new Version() && Program.Version >= StartAtVersion && Program.Version < StopAtVersion) ? UserPermission() : false;
        }

        private bool UserPermission()
        {
            return DialogResult.Yes == MessageBox.Show("Developer recommends running this action: " + Environment.NewLine + ToString() + Environment.NewLine + "Would you like to run it?", "LeagueFPSBoost: Post Update Action", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        }
    }
}
