using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PhotoArchiver
{
    public static class Utility
    {
        private static string homePath;

        private static string GetHomePath()
        {
            if (homePath == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    homePath = Environment.GetEnvironmentVariable("HOME");
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    homePath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
                else
                    throw new PlatformNotSupportedException($"Architecture: {RuntimeInformation.OSArchitecture}, Description: {RuntimeInformation.OSDescription}");
            }

            return homePath;
        }

        public static string ResolvePath(string path)
        {
            if (path == null)
                return null;

            path = path.Trim();

            if (path == "~")
                return GetHomePath();

            if (path.StartsWith("~" + Path.DirectorySeparatorChar))
                path = Path.Combine(GetHomePath(), path.Substring(2));

            return Path.GetFullPath(path);
        }

        public static void ChOwn(string owner, string group, string filename)
        {
            if (owner.Any(char.IsWhiteSpace) || group.Any(char.IsWhiteSpace))
                throw new ArgumentException();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("sudo", $"chown {owner.Trim()}:{group.Trim()} \"{filename}\"");
        }
    }
}
