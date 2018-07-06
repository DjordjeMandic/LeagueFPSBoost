using ByteSizeLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace LeagueFPSBoost.IO.Serialization
{
    [Serializable]
    public class SerializableBasicFileInformation : ISerializable, IBasicFileInfo
    {
        public string SHA1_Hash { get; private set; }
        public string SHA256_Hash { get; private set; }
        public string MD5_Hash { get; private set; }

        public string Path { get; private set; }

        public string FileDescription { get; private set; }
        public string FileVersion { get; private set; }
        public string ProductName { get; private set; }
        public string ProductVersion { get; private set; }
        public string Copyright { get; private set; }
        public string OriginalFileName { get; private set; }

        public string HumanReadableFileSize { get; private set; }

        public DateTime LastWriteTimeUtc { get; private set; }
        public DateTime LastAccessTimeUtc { get; private set; }
        public DateTime CreationTimeUtc { get; private set; }

        public DateTime FileInfoCreationTimeUtc { get; private set; }

        public string LeagueFPSBoostAssemblyVersion { get; } = Program.CurrentVersionFull;

        public SerializableBasicFileInformation(string filePath)
        {
            FileInfoCreationTimeUtc = DateTime.UtcNow;
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Could not find file '{filePath}'.");
            using (var stream = File.OpenRead(filePath))
            {
                CalculateMD5(stream);
                CalculateSHA1(stream);
                CalculateSHA256(stream);
            }

            var fvi = FileVersionInfo.GetVersionInfo(filePath);
            Path = filePath;
            FileDescription = fvi.FileDescription;
            FileVersion = fvi.FileVersion;
            ProductName = fvi.ProductName;
            ProductVersion = fvi.ProductVersion;
            Copyright = fvi.LegalCopyright;
            OriginalFileName = fvi.OriginalFilename;

            var fi = new FileInfo(filePath);
            LastWriteTimeUtc = fi.LastWriteTimeUtc;
            LastAccessTimeUtc = fi.LastAccessTimeUtc;
            CreationTimeUtc = fi.CreationTimeUtc;

            HumanReadableFileSize = ByteSize.FromBytes(fi.Length).ToString();
        }

        public SerializableBasicFileInformation(SerializationInfo info, StreamingContext context)
        {
            SHA1_Hash = (string)info.GetValue(nameof(SHA1_Hash), typeof(string));
            SHA256_Hash = (string)info.GetValue(nameof(SHA256_Hash), typeof(string));
            MD5_Hash = (string)info.GetValue(nameof(MD5_Hash), typeof(string));

            Path = (string)info.GetValue(nameof(Path), typeof(string));

            FileDescription = (string)info.GetValue(nameof(FileDescription), typeof(string));
            FileVersion = (string)info.GetValue(nameof(FileVersion), typeof(string));
            ProductName = (string)info.GetValue(nameof(ProductName), typeof(string));
            ProductVersion = (string)info.GetValue(nameof(ProductVersion), typeof(string));
            Copyright = (string)info.GetValue(nameof(Copyright), typeof(string));
            OriginalFileName = (string)info.GetValue(nameof(OriginalFileName), typeof(string));

            LastWriteTimeUtc = (DateTime)info.GetValue(nameof(LastWriteTimeUtc), typeof(DateTime));
            LastAccessTimeUtc = (DateTime)info.GetValue(nameof(LastAccessTimeUtc), typeof(DateTime));
            CreationTimeUtc = (DateTime)info.GetValue(nameof(CreationTimeUtc), typeof(DateTime));

            FileInfoCreationTimeUtc = (DateTime)info.GetValue(nameof(FileInfoCreationTimeUtc), typeof(DateTime));

            LeagueFPSBoostAssemblyVersion = (string)info.GetValue(nameof(LeagueFPSBoostAssemblyVersion), typeof(string));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(SHA1_Hash), SHA1_Hash);
            info.AddValue(nameof(SHA256_Hash), SHA256_Hash);
            info.AddValue(nameof(MD5_Hash), MD5_Hash);

            info.AddValue(nameof(Path), Path);

            info.AddValue(nameof(FileDescription), FileDescription);
            info.AddValue(nameof(FileVersion), FileVersion);
            info.AddValue(nameof(ProductName), ProductName);
            info.AddValue(nameof(ProductVersion), ProductVersion);
            info.AddValue(nameof(Copyright), Copyright);
            info.AddValue(nameof(OriginalFileName), OriginalFileName);

            info.AddValue(nameof(HumanReadableFileSize), HumanReadableFileSize);

            info.AddValue(nameof(LastWriteTimeUtc), LastWriteTimeUtc);
            info.AddValue(nameof(LastAccessTimeUtc), LastAccessTimeUtc);
            info.AddValue(nameof(CreationTimeUtc), CreationTimeUtc);

            info.AddValue(nameof(FileInfoCreationTimeUtc), FileInfoCreationTimeUtc);

            info.AddValue(nameof(LeagueFPSBoostAssemblyVersion), LeagueFPSBoostAssemblyVersion);
        }


        void CalculateSHA1(Stream stream)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(stream);
                var formatted = new StringBuilder(2 * hash.Length);
                foreach (byte b in hash)
                {
                    formatted.AppendFormat("{0:X2}", b);
                }
                SHA1_Hash = formatted.ToString().ToLowerInvariant();
            }
        }

        void CalculateMD5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                MD5_Hash = BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
            }
        }

        void CalculateSHA256(Stream stream)
        {
            using (var sha256 = new SHA256Managed())
            {
                var sha256hash = sha256.ComputeHash(stream);
                SHA256_Hash = BitConverter.ToString(sha256hash).Replace("-", String.Empty).ToLowerInvariant();
            }
        }

        public override string ToString()
        {
            return "Path: " + Path + Environment.NewLine +

                   "SHA1: " + SHA1_Hash + Environment.NewLine +
                   "SHA256: " + SHA256_Hash + Environment.NewLine +
                   "MD5: " + MD5_Hash + Environment.NewLine +

                   "File Description: " + FileDescription + Environment.NewLine +
                   "File Version: " + FileVersion + Environment.NewLine +
                   "Product Name: " + ProductName + Environment.NewLine +
                   "Product Version: " + ProductVersion + Environment.NewLine +
                   "Copyright: " + Copyright + Environment.NewLine +
                   "Original File Name: " + OriginalFileName + Environment.NewLine +

                   "File Size: " + HumanReadableFileSize + Environment.NewLine +

                   "Last Write Time Utc: " + LastWriteTimeUtc + Environment.NewLine +
                   "Last Access Time Utc: " + LastAccessTimeUtc + Environment.NewLine +
                   "Creation Time Utc: " + CreationTimeUtc + Environment.NewLine +
                   "Information Creation Time Utc: " + FileInfoCreationTimeUtc;
        }
    }
}
