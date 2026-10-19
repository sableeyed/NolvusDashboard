using System.Security;
using System.Text;
using Nolvus.Package.Rules;
using Nolvus.Tests.Conditions;

namespace Nolvus.Tests.Rules
{
    public class CopyRuleTests
    {
        private static FileCopy Load(string Source, string DestinationDirectory = "", int Destination = 0, string NewFileName = "")
        {
            var Rule = new FileCopy();

            Rule.Load(Xml.Parse(
                "<Rule>" +
                $"<Source>{SecurityElement.Escape(Source)}</Source>" +
                $"<Destination>{Destination}</Destination>" +
                $"<DestinationDirectory>{SecurityElement.Escape(DestinationDirectory)}</DestinationDirectory>" +
                $"<NewFileName>{SecurityElement.Escape(NewFileName)}</NewFileName>" +
                "</Rule>"));

            return Rule;
        }

        /// <summary>What a UTF-8 name looks like after being read as Windows-1252.</summary>
        private static string Mangle(string Name)
        {
            return Encoding.GetEncoding(1252).GetString(Encoding.UTF8.GetBytes(Name));
        }

        [Fact]
        public void Mangle_ProducesTheMojibakeSeenInPackages()
        {
            // Sanity check on the helper, against the example in CopyRule's comment.
            Assert.Equal("æ—¥æœ¬èªž", Mangle("日本語"));
        }

        [Fact]
        public void Load_RepairsKnownMojibake()
        {
            // Written out rather than produced by Mangle, so this case does not lean on the helper.
            Assert.Equal("日本語/readme.txt", Load("æ—¥æœ¬èªž/readme.txt").Source);
        }

        [Theory]
        [InlineData("日本語/readme.txt")]
        [InlineData("Textures/Café/wall.dds")]
        [InlineData("Interface/Translations/SkyUI_RUSSIAN — Копия.txt")]
        [InlineData("Ñandú/señal.esp")]
        public void Load_RepairsUtf8ReadAsWindows1252(string Expected)
        {
            Assert.Equal(Expected, Load(Mangle(Expected)).Source);
        }

        [Theory]
        [InlineData("日本語/readme.txt")]           // real CJK cannot be encoded to 1252
        [InlineData("Textures/Café/wall.dds")]      // a real é is not valid UTF-8 on its own
        [InlineData("Meshes/Armor/Ébène.nif")]
        [InlineData("SKSE/Plugins/plain.dll")]      // ASCII is returned untouched
        public void Load_LeavesCorrectNamesAlone(string Name)
        {
            Assert.Equal(Name, Load(Name).Source);
        }

        [Fact]
        public void Load_LeavesPartlyMangledTextAlone()
        {
            // Mojibake next to a character 1252 cannot hold means it was not a clean 1252 misread.
            var Name = Mangle("日本") + "語";

            Assert.Equal(Name, Load(Name).Source);
        }

        [Fact]
        public void Load_RepairsDestinationDirectoryToo()
        {
            var Rule = Load("a.txt", DestinationDirectory: Mangle("Données\\Textes"));

            Assert.Equal("Données/Textes", Rule.DestinationDirectory);
        }

        [Theory]
        [InlineData("Seasons\\foo.ini", "Seasons/foo.ini")]
        [InlineData("\\SKSE\\Plugins\\x.dll", "SKSE/Plugins/x.dll")]
        [InlineData("//Data/x.esp", "Data/x.esp")]
        public void Load_NormalizesWindowsPaths(string Raw, string Expected)
        {
            Assert.Equal(Expected, Load(Raw).Source);
        }

        [Fact]
        public void Load_EmptyDestinationDirectoryMeansCopyToRoot()
        {
            Assert.True(Load("a.txt").CopyToRoot);
            Assert.False(Load("a.txt", DestinationDirectory: "SKSE").CopyToRoot);
        }

        [Fact]
        public void Execute_CopiesIntoModSubfolder()
        {
            using var Extract = new TempDir();
            using var Mod = new TempDir();

            Extract.File("Seasons/foo.ini", "content");

            Load("Seasons\\foo.ini", DestinationDirectory: "\\SKSE\\Plugins")
                .Execute(gamePath: "", extractDir: Extract.Path, modDir: Mod.Path, instanceDir: "");

            Assert.Equal("content", File.ReadAllText(Mod.Combine("SKSE", "Plugins", "foo.ini")));
        }

        [Fact]
        public void Execute_CopiesToRootAndRenames()
        {
            using var Extract = new TempDir();
            using var Mod = new TempDir();

            Extract.File("optional/foo.ini", "content");

            Load("optional/foo.ini", NewFileName: "bar.ini")
                .Execute("", Extract.Path, Mod.Path, "");

            Assert.True(File.Exists(Mod.Combine("bar.ini")));
            Assert.False(File.Exists(Mod.Combine("foo.ini")));
        }

        [Theory]
        [InlineData(0, "mod")]
        [InlineData(1, "game")]
        [InlineData(2, "instance")]
        public void Execute_PicksDestinationBase(int Destination, string ExpectedRoot)
        {
            using var Root = new TempDir();

            Root.File("extract/a.txt", "x");

            var Dirs = new Dictionary<string, string>
            {
                ["mod"] = Root.Dir("mod"),
                ["game"] = Root.Dir("game"),
                ["instance"] = Root.Dir("instance"),
            };

            Load("a.txt", Destination: Destination)
                .Execute(Dirs["game"], Root.Combine("extract"), Dirs["mod"], Dirs["instance"]);

            foreach (var (Name, Dir) in Dirs)
                Assert.Equal(Name == ExpectedRoot, File.Exists(Path.Combine(Dir, "a.txt")));
        }

        [Fact]
        public void Execute_OverwritesExistingFile()
        {
            using var Extract = new TempDir();
            using var Mod = new TempDir();

            Extract.File("a.txt", "new");
            Mod.File("a.txt", "old");

            Load("a.txt").Execute("", Extract.Path, Mod.Path, "");

            Assert.Equal("new", File.ReadAllText(Mod.Combine("a.txt")));
        }

        [Fact]
        public void Execute_SkipsMissingSource()
        {
            using var Extract = new TempDir();
            using var Mod = new TempDir();

            Load("missing.txt", DestinationDirectory: "Sub").Execute("", Extract.Path, Mod.Path, "");

            Assert.Empty(Directory.EnumerateFileSystemEntries(Mod.Path));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(99, false)]
        public void Execute_FollowsItsConditions(int SizeKb, bool Copied)
        {
            using var Game = new TempDir();
            using var Extract = new TempDir();
            using var Mod = new TempDir();
            Game.File("SkyrimSE.exe", new string('x', 1024));
            Extract.File("a.txt", "x");

            var Rule = new FileCopy();

            Rule.Load(Xml.Parse(
                "<Rule>" +
                $"<Conditions>{ConditionTests.SizeCondition("SkyrimSE.exe", 0, SizeKb, Source: 1)}</Conditions>" +
                "<Source>a.txt</Source><Destination>0</Destination>" +
                "</Rule>"));

            Rule.Execute(Game.Path, Extract.Path, Mod.Path, "");

            Assert.Equal(Copied, File.Exists(Mod.Combine("a.txt")));
        }
    }
}
