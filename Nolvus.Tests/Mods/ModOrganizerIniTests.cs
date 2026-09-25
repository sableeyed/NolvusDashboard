using System.Diagnostics;
using Nolvus.Package.Mods;

namespace Nolvus.Tests.Mods
{
    public class ModOrganizerIniTests
    {
        [Theory]
        [InlineData("/home/user/Nolvus/Instances/Nolvus Awakening", @"Z:\home\user\Nolvus\Instances\Nolvus Awakening")]
        [InlineData("/home/user//Nolvus///Instances/", @"Z:\home\user\Nolvus\Instances")]
        [InlineData(@"\home\user\Nolvus", @"Z:\home\user\Nolvus")]
        [InlineData("  /mnt/games/Nolvus  ", @"Z:\mnt\games\Nolvus")]
        [InlineData("MODS/profiles", @"MODS\profiles")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void ToWinePath_MapsLinuxPathsOntoZ(string Linux, string Expected)
        {
            Assert.Equal(Expected, ModOrganizer.ToWinePath(Linux));
        }

        [Fact]
        public void ToWineIniPath_DoublesBackslashesForTheIni()
        {
            Assert.Equal(@"Z:\\home\\user\\Nolvus", ModOrganizer.ToWineIniPath("/home/user/Nolvus"));
        }

        /// <summary>A Nolvus root with an instance under Instances/, the layout the dashboard creates.</summary>
        private sealed class Instance : IDisposable
        {
            private readonly TempDir Temp = new TempDir();

            public string InstallDir => Temp.Combine("Nolvus", "Instances", "Nolvus Awakening");
            public string StockGame => Path.Combine(InstallDir, "STOCK GAME");
            public string Mo2Ini => Path.Combine(InstallDir, "MO2", "ModOrganizer.ini");
            public string ModsIni => Path.Combine(InstallDir, "MODS", "ModOrganizer.ini");

            public Instance(string? Mo2Ini = null, bool CreateMods = true)
            {
                if (Mo2Ini != null)
                    Write(this.Mo2Ini, Mo2Ini);

                if (CreateMods)
                    Directory.CreateDirectory(Path.Combine(InstallDir, "MODS"));
            }

            public void Write(string File, string Content)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(File)!);
                System.IO.File.WriteAllText(File, Content);
            }

            public void Dispose() => Temp.Dispose();
        }

        /// <summary>
        /// The Linux path an ini value like Z:\\home\\user\\... names under Wine, where Z: is /.
        /// Worked out here rather than with ModOrganizer's own helpers, so a test checks that a
        /// rewritten path leads back to the folder it should instead of re-running the formula
        /// that produced it. Fails on a lone backslash, since the ini escapes every one.
        /// </summary>
        private static string FromIniPath(string Value)
        {
            Assert.StartsWith(@"Z:\\", Value);

            var Linux = Value.Substring(2).Replace(@"\\", "/");

            Assert.DoesNotContain(@"\", Linux);

            return Linux;
        }

        /// <summary>The same for the Z:/home/user/... spelling binary= entries use.</summary>
        private static string FromForwardIniPath(string Value)
        {
            Assert.StartsWith("Z:/", Value);

            return Value.Substring(2);
        }

        /// <summary>The value of Key in ini text, unwrapped from @ByteArray(...) when it has one.</summary>
        private static string ValueOf(string Text, string Key)
        {
            var Value = Text.Split('\n')
                .Select(x => x.TrimEnd('\r'))
                .Single(x => x.StartsWith(Key + "="))
                .Substring(Key.Length + 1);

            return Value.StartsWith("@ByteArray(") && Value.EndsWith(")") ? Value["@ByteArray(".Length..^1] : Value;
        }

        private static string Lines(params string[] Lines) => string.Join("\r\n", Lines);

        private static readonly string OldMo2Ini = Lines(
            "[General]",
            "gameName=Skyrim Special Edition",
            @"gamePath=@ByteArray(X:\\Instances\\Nolvus Awakening\\STOCK GAME)",
            @"base_directory=X:\\Instances\\Nolvus Awakening\\MODS",
            "",
            "[customExecutables]",
            "size=2",
            "1\\binary=X:/Instances/Nolvus Awakening/STOCK GAME/skse64_loader.exe",
            @"2\arguments=-D:\""X:\\Instances\\Nolvus Awakening\\STOCK GAME\\Data\""",
            "");

        [Fact]
        public void EnsureInstanceIni_ConvertsXPathsToZ()
        {
            using var Inst = new Instance(OldMo2Ini);

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            var Text = File.ReadAllText(Inst.ModsIni);

            // X: stood for the folder holding Instances, so every converted path has to lead back
            // to the real folder or file under this instance.
            Assert.DoesNotContain("X:", Text);
            Assert.Equal(Inst.StockGame, FromIniPath(ValueOf(Text, "gamePath")));
            Assert.Equal(Path.Combine(Inst.StockGame, "skse64_loader.exe"), FromForwardIniPath(ValueOf(Text, "1\\binary")));

            // xEdit's argument keeps its quotes: -D:\"<path>\"
            var Arguments = ValueOf(Text, "2\\arguments");

            Assert.StartsWith("-D:\\\"", Arguments);
            Assert.EndsWith("\\\"", Arguments);
            Assert.Equal(Path.Combine(Inst.StockGame, "Data"), FromIniPath(Arguments[5..^2]));
        }

        [Fact]
        public void EnsureInstanceIni_DropsBaseDirectoryAndAddsGameEditionAfterGamePath()
        {
            using var Inst = new Instance(OldMo2Ini);

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            var Result = File.ReadAllText(Inst.ModsIni).Split("\r\n");
            var GamePath = Array.FindIndex(Result, x => x.StartsWith("gamePath="));

            Assert.DoesNotContain(Result, x => x.StartsWith("base_directory"));
            Assert.Equal("game_edition=Steam", Result[GamePath + 1]);
        }

        [Fact]
        public void EnsureInstanceIni_EditsLfOnlyIni()
        {
            using var Inst = new Instance(OldMo2Ini.Replace("\r\n", "\n"));

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            var Text = File.ReadAllText(Inst.ModsIni);
            var Result = Text.Split('\n');
            var GamePath = Array.FindIndex(Result, x => x.StartsWith("gamePath="));

            Assert.DoesNotContain("\r\n", Text);
            Assert.DoesNotContain(Result, x => x.StartsWith("base_directory"));
            Assert.Equal("game_edition=Steam", Result[GamePath + 1]);
        }

        [Fact]
        public void EnsureInstanceIni_OutsideInstancesFolderMapsXToRoot()
        {
            // The MO2-era dashboard mapped X: to the filesystem root when an instance was not
            // inside an Instances folder, so its ini holds whole paths: X:\\home\\...\\STOCK GAME.
            using var Temp = new TempDir();
            var InstallDir = Temp.Dir("MyInstance");
            var StockGame = Path.Combine(InstallDir, "STOCK GAME");
            var Mo2Ini = Path.Combine(InstallDir, "MO2", "ModOrganizer.ini");
            var ModsIni = Path.Combine(InstallDir, "MODS", "ModOrganizer.ini");

            Directory.CreateDirectory(Path.GetDirectoryName(Mo2Ini)!);
            Directory.CreateDirectory(Path.GetDirectoryName(ModsIni)!);
            File.WriteAllText(Mo2Ini, Lines("[General]", $"gamePath=@ByteArray(X:{StockGame.Replace("/", @"\\")})"));

            ModOrganizer.EnsureInstanceIni(InstallDir);

            Assert.Equal(StockGame, FromIniPath(ValueOf(File.ReadAllText(ModsIni), "gamePath")));
        }

        [Fact]
        public void EnsureInstanceIni_KeepsExistingGameEditionAndLineEndings()
        {
            using var Inst = new Instance("[General]\ngame_edition=GOG\ngamePath=@ByteArray(X:\\\\Instances)\n");

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            var Text = File.ReadAllText(Inst.ModsIni);

            Assert.Single(Text.Split('\n'), x => x.StartsWith("game_edition"));
            Assert.Contains("game_edition=GOG", Text);
            Assert.DoesNotContain("\r\n", Text);
        }

        [Fact]
        public void EnsureInstanceIni_KeepsCrlfLineEndings()
        {
            using var Inst = new Instance(OldMo2Ini);

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            Assert.DoesNotContain("\n", File.ReadAllText(Inst.ModsIni).Replace("\r\n", ""));
        }

        [Fact]
        public void EnsureInstanceIni_DoesNotOverwriteExistingModsIni()
        {
            using var Inst = new Instance(OldMo2Ini);
            Inst.Write(Inst.ModsIni, "fluorine's own");

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            Assert.Equal("fluorine's own", File.ReadAllText(Inst.ModsIni));
        }

        [Fact]
        public void EnsureInstanceIni_DoesNothingWithoutMo2IniOrModsFolder()
        {
            using (var Inst = new Instance())
            {
                ModOrganizer.EnsureInstanceIni(Inst.InstallDir);
                Assert.False(File.Exists(Inst.ModsIni));
            }

            using (var Inst = new Instance(OldMo2Ini, CreateMods: false))
            {
                ModOrganizer.EnsureInstanceIni(Inst.InstallDir);
                Assert.False(Directory.Exists(Path.GetDirectoryName(Inst.ModsIni)));
            }
        }

        /// <summary>
        /// A process Fluorine.IsRunning takes for a running Fluorine: it looks for a command line
        /// containing ModOrganizer-core under Fluorine's install directory. TestEnvironment points
        /// that directory at a temp folder, so a real Fluorine on this machine never counts.
        /// </summary>
        private sealed class FakeFluorine : IDisposable
        {
            private readonly Process Shell;

            public FakeFluorine()
            {
                var Core = Path.Combine(Fluorine.InstallDirectory, "ModOrganizer-core");

                Directory.CreateDirectory(Fluorine.InstallDirectory);

                // sleep is not the last command, so sh stays in the process table under this
                // script's path instead of exec'ing into sleep.
                File.WriteAllText(Core, "#!/bin/sh\nsleep 60\ntrue\n");
                File.SetUnixFileMode(Core, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

                Shell = Process.Start(new ProcessStartInfo(Core) { UseShellExecute = false })!;

                // Process.Start returns as soon as the exec has happened, but the kernel fills in
                // /proc/<pid>/cmdline, which pgrep -f reads, a moment later. Checking straight away
                // saw an empty command line in about a third of starts.
                var Deadline = DateTime.UtcNow.AddSeconds(5);

                while (!File.ReadAllText($"/proc/{Shell.Id}/cmdline").Contains(Core) && DateTime.UtcNow < Deadline)
                    Thread.Sleep(1);
            }

            public void Dispose()
            {
                Shell.Kill(entireProcessTree: true);
                Shell.WaitForExit();
                Shell.Dispose();
            }
        }

        [Fact]
        public void IniFixesWaitWhileFluorineIsRunning()
        {
            // Fluorine re-saves MODS/ModOrganizer.ini when it exits, so anything written while it
            // is open would be overwritten.
            using var Inst = new Instance(OldMo2Ini);
            using var Running = new FakeFluorine();

            Assert.True(Fluorine.IsRunning, "the stand-in Fluorine process was not detected");

            ModOrganizer.EnsureInstanceIni(Inst.InstallDir);

            Assert.False(File.Exists(Inst.ModsIni));

            var Original = @"download_directory=X:\\ARCHIVE";
            Inst.Write(Inst.ModsIni, Original);

            ModOrganizer.RepairIniDriveLetters(Inst.InstallDir);

            Assert.Equal(Original, File.ReadAllText(Inst.ModsIni));
        }

        [Fact]
        public void RepairIniDriveLetters_PointsXAtThisInstance()
        {
            using var Inst = new Instance();
            Inst.Write(Inst.ModsIni, Lines(
                "[General]",
                @"gamePath=@ByteArray(X:\\STOCK GAME)",
                @"download_directory=X:\\ARCHIVE",
                "1\\binary=X:/STOCK GAME/skse64_loader.exe"));

            ModOrganizer.RepairIniDriveLetters(Inst.InstallDir);

            var Text = File.ReadAllText(Inst.ModsIni);

            // In these inis X: meant the instance folder itself.
            Assert.Equal(Inst.StockGame, FromIniPath(ValueOf(Text, "gamePath")));
            Assert.Equal(Path.Combine(Inst.InstallDir, "ARCHIVE"), FromIniPath(ValueOf(Text, "download_directory")));
            Assert.Equal(Path.Combine(Inst.StockGame, "skse64_loader.exe"), FromForwardIniPath(ValueOf(Text, "1\\binary")));
        }

        [Fact]
        public void RepairIniDriveLetters_OnlyTouchesXAtStartOfValue()
        {
            using var Inst = new Instance();
            var Original = Lines(
                "[Settings]",
                @"1\name=Backup of X:\\old stuff",
                @"2\arguments=-D:\""X:\\STOCK GAME\\Data\""");
            Inst.Write(Inst.ModsIni, Original);

            ModOrganizer.RepairIniDriveLetters(Inst.InstallDir);

            Assert.Equal(Original, File.ReadAllText(Inst.ModsIni));
        }

        [Fact]
        public void RepairIniDriveLetters_DollarInInstallPathIsNotASubstitution()
        {
            using var Temp = new TempDir();
            var InstallDir = Temp.Dir("Instances/Cost$1 $0 Build");
            var Ini = Path.Combine(InstallDir, "MODS", "ModOrganizer.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(Ini)!);
            File.WriteAllText(Ini, @"download_directory=X:\\ARCHIVE");

            ModOrganizer.RepairIniDriveLetters(InstallDir);

            Assert.Equal(Path.Combine(InstallDir, "ARCHIVE"), FromIniPath(ValueOf(File.ReadAllText(Ini), "download_directory")));
        }

        [Fact]
        public void RepairIniDriveLetters_MissingIniIsIgnored()
        {
            using var Inst = new Instance(CreateMods: false);

            ModOrganizer.RepairIniDriveLetters(Inst.InstallDir);

            Assert.False(File.Exists(Inst.ModsIni));
        }
    }
}
