# AGENTS.md

This file provides guidance to AI Agents when working with code in this repository.

## What this is

A Linux port of the official Windows [Nolvus Dashboard](https://github.com/vektor9999/NolvusDashboard), the installer and manager for the Nolvus Skyrim modlist. It is .NET 10, with the upstream WinForms UI rebuilt in Avalonia. The guiding rule is **upstream parity**: keep the upstream code and structure, and change only what Linux needs. See "The Upstream Parity Principle" in [CONTRIBUTING.md](CONTRIBUTING.md), and read it before making non-trivial changes.

## Commands

```bash
dotnet build "Nolvus Dashboard Linux.sln"                        # build everything
dotnet run --project Nolvus.Dashboard/Nolvus.Dashboard.csproj    # run from source (add -- --debugging for Program.DebugMode)
dotnet test Nolvus.Tests                                         # xUnit suite, a few seconds
dotnet test Nolvus.Tests --filter "FullyQualifiedName~CopyRuleTests"   # one class (or ~Class.Method)
Nolvus.Tests/stryker.sh                                          # mutation testing of Nolvus.Package (Stryker.NET via dotnet tool restore)
./build.sh                                                       # release tarball -> ~/Desktop/Binaries-<version>.tar.gz
```

CI ([.github/workflows/build-and-test.yml](.github/workflows/build-and-test.yml)) builds the Dashboard and Updater with `-c Release -r linux-x64`, then runs the tests. Some Avalonia XAML compile errors only show up in that Release build, so run it when touching `.axaml`:
`dotnet build Nolvus.Dashboard/Nolvus.Dashboard.csproj -c Release -r linux-x64`.

The build already has about 1000 warnings, most of them inherited from upstream. Don't add new ones, but don't clean up the existing ones either, because that would break parity.

The version lives in `<Version>`/`<AssemblyVersion>` in [Nolvus.Dashboard/Nolvus.Dashboard.csproj](Nolvus.Dashboard/Nolvus.Dashboard.csproj). `build.sh` reads it from there. The version matches the upstream release whose changes have been ported (currently 3.8.12). There is no upstream git remote, and the upstream UI is WinForms, so upstream releases are ported by hand in commits named like `Update 3.8.12 (n)`.

## Architecture

**Service locator.** `Program.Main` registers each service implementation into `ServiceSingleton` ([Nolvus.Core/Services/ServiceSingleton.cs](Nolvus.Core/Services/ServiceSingleton.cs)), and code everywhere reaches them through static accessors (`ServiceSingleton.Folders`, `.Packages`, `.Instances`, `.Logger`, `.Dashboard`, …). There is no DI container. An unregistered service resolves to `null`, which is why tests register a logger in `TestEnvironment`'s module initializer. Interfaces live in `Nolvus.Core/Interfaces` and implementations in `Nolvus.Services`, `Nolvus.Package` and `Nolvus.Instance`.

**UI = frames.** `DashboardWindow` hosts a single content area. Every screen is a `DashboardFrame` subclass ([Nolvus.Core/Frames/DashboardFrame.cs](Nolvus.Core/Frames/DashboardFrame.cs)), and navigation goes through `ServiceSingleton.Dashboard.LoadFrame<T>()` / `LoadFrameAsync<T>()` with `FrameParameters`. Frame names mirror the upstream WinForms frames. Screens that differ between Nolvus modlist versions are split into `v5/` and `v6/` namespaces, and the caller chooses one based on the instance version.

**Install pipeline** (in `Nolvus.Package`):
1. `PackageService.Load` downloads and extracts the modlist's `InstallPackage.xml` manifest and turns it into `InstallableElement`s (`Software`, `Category` → `Mod`). Each element then loads its own `ModFile`s, `Rule`s and `InstallCondition`s.
2. **Manifest types are instantiated by reflection from their names**, e.g. `Type.GetType("Nolvus.Package.Rules." + node["Type"])`. The same applies to the `Mods`, `Rules`, `Conditions` and `Files` namespaces. Renaming or moving those classes breaks installs at runtime and the compiler won't catch it.
3. `PackageService.InstallModList` runs elements in parallel, limited by a `SemaphoreSlim` set to `Settings.ProcessCount`. Each element goes through `Install`: Download → Extract → Unpack (BSA) → Copy (rules) → Patch → Archive.
4. Free Nexus accounts need a manual download link, which they get through an embedded CefGlue browser window (`Nolvus.Browser`, with JS helpers in `Nolvus.Browser/Scripts`). `BrowserGate` makes sure only one browser window is open at a time. Hold it only around the browser interaction itself, never around download or extraction.

**Mod manager: Fluorine, not Windows MO2.** Instances are launched through Fluorine, a native Linux build of MO2 installed at `$XDG_DATA_HOME/fluorine/bin` ([Nolvus.Package/Mods/Fluorine.cs](Nolvus.Package/Mods/Fluorine.cs)). `FluorineManager` is added to the install as its own element, so a failure there can't block MO2. The Windows MO2 files are still installed into each instance's `MO2/` folder. MO2-only upstream changes should still be carried over. A feature that Fluorine makes redundant, such as CRDW caching, should still be set up for MO2 rather than dropped. MO2 under Wine is not actively supported, though, so don't spend effort testing or polishing MO2-only paths. Fluorine handles Wine/Proton prefix setup itself; the old protontricks-based prefix code was removed on purpose, so don't bring it back. `ModOrganizer.cs` holds the Skyrim/MO2 ini templates plus the Linux handling for ini files and `Mod Organizer.conf`.

More Fluorine behavior that is intended:
- MO2 Python plugins (`Fluorine.InstallPlugins`, e.g. `crdw_auto_mode`) are copied into Fluorine's `bin/plugins` **only when Play is pressed**, from `InstancePanel` / `InstanceDetailFrame`. Right after an install they aren't there yet, and that is expected.
- `Fluorine.IsInstalled` checks for `bin/fluorine-manager` or the dashboard's `nolvus-fluorine-release.txt` marker.
- The released Fluorine launcher installs itself into that same `bin` folder when it runs and writes `bin/.version`, which the dashboard doesn't read. The launcher's real behavior is defined in `docker/build-inner.sh` in [SulfurNitride/Fluorine-Manager](https://github.com/SulfurNitride/Fluorine-Manager), not in its `src/fluorine-manager` wrapper.

**Runtime layout.** Everything resolves relative to the executable (`AppContext.BaseDirectory`): `NolvusDashboard.ini` (settings and account credentials), `Cache/` (downloads, extraction, CEF web cache), `Instances/InstancesData.xml`, and `lib/`. **`lib/` (7-Zip, xdelta3, BSArch, binary patches) is not in the repository.** It only ships with releases, so tests and CI must never run those tools. Test the logic around them instead; `SplitBackslashPathsTests` is an example.

**Updater.** `Nolvus.Updater` is a separate small Avalonia app. `build.sh` merges it into the same output folder as the dashboard. The Nolvus game launcher ships as the embedded resource `Nolvus.Dashboard/Resources/NolvusLauncher.exe` (CONTRIBUTING.md lists a `Nolvus.Launcher` project, but no such project exists).

## Linux-specific gotchas

- **CEF breaks child-process waits.** CEF resets the runtime's SIGCHLD handler, so every `Process.WaitForExit` / `WaitForExitAsync` (on 7z, xdelta3, BSArch, …) then hangs forever without any error. `Process.HasExited` blocks the same way, because it doesn't collect the exited child either, so polling it is no workaround. `SigChldGuard` ([Nolvus.Core/Utils/SigChldGuard.cs](Nolvus.Core/Utils/SigChldGuard.cs)) is installed before CEF starts and restores the handler from one central place. Don't add `waitpid` P/Invokes or per-call-site workarounds. If you suspect a hang of this kind, reproduce it in a standalone program before changing anything.
- **Package paths are Windows-shaped.** Manifests use backslashes and assume a case-insensitive filesystem. Use `PathResolver` for case-insensitive lookups. `FileExtractor.SplitBackslashPaths` repairs archives that extracted `a\b.txt` as one literal filename. `CopyRule` repairs file names that were UTF-8 but got read as Windows-1252, which needs the `CodePagesEncodingProvider` registered in `Program.Main`.
- **Paths written for Wine/Fluorine** go through `ModOrganizer.ToWinePath` / `ToWineIniPath`, which map them to `Z:\…`. Ini files are written with CRLF line endings.
- **`build.sh` changes the published output** in ways `dotnet build` never does. It strips the bundled .NET runtime out of `CefGlueBrowserProcess/` and replaces its apphost with a launcher script, deletes `.pdb` files and QuestPDF's fonts. So the repo's `bin/` output differs from what actually ships. If you touch `Nolvus.Browser` or bump CefGlue, check that `build.sh` still finds the subprocess.
- External tools are found via `PATH` (`xrandr`, `glxinfo`, `lspci`, `tar`, `pgrep`). `ExecutableResolver` is available for this.

## Conventions

- **Fill in upstream's existing seams.** Upstream code on this port has empty stubs, and methods that look unreferenced, e.g. `ModOrganizer.ModifyExecutable` exists for the stubbed `RemapInstanceFrame.ModifyMO2Executables()`. New logic belongs in those stubs, not in a new entry point next to them. Don't delete such methods as dead code. Logic that only exists on Linux, like Fluorine config, can live wherever fits best.
- Keep changes minimal and scoped. Don't rename or reformat upstream code, and don't fold extra cleanup into a fix. Some bug reports sound like a missing step; first check whether an existing code path already covers it before adding new call sites.
- Buttons in frames are `controls:FlatButton` (`Nolvus.Components.Controls`), as every button upstream is. Don't use a plain Fluent `Button` there. Set only the colors where upstream's designer file differs from FlatButton's defaults (`#363636` background, orange text, white `BorderColor`), which in practice means `Foreground="White"` on most buttons.
- Match the upstream style in the file you're editing. That includes PascalCase locals and parameters (`var Rule = …`, `string InstallDir`) where upstream uses them.
- Add a comment explaining *why* for every non-obvious Linux workaround. Log lines use bracketed area tags such as `[FLUORINE]`, `[SIGCHLD]` and `[FRAME]`.
- Tests are xUnit in `Nolvus.Tests`. They reference `Core`, `Package` and `Services` only (`Services` exposes its internals to the tests). Use the `TempDir` / `Xml.Parse` helpers in `TestEnvironment.cs`. That file also points `XDG_DATA_HOME` at a temp folder, so a Fluorine running on the dev machine doesn't affect the tests.
