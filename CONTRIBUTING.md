# Contributing to Nolvus Dashboard for Linux

Thanks for your interest in helping out! Nolvus Dashboard for Linux is a community port of the official [Nolvus Dashboard](https://github.com/vektor9999/NolvusDashboard), and it's in **beta**, so bug reports, testing, and code contributions are all genuinely valuable.

This document explains how to report problems, set up a development environment, and get changes merged.

---

## Table of Contents

- [Ground Rules](#ground-rules)
- [Where to Ask for Help](#where-to-ask-for-help)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Features](#suggesting-features)
- [Development Setup](#development-setup)
- [Project Layout](#project-layout)
- [The Upstream Parity Principle](#the-upstream-parity-principle)
- [Making Changes](#making-changes)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Testing Your Changes](#testing-your-changes)
- [License](#license)

---

## Ground Rules

- Be respectful and patient. Everyone here is a volunteer.
- **This repository is for the Linux port only.** Problems with the Nolvus modlist itself, or with the official Windows dashboard, belong in the [Official Nolvus Discord](https://discord.gg/Zkh5PwD), not here.
- Search [existing issues](https://github.com/sableeyed/NolvusDashboard/issues) before opening a new one.
- Keep pull requests focused. One fix or feature per PR makes review much easier.

## Where to Ask for Help

| You want to…                                         | Go to                                                                 |
| ---------------------------------------------------- | --------------------------------------------------------------------- |
| Report a bug in the Linux dashboard                  | [GitHub Issues](https://github.com/sableeyed/NolvusDashboard/issues)   |
| Ask a question or discuss an idea                    | [GitHub Discussions](https://github.com/sableeyed/NolvusDashboard/discussions) or the [Nolvus Linux Discord](https://discord.gg/Tazgf4Tr4u) |
| Get help with the modlist itself (not Linux-specific) | [Official Nolvus Discord](https://discord.gg/Zkh5PwD)                 |
| Read setup guides and troubleshooting                | [Wiki](https://github.com/sableeyed/NolvusDashboard/wiki)              |

---

## Reporting Bugs

A good bug report saves a lot of back-and-forth. Please include:

1. **Dashboard version** (from the release you downloaded, or the commit hash if built from source).
2. **Distribution and version** (e.g. Arch, Fedora 42, Ubuntu 24.04, SteamOS, Bazzite).
3. **Desktop session**: X11 or Wayland, and which desktop environment / compositor.
4. **Steam install type**: native package, Flatpak, or Snap.
5. **Proton version** used for the Skyrim prefix.
6. **Versions of external tools**:
   ```bash
   dotnet --list-runtimes
   protontricks --version
   winetricks --version
   ```
7. **What you did**, what you expected, and what actually happened.
8. **At what step it failed** (e.g. downloading, extracting, prefix setup, launcher setup, Steam shortcut creation).
9. **Logs and terminal output.** Running the dashboard from a terminal (`./NolvusDashboard`) and pasting the output is often the fastest way to diagnose a problem. Please attach long logs as a file or use a `<details>` block rather than pasting them inline.

> ⚠️ **Scrub personal data before posting logs.** Remove Nexus API keys, usernames in paths if you care about them, and any other credentials.

### Before reporting, please check

- `winetricks` is up to date: `sudo winetricks --self-update`
- `protontricks`, `winetricks`, `xrandr`, and (on Wayland) `xwayland` are installed and on your `PATH`.
- The .NET 9 runtime is installed.
- Vanilla Skyrim with the Anniversary Edition content is fully downloaded in Steam.

---

## Suggesting Features

Open a [Discussion](https://github.com/sableeyed/NolvusDashboard/discussions) or an issue describing:

- The problem you're trying to solve (not only the solution you have in mind).
- Whether it's Linux-specific or something the official Windows dashboard also lacks.

Features that diverge from the Windows dashboard's behavior need a strong Linux-specific justification. See [The Upstream Parity Principle](#the-upstream-parity-principle) below.

---

## Development Setup

### Prerequisites

- Linux (x86_64)
- [.NET 9 SDK](https://learn.microsoft.com/dotnet/core/install/linux) (the SDK, not only the runtime)
- Git
- The same runtime tools end users need, if you plan to test installs end to end: `protontricks`, `winetricks`, `xrandr`, and `xwayland` on Wayland
- An IDE of your choice. JetBrains Rider, VS Code with the C# Dev Kit, or plain `dotnet` CLI all work with the solution file.

### Clone and build

```bash
# Fork the repo on GitHub first, then:
git clone https://github.com/<your-username>/NolvusDashboard.git
cd NolvusDashboard
git remote add upstream https://github.com/sableeyed/NolvusDashboard.git

# Restore and build the whole solution
dotnet build "Nolvus Dashboard Linux.sln"
```

### Run from source

```bash
dotnet run --project Nolvus.Dashboard/Nolvus.Dashboard.csproj
```

### Building a release package

`build.sh` produces a release tarball the same way official releases are built:

```bash
./build.sh
```

A few things to know about it:

- It reads the version from the `<Version>` element in `Nolvus.Dashboard/Nolvus.Dashboard.csproj`, so make sure that's set.
- It publishes `Nolvus.Dashboard` and `Nolvus.Updater` for `linux-x64`, merges them into one `Nolvus/` folder, and strips debug symbols.
- It rewires the bundled CefGlue browser subprocess to run on the system .NET runtime instead of shipping a second copy. If you touch anything related to `Nolvus.Browser` or bump the CefGlue package, re-check that this step still works (the script prints a warning if the subprocess isn't found).
- The finished archive is written to `~/Desktop/Binaries-<version>.tar.gz`, and the intermediate `bin/` directory is deleted afterward.

You usually don't need `build.sh` for day-to-day development; `dotnet build` / `dotnet run` is faster.

---

## Project Layout

| Project                         | Purpose (high level)                                          |
| ------------------------------- | ------------------------------------------------------------- |
| `Nolvus.Dashboard`              | The main GUI application and entry point                      |
| `Nolvus.Core`                   | Shared core types, interfaces, and utilities                  |
| `Nolvus.Services`               | Services used by the dashboard (files, downloads, settings, etc.) |
| `Nolvus.Components`             | Reusable UI components / controls                             |
| `Nolvus.Browser`                | Embedded browser (CefGlue) used for web-based flows           |
| `Nolvus.NexusApi`               | Nexus Mods API client                                         |
| `Nolvus.Api.Library.Installer`  | Client for the Nolvus installer API                           |
| `Nolvus.Instance`               | Modlist instance management                                   |
| `Nolvus.Package`                | Mod package handling and installation steps                   |
| `Nolvus.StockGame`              | Stock game (clean Skyrim copy) creation                       |
| `Nolvus.Launcher`               | The Nolvus Launcher installed alongside the modlist          |
| `Nolvus.Updater`                | Self-updater for the dashboard                                |

If you're not sure where a change belongs, ask in your issue or PR.

---

## The Upstream Parity Principle

This is the most important thing to understand before contributing code.

The Linux dashboard **uses the same codebase as the official Windows dashboard and only changes what is necessary for it to work on Linux**. The goal is to be *bug-for-bug compatible* with upstream. That keeps the two projects easy to compare, makes it possible to pull in upstream changes, and means Nolvus support staff can reason about both versions the same way.

In practice this means:

- **Prefer the smallest change that makes something work on Linux.** Avoid refactoring, renaming, or reformatting upstream code unless it's required for your fix.
- **Isolate Linux-specific logic** where you reasonably can (a dedicated class, method, or clearly marked block) instead of rewriting shared code paths.
- **Comment non-obvious Linux workarounds** (Proton/Wine quirks, path case sensitivity, Steam install variants, etc.) so future maintainers understand *why* they exist.
- **Bugs that also exist in the Windows dashboard** should generally be reported to the [original project](https://github.com/vektor9999/NolvusDashboard) rather than fixed only here. If a fix is needed here anyway, say so in your PR.
- **New features** that don't exist in the Windows dashboard are held to a higher bar and should be discussed in an issue first.

Common Linux-specific concerns worth keeping in mind:

- File paths are **case-sensitive**. Don't assume `Data` and `data` are the same folder.
- Use `Path.Combine` / `Path.DirectorySeparatorChar` rather than hard-coded `\`.
- Steam can live in several places (`~/.steam`, `~/.local/share/Steam`, Flatpak under `~/.var/app/com.valvesoftware.Steam`). Don't assume one.
- External tools (`protontricks`, `winetricks`, `xrandr`) may be missing or outdated. Fail with a clear, actionable error rather than a stack trace.

---

## Making Changes

1. **Sync with upstream** before starting:
   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   ```
2. **Create a branch** with a descriptive name:
   ```bash
   git checkout -b fix/prefix-setup-flatpak-steam
   ```
3. **Write clear commit messages.** A short summary line (≈50–72 characters), then a blank line and more detail if needed:
   ```
   Fix Steam library detection for Flatpak installs

   The Flatpak Steam root lives under ~/.var/app/com.valvesoftware.Steam,
   which was not included in the search paths.
   ```
4. **Match the surrounding code style.** Follow the conventions already used in the file you're editing (naming, bracing, indentation). Don't mix formatting changes into functional changes.
5. **Make sure the solution builds without new warnings** you introduced:
   ```bash
   dotnet build "Nolvus Dashboard Linux.sln"
   ```

---

## Submitting a Pull Request

1. Push your branch to your fork and open a PR against `main`.
2. In the PR description, include:
   - **What** the change does and **why**.
   - A link to the related issue (e.g. `Fixes #123`).
   - **How you tested it**: distro, desktop session, Steam install type, and which part of the install flow you exercised.
   - Whether the change **diverges from upstream** behavior, and if so, why that's necessary.
   - Screenshots for any UI changes.
3. Keep the PR up to date with `main` and respond to review feedback. It's fine to push follow-up commits; they can be squashed on merge.
4. Be patient. Reviews happen when maintainers have time.

### PR checklist

- [ ] The solution builds cleanly
- [ ] I ran the dashboard and exercised the code path I changed
- [ ] Changes are minimal and scoped to the problem (upstream parity)
- [ ] Linux-specific workarounds are commented
- [ ] No secrets, API keys, or personal paths are committed
- [ ] The PR description explains what, why, and how it was tested

---

## Testing Your Changes

A full Nolvus installation is large and time-consuming, so test as close to the change as you practically can:

- **UI or settings changes**: run the dashboard from source and verify the affected screens.
- **Installer, download, or extraction changes**: exercise the relevant step. A full end-to-end install is the gold standard but isn't always required. Say in your PR how far you tested.
- **Proton prefix / Steam integration changes**: test with a fresh prefix if possible, and note your Steam install type and Proton version.
- **Build script or packaging changes**: run `./build.sh`, extract the tarball, and confirm the packaged `NolvusDashboard` and `NolvusUpdater` launch.

Testing on multiple distros or desktop sessions (X11 vs Wayland, native vs Flatpak Steam) is especially helpful. If you can only test one setup, that's fine; just say which.

**Not a coder?** Testing releases and filing detailed bug reports is one of the most useful contributions you can make to a beta project.

---

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE). By submitting a contribution, you agree that it will be distributed under the same license.

---

Thanks again for helping make Nolvus work great on Linux! 🐧
