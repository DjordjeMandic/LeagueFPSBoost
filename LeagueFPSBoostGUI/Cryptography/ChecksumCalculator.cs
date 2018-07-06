using System;
using System.IO;
using System.Security.Cryptography;

namespace LeagueFPSBoost.Cryptography
{
    public class ChecksumCalculator : ICheckSumCalculator
    {
        public Stream Stream { get; private set; }

        public ChecksumCalculator(Stream stream)
        {
            Stream = stream;
        }

        public string CalculateMD5()
        {
            using (var md5 = MD5.Create())
            {
                return GetHashString(md5.ComputeHash(Stream));
            }
        }

        public string CalculateSHA1()
        {
            using (var sha1 = new SHA1Managed())
            {
                return GetHashString(sha1.ComputeHash(Stream));
            }
        }

        public string CalculateSHA256()
        {
            using (var sha256 = new SHA256Managed())
            {
                return GetHashString(sha256.ComputeHash(Stream));
            }
        }

        public string CalculateSHA384()
        {
            using (var sha384 = new SHA384Managed())
            {
                return GetHashString(sha384.ComputeHash(Stream));
            }
        }

        public string CalculateSHA512()
        {
            using (var sha512 = new SHA512Managed())
            {
                return GetHashString(sha512.ComputeHash(Stream));
            }
        }

        public string GetHashString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", String.Empty).ToLower();
        }

        public string GetHashString(byte[] hash, bool upperCae)
        {
            return GetHashString(hash).ToUpper();
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
