using System;
using System.Diagnostics;
using System.IO;

namespace Unity.VersionControl.Git
{
    public static class Utils
    {
        public static bool Copy(Stream source, Stream destination,
            long totalSize = 0,
            int chunkSize = 8192,
            Func<long, long, bool> progress = null,
            int progressUpdateRate = 100)
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead = 0;
            long totalRead = 0;
            float averageSpeed = -1f;
            float lastSpeed = 0f;
            float smoothing = 0.005f;
            long readLastSecond = 0;
            long timeToFinish = 0;
            Stopwatch watch = null;
            bool success = true;
            totalSize = totalSize < 0 ? 0 : totalSize;

            bool trackProgress = totalSize > 0 && progress != null;
            if (trackProgress)
                watch = new Stopwatch();

            do
            {
                if (trackProgress)
                    watch.Start();

                bytesRead = source.Read(buffer, 0, totalSize > 0 && totalRead + chunkSize > totalSize ? (int)(totalSize - totalRead) : chunkSize);

                if (trackProgress)
                    watch.Stop();

                totalRead += bytesRead;

                if (bytesRead > 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                    if (trackProgress)
                    {
                        readLastSecond += bytesRead;
                        if (watch.ElapsedMilliseconds >= progressUpdateRate || totalRead == totalSize || bytesRead == 0)
                        {
                            watch.Reset();
                            if (bytesRead == 0) // we've reached the end
                                totalSize = totalRead;

                            lastSpeed = readLastSecond;
                            readLastSecond = 0;
                            averageSpeed = averageSpeed < 0f
                                ? lastSpeed
                                : smoothing * lastSpeed + (1f - smoothing) * averageSpeed;
                            timeToFinish = Math.Max(1L,
                                (long)((totalSize - totalRead) / (averageSpeed / progressUpdateRate)));

                            success = progress(totalRead, timeToFinish);
                            if (!success)
                                break;
                        }
                    }
                    else // we still need to call the callback if it's there, so we can abort if needed
                    {
                        success = progress?.Invoke(totalRead, timeToFinish) ?? true;
                        if (!success)
                            break;
                    }
                }
            } while (bytesRead > 0 && (totalSize == 0 || totalSize > totalRead));

            if (totalRead > 0)
                destination.Flush();
            return success;
        }
        public static bool VerifyFileIntegrity(NPath file, string hash)
        {
            if (!file.IsInitialized || !file.FileExists())
                return false;
            string actual = null;
            if (hash.Length == 32)
                actual = file.ToMD5();
            else
                actual = file.ToSha256();
            return hash.Equals(actual, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string ToMD5(this NPath path)
        {
            byte[] computeHash;
            using (var hash = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = NPath.FileSystem.OpenRead(path))
                {
                    computeHash = hash.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
        }

        private static string ToSha256(this NPath path)
        {
            byte[] computeHash;
            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = NPath.FileSystem.OpenRead(path))
                {
                    computeHash = hash.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
        }

    }
}
