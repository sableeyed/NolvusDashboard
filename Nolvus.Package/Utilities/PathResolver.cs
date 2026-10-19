using System;
using System.IO;
using System.Linq;

namespace Nolvus.Package.Utilities
{
    public static class PathResolver
    {
        /// <summary>
        /// Resolves a multi-segment directory path inside <paramref name="root"/>
        /// in a case-insensitive way. Returns the real existing directory path
        /// or null if not found.
        /// </summary>
        public static string? ResolveCaseInsensitiveDirectory(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath))
                return null;

            string current = root;

            foreach (var segment in relativePath.Split(new[] { '/', '\\' },
                            StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Directory.Exists(current))
                    return null;

                var match = FindCaseInsensitiveMatch(
                    Directory.GetDirectories(current),
                    segment
                );

                if (match == null)
                    return null;

                current = match;
            }

            return current;
        }

        /// <summary>
        /// Resolves a file name inside a directory in a case-insensitive way.
        /// Returns full absolute file path or null.
        /// </summary>
        public static string? ResolveCaseInsensitiveFile(string directory, string fileName)
        {
            if (!Directory.Exists(directory))
                return null;

            return FindCaseInsensitiveMatch(
                Directory.GetFiles(directory), 
                fileName
            );
        }

        /// <summary>
        /// Resolves <paramref name="relativePath"/> under <paramref name="root"/> case-insensitively,
        /// file or directory, for as far as it exists. The part that does not exist is appended as
        /// written, so the result is also usable as a destination: new entries land in the existing
        /// folders rather than beside them in a second spelling. The caller checks existence.
        /// </summary>
        public static string ResolveCaseInsensitivePath(string root, string relativePath)
        {
            string current = root;

            var parts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                var match = Directory.Exists(current)
                    ? FindCaseInsensitiveMatch(Directory.GetFileSystemEntries(current), parts[i])
                    : null;

                if (match == null)
                    return Path.Combine(new[] { current }.Concat(parts.Skip(i)).ToArray());

                current = match;
            }

            return current;
        }

        /// <summary>
        /// Finds a directory or file in <paramref name="entries"/> matching <paramref name="name"/>
        /// using case-insensitive comparison. Returns absolute path or null.
        /// </summary>
        private static string? FindCaseInsensitiveMatch(string[] entries, string name)
        {
            // first try exact match
            var exact = entries
                .FirstOrDefault(e => Path.GetFileName(e) == name);

            if (exact != null)
                return exact;

            // then case-insensitive
            return entries
                .FirstOrDefault(e => 
                    string.Equals(Path.GetFileName(e), name, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        ///  Finds the full absolute path of a file/folder in a directory and resolve Directory override in xml
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string? ResolveExistingDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Replace("\\", "/");
            string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            string current = Path.GetPathRoot(path) ?? "/";

            // If no root (Linux relative path)
            if (string.IsNullOrEmpty(current) || current == "")
                current = "/";

            foreach (var part in parts)
            {
                var resolved = ResolveCaseInsensitiveDirectory(current, part);
                if (resolved == null)
                    return null;

                current = resolved;
            }

            return current;
        }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="baseDir"></param>
            /// <param name="relativePath"></param>
            /// <returns></returns>
            public static string? ResolvePathSegments(string baseDir, string relativePath)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return baseDir;

                string current = baseDir;
                var parts = relativePath.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    string? resolved = ResolveCaseInsensitiveDirectory(current, part);

                    if (resolved == null)
                        return null;

                    current = resolved;
                }

                return current;
            }

    }
}
