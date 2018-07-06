using System;

namespace LeagueFPSBoost.IO
{
    interface IBasicFileInfo
    {
        string SHA1_Hash { get; }
        string SHA256_Hash { get; }
        string MD5_Hash { get; }

        string Path { get; }

        string FileDescription { get; }
        string FileVersion { get; }
        string ProductName { get; }
        string ProductVersion { get; }
        string Copyright { get; }
        string OriginalFileName { get; }

        string HumanReadableFileSize { get; }

        DateTime LastWriteTimeUtc { get; }
        DateTime LastAccessTimeUtc { get; }
        DateTime CreationTimeUtc { get; }

        DateTime FileInfoCreationTimeUtc { get; }

        string LeagueFPSBoostAssemblyVersion { get; }

        string ToString();
    }
}
