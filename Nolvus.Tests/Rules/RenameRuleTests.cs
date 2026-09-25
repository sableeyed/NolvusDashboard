using System.Security;
using Nolvus.Package.Rules;

namespace Nolvus.Tests.Rules
{
    public class RenameRuleTests
    {
        private static RenameRule Load(string OldName, string NewName, bool IsDirectory = false, int Source = 0)
        {
            var Rule = new RenameRule();

            Rule.Load(Xml.Parse(
                "<Rule>" +
                $"<OldName>{SecurityElement.Escape(OldName)}</OldName>" +
                $"<NewName>{SecurityElement.Escape(NewName)}</NewName>" +
                $"<Source>{Source}</Source>" +
                $"<IsDirectory>{IsDirectory.ToString().ToLower()}</IsDirectory>" +
                "</Rule>"));

            return Rule;
        }

        [Fact]
        public void Load_NormalizesPaths()
        {
            var Rule = Load("\\Textures\\a.dds", "\\Textures\\b.dds", IsDirectory: true, Source: 1);

            Assert.Equal("Textures/a.dds", Rule.OldName);
            Assert.Equal("Textures/b.dds", Rule.NewName);
            Assert.True(Rule.IsDirectory);
            Assert.Equal(1, Rule.Source);
        }

        [Fact]
        public void Execute_RenamesFileWhoseCaseDiffersOnDisk()
        {
            using var Mod = new TempDir();
            Mod.File("Textures/Terrain/Old.dds", "x");

            Load("textures\\terrain\\old.dds", "textures\\terrain\\new.dds").Execute("", "", Mod.Path, "");

            // Lands in the archive's own Textures/Terrain, not a second lower-case tree.
            Assert.True(File.Exists(Mod.Combine("Textures", "Terrain", "new.dds")));
            Assert.False(File.Exists(Mod.Combine("Textures", "Terrain", "Old.dds")));
            Assert.False(Directory.Exists(Mod.Combine("textures")));
        }

        [Fact]
        public void Execute_CaseOnlyRenameKeepsTheFile()
        {
            using var Mod = new TempDir();
            Mod.File("Readme.txt", "keep me");

            // Both names resolve to the same entry; deleting the "existing destination" would
            // delete the source.
            Load("Readme.txt", "README.TXT").Execute("", "", Mod.Path, "");

            Assert.Equal("keep me", File.ReadAllText(Mod.Combine("Readme.txt")));
        }

        [Fact]
        public void Execute_ReplacesExistingDestinationFile()
        {
            using var Mod = new TempDir();
            Mod.File("a.ini", "new");
            Mod.File("b.ini", "old");

            Load("a.ini", "b.ini").Execute("", "", Mod.Path, "");

            Assert.False(File.Exists(Mod.Combine("a.ini")));
            Assert.Equal("new", File.ReadAllText(Mod.Combine("b.ini")));
        }

        [Fact]
        public void Execute_RenamesDirectoryAndReplacesExistingOne()
        {
            using var Mod = new TempDir();
            Mod.File("Option A/file.txt", "a");
            Mod.File("Data/stale.txt", "stale");

            Load("option a", "Data", IsDirectory: true).Execute("", "", Mod.Path, "");

            Assert.False(Directory.Exists(Mod.Combine("Option A")));
            Assert.True(File.Exists(Mod.Combine("Data", "file.txt")));
            Assert.False(File.Exists(Mod.Combine("Data", "stale.txt")));
        }

        [Fact]
        public void Execute_CreatesMissingDestinationFolders()
        {
            using var Mod = new TempDir();
            Mod.File("a.txt");

            Load("a.txt", "Sub/Deeper/b.txt").Execute("", "", Mod.Path, "");

            Assert.True(File.Exists(Mod.Combine("Sub", "Deeper", "b.txt")));
        }

        [Fact]
        public void Execute_MissingSourceDoesNothing()
        {
            using var Mod = new TempDir();
            Mod.File("b.txt", "untouched");

            Load("a.txt", "b.txt").Execute("", "", Mod.Path, "");

            Assert.Equal("untouched", File.ReadAllText(Mod.Combine("b.txt")));
        }

        [Fact]
        public void Execute_Source1WorksInGameFolder()
        {
            using var Game = new TempDir();
            using var Mod = new TempDir();
            Game.File("Data/Skyrim.ccc");
            Mod.File("Data/Skyrim.ccc");

            Load("Data/Skyrim.ccc", "Data/Skyrim.ccc.bak", Source: 1).Execute(Game.Path, "", Mod.Path, "");

            Assert.True(File.Exists(Game.Combine("Data", "Skyrim.ccc.bak")));
            Assert.True(File.Exists(Mod.Combine("Data", "Skyrim.ccc")));
        }
    }
}
