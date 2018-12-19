using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace LeagueFPSBoost.Cryptography
{
    public enum ChecksumType
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512
    }

    public struct Checksum
    {
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ChecksumType Type { get; private set; }
        [JsonProperty]
        public string Value { get; private set; }

        public Checksum(Stream stream, ChecksumType type = ChecksumType.MD5)
        {
            Type = type;
            var chksum = string.Empty;
            using (ICheckSumCalculator checkSumCalc = new ChecksumCalculator(stream))
            {
                switch (Type)
                {
                    case ChecksumType.MD5:
                        chksum = checkSumCalc.CalculateMD5();
                        break;
                    case ChecksumType.SHA1:
                        chksum = checkSumCalc.CalculateSHA1();
                        break;
                    case ChecksumType.SHA256:
                        chksum = checkSumCalc.CalculateSHA256();
                        break;
                    case ChecksumType.SHA384:
                        chksum = checkSumCalc.CalculateSHA384();
                        break;
                    case ChecksumType.SHA512:
                        chksum = checkSumCalc.CalculateSHA512();
                        break;
                }
            }
            Value = chksum;
        }

        [JsonConstructor]
        public Checksum(ChecksumType type, string val)
        {
            Type = type;
            Value = val;
        }

    }
}
