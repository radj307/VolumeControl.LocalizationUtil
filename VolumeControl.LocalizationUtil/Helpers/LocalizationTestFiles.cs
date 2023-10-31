using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace VolumeControl.LocalizationUtil.Helpers
{
    public static class LocalizationTestFiles
    {
        static LocalizationTestFiles()
        {
            foreach (var resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (resourceName.StartsWith(ResourcePathRoot, StringComparison.Ordinal))
                {
                    _fileNames.Add(resourceName[(ResourcePathRoot.Length + 1)..]);
                }
            }
        }

        public const string ResourcePathRoot = "VolumeControl.LocalizationUtil.LocalizationTestFiles";

        public static IReadOnlyList<string> FileNames => _fileNames;
        private static readonly List<string> _fileNames = new();

        private static string GetResourceFilePath(string fileName)
            => $"{ResourcePathRoot}.{fileName}";

        public static string? GetFileContent(string fileName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetResourceFilePath(fileName));
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
