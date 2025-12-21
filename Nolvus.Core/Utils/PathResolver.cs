using System;
using System.IO;

namespace Nolvus.Core.Utils
{
    public static class PathResolver
    {
        public static string? FindExecutable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // If name contains slash treat as a path and don't search PATH.
            if (name.Contains('/'))
            {
                var candidate = Path.GetFullPath(name);
                return IsExecutable(candidate) ? candidate : null;
            }

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv))
                return null;

            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;

                var fullPath = Path.Combine(dir.Trim(), name);
                if (IsExecutable(fullPath))
                    return fullPath;
            }

            return null;
        }

        public static string RequireExecutable(string name)
        {
            var path = FindExecutable(name);
            if (path == null)
                throw new FileNotFoundException($"Required executable '{name}' not found in PATH.");
            return path;
        }

        private static bool IsExecutable(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                var mode = File.GetUnixFileMode(path);
                const UnixFileMode anyExec =
                    UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

                return (mode & anyExec) != 0;
            }
            catch
            {
                // If we can't read mode assume not executable
                return false;
            }
        }
    }
}
