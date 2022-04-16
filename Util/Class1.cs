using System;
using System.IO;

namespace Antlr4.Build.Tasks.Util
{
    internal static class Class1
    {
        public static bool ExistsOnPath(this string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(this string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
