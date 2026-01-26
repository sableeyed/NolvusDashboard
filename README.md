![Nolvus Banner](Nolvus.Dashboard/Assets/background-nolvus-banner.svg)

<div align="center">

[Wiki](https://github.com/sableeyed/NolvusDashboard/wiki) | [Download](https://github.com/sableeyed/NolvusDashboard/releases) | [Nolvus Linux Discord](https://discord.gg/Tazgf4Tr4u) | [Official Nolvus Discord](https://discord.gg/Zkh5PwD) | [Nolvus Linux Issues](https://github.com/sableeyed/NolvusDashboard/issues) | [Original Project](https://github.com/vektor9999/NolvusDashboard)

</div>

---


# Nolvus Dashboard for Linux
A Linux native application for installing and managing the Nolvus modlist.

Nolvus Linux automatically downloads and install all requirements for Nolvus and attempts to automate steam shortcut creation and Proton prefix configuration.


## Introduction

Nolvus Dashboard for Linux is a Linux native application written in .NET 9 and uses the same codebase as the Windows version, only changing what is necessary for functionality.

The official Windows dashboard does not function natively on Linux and required a Windows system in some form (VM or dual boot) to complete installation. This project exists to remove that requirement entirely.

**Important Notes**
- This project is in Beta - There will be bugs
- Currently, Nexus Premium is required, but support for free accounts are planned
- Please report any issues you encounter to help improve the application

---

## Features

- Linux First: Designed specifically for Linux with minimal external dependencies
- Upstream Parity: Aims to be bug-for-bug compatible with the official Windows Dashboard
- Automated Nolvus Installation: Install just like you would on Windows
- Proton Prefix Configuration: Attempts to manually configure Proton for you, but can also be done manually
- Launcher Setup: Automatically installs and configures the [Nolvus Launcher](https://github.com/sableeyed/NolvusDashboard/tree/main/Nolvus.Launcher)
- Improved User Experience: Full GUI application with terminal support


## Quick Start

### Requirements
- Linux
- .NET 9 Runtime installed with your package manager
- Steam installed and configured
- Vanilla Skyrim with AE content downloaded
- Nexus Premium
  - Free support planned in the future
- External Tools (must be accessible in PATH)
  - protontricks
  - winetricks
  - xrandr (for resolution detection)
  - xwayland (if using a wayland compositor)

## Installation

### Prebuilt Binaries (Recommended)

Prebuilt binaries are provided. Download the [latest release](https://github.com/sableeyed/NolvusDashboard/releases) and run it like a standard Linux application.

```bash
chmod +x NolvusDashboard
./NolvusDashboard
```

Or double click `NolvusDashboard`


## License

This project is licensed under the **GNU General Public License v3 (GPL-3.0).**

## Credits & Acknowledgements

- Vektor for creating Nolvus and open sourcing the dashboard
- Furglitch/Rockerbacon for their work on MO2 Linux
- The Nolvus Community
- Linux gaming and modding community
- Valve for Proton
- Everyone who has contributed to this project with code or testing
