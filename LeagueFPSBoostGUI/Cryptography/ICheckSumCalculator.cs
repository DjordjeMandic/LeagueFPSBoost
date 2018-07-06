using System;

namespace LeagueFPSBoost.Cryptography
{
    public interface ICheckSumCalculator : IDisposable
    {
        string CalculateMD5();
        string CalculateSHA1();
        string CalculateSHA256();
        string CalculateSHA384();
        string CalculateSHA512();
        string GetHashString(byte[] hash);
        string GetHashString(byte[] hash, bool upperCase);
    }
}
