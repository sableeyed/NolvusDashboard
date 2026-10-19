using Nolvus.Package.Conditions;
using Nolvus.Package.Rules;

namespace Nolvus.Tests.Conditions
{
    public class ConditionTests
    {
        /// <summary>A FileSizeCondition as it appears in a package manifest.</summary>
        internal static string SizeCondition(string File, int Operator, int SizeKb, int Source) =>
            "<Condition>" +
            "<Type>FileSizeCondition</Type>" +
            $"<DataToCompare>{File}</DataToCompare>" +
            $"<Operator>{Operator}</Operator>" +
            $"<ValueToCompare>{SizeKb}</ValueToCompare>" +
            $"<Source>{Source}</Source>" +
            "</Condition>";

        private static RenameRule Rename(string OldName, string NewName, params string[] Conditions)
        {
            var Rule = new RenameRule();

            Rule.Load(Xml.Parse(
                "<Rule>" +
                $"<Conditions>{string.Concat(Conditions)}</Conditions>" +
                $"<OldName>{OldName}</OldName><NewName>{NewName}</NewName>" +
                "<Source>0</Source><IsDirectory>false</IsDirectory>" +
                "</Rule>"));

            return Rule;
        }

        [Fact]
        public void RuleLoad_ResolvesConditionTypesByName()
        {
            var Rule = Rename("a", "b", SizeCondition("SkyrimSE.exe", 0, 2, 1));

            var Condition = Assert.IsType<FileSizeCondition>(Assert.Single(Rule.Conditions));

            Assert.Equal("SkyrimSE.exe", Condition.DataToCompare);
            Assert.Equal(0, Condition.Operator);
            Assert.Equal("2", Condition.ValueToCompare);
            Assert.Equal(1, Condition.Source);
        }

        [Fact]
        public void RuleLoad_SkipsUnknownConditionTypes()
        {
            var Rule = Rename("a", "b",
                "<Condition><Type>NoSuchCondition</Type></Condition>",
                SizeCondition("SkyrimSE.exe", 0, 2, 1));

            Assert.IsType<FileSizeCondition>(Assert.Single(Rule.Conditions));
            Assert.Contains(TestEnvironment.Logger.Messages, x => x.Contains("NoSuchCondition"));
        }

        [Fact]
        public void RuleLoad_ReloadingClearsPreviousConditions()
        {
            var Rule = Rename("a", "b", SizeCondition("SkyrimSE.exe", 0, 2, 1));

            Rule.Load(Xml.Parse("<Rule><OldName>a</OldName><NewName>b</NewName></Rule>"));

            Assert.Empty(Rule.Conditions);
        }

        [Fact]
        public void RuleLoad_ReadsForce()
        {
            // Mod.cs orders rules by Force, so losing it changes what an install does.
            var Rule = new RenameRule();

            Rule.Load(Xml.Parse("<Rule><Force>true</Force><OldName>a</OldName><NewName>b</NewName></Rule>"));
            Assert.True(Rule.Force);

            Rule.Load(Xml.Parse("<Rule><OldName>a</OldName><NewName>b</NewName></Rule>"));
            Assert.False(Rule.Force);
        }

        [Fact]
        public void FileSizeCondition_UnknownOperatorFails()
        {
            using var Game = new TempDir();
            Game.File("SkyrimSE.exe", new string('x', 1024));

            var Rule = Rename("a", "b", SizeCondition("SkyrimSE.exe", 4, 1, Source: 1));

            Assert.False(Rule.Conditions[0].IsValid(Game.Path, "/nonexistent"));
        }

        [Theory]
        [InlineData(0, 2, true)]    // ==
        [InlineData(0, 3, false)]
        [InlineData(1, 3, true)]    // !=
        [InlineData(1, 2, false)]
        [InlineData(2, 1, true)]    // >
        [InlineData(2, 2, false)]
        [InlineData(3, 3, true)]    // <
        [InlineData(3, 2, false)]
        public void FileSizeCondition_ComparesSizeInKilobytes(int Operator, int SizeKb, bool Expected)
        {
            using var Game = new TempDir();
            Game.File("SkyrimSE.exe", new string('x', 2048 + 100));   // 2 KB after integer division

            var Rule = Rename("a", "b", SizeCondition("SkyrimSE.exe", Operator, SizeKb, Source: 1));

            Assert.Equal(Expected, Rule.Conditions[0].IsValid(Game.Path, "/nonexistent"));
        }

        [Fact]
        public void FileSizeCondition_SourceSelectsGameOrInstallFolder()
        {
            using var Game = new TempDir();
            using var Install = new TempDir();
            Game.File("a.bin", new string('x', 1024));
            Install.File("a.bin", new string('x', 4096));

            var FromGame = Rename("a", "b", SizeCondition("a.bin", 0, 1, Source: 1)).Conditions[0];
            var FromInstall = Rename("a", "b", SizeCondition("a.bin", 0, 4, Source: 0)).Conditions[0];

            Assert.True(FromGame.IsValid(Game.Path, Install.Path));
            Assert.True(FromInstall.IsValid(Game.Path, Install.Path));
        }

        [Fact]
        public void RuleExecute_SkipsWhenAnyConditionFails()
        {
            using var Game = new TempDir();
            using var Mod = new TempDir();
            Game.File("SkyrimSE.exe", new string('x', 1024));
            Mod.File("a.txt");

            // The first condition passes; the second alone has to stop the rule.
            Rename("a.txt", "b.txt",
                    SizeCondition("SkyrimSE.exe", 0, 1, Source: 1),
                    SizeCondition("SkyrimSE.exe", 0, 99, Source: 1))
                .Execute(Game.Path, "", Mod.Path, "");

            Assert.True(File.Exists(Mod.Combine("a.txt")));
            Assert.False(File.Exists(Mod.Combine("b.txt")));
        }

        [Fact]
        public void RuleExecute_RunsWhenAllConditionsPass()
        {
            using var Game = new TempDir();
            using var Mod = new TempDir();
            Game.File("SkyrimSE.exe", new string('x', 1024));
            Mod.File("a.txt");

            Rename("a.txt", "b.txt",
                    SizeCondition("SkyrimSE.exe", 0, 1, Source: 1),
                    SizeCondition("SkyrimSE.exe", 3, 5, Source: 1))
                .Execute(Game.Path, "", Mod.Path, "");

            Assert.True(File.Exists(Mod.Combine("b.txt")));
        }
    }
}
