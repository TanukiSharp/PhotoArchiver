using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PhotoArchiver
{
    public static class Utility
    {
        private static Regex filenameRegex = new Regex(@"^\d{4}\.\d{2}\.\d{2}_\d{2}\.\d{2}\.\d{2}\.", RegexOptions.Compiled);

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

        //public static bool IsExtensionAllowed(Config config, string filename)
        //{
        //    string extension = Path.GetExtension(filename).TrimStart('.').ToLower();
        //    return config.SupportedExtensions.Any(ext => ext.ToLower() == extension
        //}

        //public static bool IsAlreadyNamed(string filename)
        //{
        //    filename = Path.GetFileNameWithoutExtension(filename);
        //    return filenameRegex.IsMatch(filename);
        //}
    }
}
