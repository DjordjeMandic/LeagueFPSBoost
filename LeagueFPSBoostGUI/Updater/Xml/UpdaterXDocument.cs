using NLog;
using System;
using System.Xml.Linq;

namespace LeagueFPSBoost.Updater.Xml
{
    public static class UpdaterXDocument
    {
        public static bool Mandatory = true;
        public static Version Version = Program.Version;
        public static string DownloadURL = "https://dl.dropboxusercontent.com/s/e8ogbhle3i0v7aw/LeagueFPSBoost.zip";
        public static string ChangelogURL = "https://boards.eune.leagueoflegends.com/en/c/alpha-client-discussion-en/jkmeEvQe-fps-boost-program-open-source-ask-any-questions-if-you-have";
        public static string CommandLineArguments = string.Empty;

        public static Checksum Checksum = new Checksum();

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static XDocument Save(string fileName)
        {
            if (string.IsNullOrEmpty(DownloadURL)) { logger.Error("DownloadURL is null or empty."); throw new ArgumentException("DownloadURL is null or empty."); }
            
            var xmlItem = new XElement("item");
            xmlItem.Add(new XElement("version", Version.ToString()));
            xmlItem.Add(new XElement("url", DownloadURL));

            if (!string.IsNullOrEmpty(ChangelogURL)) xmlItem.Add(new XElement("changelog", ChangelogURL));

            xmlItem.Add(new XElement("mandatory", Mandatory));

            if (!string.IsNullOrEmpty(CommandLineArguments)) xmlItem.Add(new XElement("args", CommandLineArguments));

            if (!string.IsNullOrEmpty(Checksum.Value))
            {
                var xmlChecksum = new XElement("checksum", Checksum.Value);
                xmlChecksum.SetAttributeValue("algorithm", Checksum.Type);
                xmlItem.Add(xmlChecksum);
            }
            var xmlDoc = new XDocument(xmlItem);
            xmlDoc.Save(fileName);
            return xmlDoc;
        }
    }
}
