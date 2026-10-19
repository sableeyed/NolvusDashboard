using Nolvus.Services.Files.Extractor;

namespace Nolvus.Tests.Files
{
    /// <summary>
    /// 7z on Linux extracts Windows-made zips with backslash paths as flat files whose names
    /// contain backslashes. These build that output by hand.
    /// </summary>
    public class SplitBackslashPathsTests
    {
        /// <summary>
        /// Everything under Root, relative and sorted: folders end in /, files carry their contents.
        /// Comparing whole trees also catches anything left behind or created by mistake.
        /// </summary>
        private static string[] Tree(string Root) =>
            Directory.EnumerateFileSystemEntries(Root, "*", SearchOption.AllDirectories)
                .Select(x => Path.GetRelativePath(Root, x) + (Directory.Exists(x) ? "/" : " = " + File.ReadAllText(x)))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

        [Fact]
        public void MovesBackslashNamedFilesIntoFolders()
        {
            using var Root = new TempDir();
            Root.File("Seasons\\foo.ini", "foo");
            Root.File("Seasons\\Winter\\bar.ini", "bar");

            FileExtractor.SplitBackslashPaths(Root.Path);

            Assert.Equal(new[]
            {
                "Seasons/",
                "Seasons/Winter/",
                "Seasons/Winter/bar.ini = bar",
                "Seasons/foo.ini = foo",
            }, Tree(Root.Path));
        }

        [Fact]
        public void RemovesEmptyBackslashDirectoryEntries()
        {
            using var Root = new TempDir();
            Root.Dir("Seasons\\");
            Root.Dir("Seasons\\Winter\\");
            Root.File("Seasons\\Winter\\bar.ini", "bar");

            FileExtractor.SplitBackslashPaths(Root.Path);

            Assert.Equal(new[] { "Seasons/", "Seasons/Winter/", "Seasons/Winter/bar.ini = bar" }, Tree(Root.Path));
        }

        [Fact]
        public void RemovesNestedBackslashDirectoriesDeepestFirst()
        {
            // A backslash-named folder inside another: the outer one is only empty once the inner
            // one is gone, which is why the cleanup works deepest first.
            using var Root = new TempDir();
            Root.Dir("Outer\\/Inner\\");
            Root.File("Seasons\\foo.ini", "foo");

            FileExtractor.SplitBackslashPaths(Root.Path);

            Assert.Equal(new[] { "Seasons/", "Seasons/foo.ini = foo" }, Tree(Root.Path));
        }

        [Fact]
        public void HandlesBackslashNamesInsideRealFolders()
        {
            using var Root = new TempDir();
            Root.File("meshes/armor\\iron\\cuirass.nif", "nif");

            FileExtractor.SplitBackslashPaths(Root.Path);

            Assert.Equal(new[]
            {
                "meshes/",
                "meshes/armor/",
                "meshes/armor/iron/",
                "meshes/armor/iron/cuirass.nif = nif",
            }, Tree(Root.Path));
        }

        [Fact]
        public void OverwritesFileAlreadyAtTarget()
        {
            using var Root = new TempDir();
            Root.File("Seasons/foo.ini", "old");
            Root.File("Seasons\\foo.ini", "new");

            FileExtractor.SplitBackslashPaths(Root.Path);

            Assert.Equal(new[] { "Seasons/", "Seasons/foo.ini = new" }, Tree(Root.Path));
        }

        [Fact]
        public void LeavesNormalArchivesAlone()
        {
            using var Root = new TempDir();
            Root.File("SKSE/Plugins/a.dll", "a");
            Root.File("readme.txt", "r");
            var Before = Tree(Root.Path);

            FileExtractor.SplitBackslashPaths(Root.Path);

            Assert.Equal(Before, Tree(Root.Path));
        }
    }
}
