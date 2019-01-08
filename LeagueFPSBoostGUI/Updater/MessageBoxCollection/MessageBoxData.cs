using LeagueFPSBoost.Text;
using Newtonsoft.Json;
using NLog;
using System;
using System.Windows.Forms;

namespace LeagueFPSBoost.Updater.MessageBoxCollection
{

    [JsonObject(MemberSerialization.OptIn)]
    public class MessageBoxData : IEquatable<MessageBoxData>, IUpdaterMessageBox
    {
        [JsonProperty]
        public string Caption { get; private set; }

        [JsonProperty]
        public MessageBoxIcon MessageBoxIcon { get; private set; }

        [JsonProperty]
        public MessageBoxButtons MessageBoxButton { get; private set; }

        [JsonProperty]
        public string Message { get; private set; }

        [JsonProperty]
        public bool RequiresSpecialCall { get; private set; }

        [JsonProperty]
        public bool RunOnce { get; private set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MessageBoxData(string message)
            : this(message, "LeagueFPSBoost")
        {

        }

        public MessageBoxData(string message, bool requiresSpecialCall)
            : this(message, "LeagueFPSBoost", requiresSpecialCall)
        {

        }

        public MessageBoxData(string message, string caption)
            : this(message, caption, MessageBoxButtons.OK)
        {
        }

        public MessageBoxData(string message, string caption, bool requiresSpecialCall)
            : this(message, caption, MessageBoxButtons.OK, requiresSpecialCall)
        {
        }

        public MessageBoxData(string message, string caption, MessageBoxButtons messageBoxButton)
            : this(message, caption, messageBoxButton, MessageBoxIcon.None, false, true)
        {
        }

        public MessageBoxData(string message, string caption, MessageBoxButtons messageBoxButton, bool requiresSpecialCall)
            : this(message, caption, messageBoxButton, MessageBoxIcon.None, requiresSpecialCall, true)
        {
        }

        public MessageBoxData(string message, string caption, MessageBoxButtons messageBoxButton, MessageBoxIcon messageBoxIcon)
            : this(message, caption, messageBoxButton, messageBoxIcon, false, true)
        {
        }

        public MessageBoxData(string message, string caption, MessageBoxButtons messageBoxButton, MessageBoxIcon messageBoxIcon, bool requiresSpecialCall, bool runOnce)
        {
            Message = message;
            Caption = caption;
            MessageBoxButton = messageBoxButton;
            MessageBoxIcon = messageBoxIcon;
            RequiresSpecialCall = requiresSpecialCall;
            RunOnce = runOnce;
            logger.Debug("Created new instance: " + Environment.NewLine +
                Strings.tabWithLine + "Message: " + Message + Environment.NewLine +
                Strings.tabWithLine + "Caption: " + Caption + Environment.NewLine +
                Strings.tabWithLine + "MessageBoxButton: " + MessageBoxButton + Environment.NewLine +
                Strings.tabWithLine + "MessageBoxIcon: " + MessageBoxIcon + Environment.NewLine +
                Strings.tabWithLine + "Requires Special Call: " + requiresSpecialCall + Environment.NewLine +
                Strings.tabWithLine + "RunOnce: " + RunOnce);
        }

        [JsonConstructor]
        public MessageBoxData(string caption, MessageBoxIcon messageBoxIcon, MessageBoxButtons messageBoxButton, string message, bool requiresSpecialCall, bool runOnce)
            : this(message, caption, messageBoxButton, messageBoxIcon, requiresSpecialCall, runOnce)
        {

        }

        public virtual DialogResult ShowMessageBox()
        {
            logger.Debug("Showing new message box: " + Environment.NewLine +
                ToStringTabbed());
            var result = MessageBox.Show(Message, Caption, MessageBoxButton, MessageBoxIcon);
            logger.Info("Message box closed with result: " + result);
            return result;
        }

        public override string ToString()
        {
            return "Message: " + Message + Environment.NewLine +
                "Caption: " + Caption + Environment.NewLine +
                "MessageBoxButtons: " + MessageBoxButton + Environment.NewLine +
                "MessageBoxIcon: " + MessageBoxIcon + Environment.NewLine + 
                "Requires Special Call: " + RequiresSpecialCall + Environment.NewLine +
                "RunOnce: " + RunOnce;
        }

        public string ToStringTabbed()
        {
            return Strings.tabWithLine + "Message: " + Message + Environment.NewLine +
                Strings.tabWithLine + "Caption: " + Caption + Environment.NewLine +
                Strings.tabWithLine + "MessageBoxButtons: " + MessageBoxButton + Environment.NewLine +
                Strings.tabWithLine + "MessageBoxIcon: " + MessageBoxIcon + Environment.NewLine +
                Strings.tabWithLine + "Requires Special Call: " + RequiresSpecialCall + Environment.NewLine +
                Strings.tabWithLine + "RunOnce: " + RunOnce;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MessageBoxData);
        }

        public bool Equals(MessageBoxData other)
        {
            //Check whether the compared object is null. 
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data. 
            if (Object.ReferenceEquals(this, other)) return true;

            return(Message == other.Message) &&
                (Caption == other.Caption) && 
                (MessageBoxButton == other.MessageBoxButton) && 
                (MessageBoxIcon == other.MessageBoxIcon) && 
                (RequiresSpecialCall == other.RequiresSpecialCall);
        }

        public override int GetHashCode()
        {
            var result = 0;
            result = (result * 397) ^ (Message == null ? 0 : Message.GetHashCode());
            result = (result * 397) ^ (Caption == null ? 0 : Caption.GetHashCode());
            result = (result * 397) ^ MessageBoxButton.GetHashCode();
            result = (result * 397) ^ MessageBoxIcon.GetHashCode();
            result = (result * 397) ^ RequiresSpecialCall.GetHashCode();
            return result;
        }

        public bool GetRequiresSpecialCall()
        {
            return RequiresSpecialCall;
        }

        public static bool operator ==(MessageBoxData x, MessageBoxData y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(MessageBoxData x, MessageBoxData y)
        {
            return !x.Equals(y);
        }
    }
}
