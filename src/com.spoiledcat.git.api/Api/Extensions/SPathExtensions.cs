using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Unity.VersionControl.Git
{
    using IO;

    public static class SPathExtensions
    {
        public static string CalculateMD5(this SPath file)
        {
            byte[] computeHash;
            using (var md5 = MD5.Create())
            {
                using (var stream = file.OpenRead())
                {
                    computeHash = md5.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
        }
    }
}
