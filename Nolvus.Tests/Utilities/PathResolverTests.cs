using Nolvus.Package.Utilities;

namespace Nolvus.Tests.Utilities
{
    public class PathResolverTests
    {
        [Fact]
        public void ResolveCaseInsensitivePath_FindsExistingFileInAnyCase()
        {
            using var Root = new TempDir();
            var Expected = Root.File("Textures/Terrain/Foo.DDS");

            Assert.Equal(Expected, PathResolver.ResolveCaseInsensitivePath(Root.Path, "textures/TERRAIN/foo.dds"));
        }

        [Fact]
        public void ResolveCaseInsensitivePath_AcceptsBackslashes()
        {
            using var Root = new TempDir();
            var Expected = Root.File("Meshes/Armor/a.nif");

            Assert.Equal(Expected, PathResolver.ResolveCaseInsensitivePath(Root.Path, "meshes\\armor\\A.nif"));
        }

        [Fact]
        public void ResolveCaseInsensitivePath_AppendsMissingTailToExistingSpelling()
        {
            using var Root = new TempDir();
            Root.Dir("Textures");

            // New entries must land in the existing Textures, not beside it in a second spelling.
            Assert.Equal(Root.Combine("Textures", "new", "bar.dds"),
                PathResolver.ResolveCaseInsensitivePath(Root.Path, "textures/new/bar.dds"));
        }

        [Fact]
        public void ResolveCaseInsensitivePath_ReturnsPathAsWrittenWhenNothingExists()
        {
            using var Root = new TempDir();

            Assert.Equal(Root.Combine("A", "b.txt"), PathResolver.ResolveCaseInsensitivePath(Root.Path, "A/b.txt"));
        }

        [Fact]
        public void ResolveCaseInsensitivePath_MissingRootReturnsPathAsWritten()
        {
            // A rule pointed at a game folder that is not there should report the file missing,
            // not crash the install.
            using var Temp = new TempDir();
            var Missing = Temp.Combine("does-not-exist");

            Assert.Equal(Path.Combine(Missing, "Data", "Skyrim.esm"),
                PathResolver.ResolveCaseInsensitivePath(Missing, "Data\\Skyrim.esm"));
        }

        [Fact]
        public void ResolveCaseInsensitivePath_PrefersExactMatchWhenBothSpellingsExist()
        {
            using var Root = new TempDir();
            var Upper = Root.Dir("Data");
            var Lower = Root.Dir("data");

            Assert.Equal(Upper, PathResolver.ResolveCaseInsensitivePath(Root.Path, "Data"));
            Assert.Equal(Lower, PathResolver.ResolveCaseInsensitivePath(Root.Path, "data"));
        }

        [Fact]
        public void ResolveCaseInsensitiveDirectory_FindsNestedDirectory()
        {
            using var Root = new TempDir();
            var Expected = Root.Dir("SKSE/Plugins");

            Assert.Equal(Expected, PathResolver.ResolveCaseInsensitiveDirectory(Root.Path, "skse\\plugins"));
        }

        [Fact]
        public void ResolveCaseInsensitiveDirectory_IgnoresFiles()
        {
            using var Root = new TempDir();
            Root.File("Plugins");

            Assert.Null(PathResolver.ResolveCaseInsensitiveDirectory(Root.Path, "plugins"));
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveCaseInsensitiveDirectory_ReturnsNullWhenNotFound(string Relative)
        {
            using var Root = new TempDir();

            Assert.Null(PathResolver.ResolveCaseInsensitiveDirectory(Root.Path, Relative));
        }

        [Fact]
        public void ResolveCaseInsensitiveFile_FindsFileInAnyCase()
        {
            using var Root = new TempDir();
            var Expected = Root.File("SkyrimSE.exe");

            Assert.Equal(Expected, PathResolver.ResolveCaseInsensitiveFile(Root.Path, "skyrimse.EXE"));
            Assert.Null(PathResolver.ResolveCaseInsensitiveFile(Root.Path, "missing.exe"));
            Assert.Null(PathResolver.ResolveCaseInsensitiveFile(Root.Combine("nope"), "SkyrimSE.exe"));
        }

        [Fact]
        public void ResolvePathSegments_ReturnsBaseForEmptyPath()
        {
            using var Root = new TempDir();

            Assert.Equal(Root.Path, PathResolver.ResolvePathSegments(Root.Path, ""));
        }

        [Fact]
        public void ResolvePathSegments_ResolvesOrReturnsNull()
        {
            using var Root = new TempDir();
            var Expected = Root.Dir("Interface/Translations");

            Assert.Equal(Expected, PathResolver.ResolvePathSegments(Root.Path, "interface\\translations"));
            Assert.Null(PathResolver.ResolvePathSegments(Root.Path, "interface/missing"));
        }

        [Fact]
        public void ResolveExistingDirectory_ResolvesAbsolutePathInAnyCase()
        {
            using var Root = new TempDir();
            var Expected = Root.Dir("Stock Game/Data");

            Assert.Equal(Expected, PathResolver.ResolveExistingDirectory(Root.Combine("STOCK GAME", "data")));
            Assert.Null(PathResolver.ResolveExistingDirectory(Root.Combine("Stock Game", "missing")));
        }
    }
}
