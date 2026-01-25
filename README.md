![Nolvus Banner](https://github.com/sableeyed/NolvusDashboard/blob/main/Nolvus.Dashboard/Assets/background-nolvus.jpg)

<div align="center">

[Wiki](https://github.com/sableeyed/NolvusDashboard/wiki) | [Download](https://github.com/sableeyed/NolvusDashboard/releases) | [Nolvus Linux Discord](https://discord.gg/Tazgf4Tr4u) | [Nolvus Linux Issues](https://github.com/sableeyed/NolvusDashboard/issues) | [Original Project](https://github.com/vektor9999/NolvusDashboard)

</div>

---


# Nolvus Dashboard for Linux
A native Linux dashboard for installing and managing the Nolvus mod list

---

## Overview

Nolvus Dashboard for Linux is a Linux only reimplementation of the official Nolvus Dashboard, designed to make installing and running the Nolvus modlist possible without requiring Windows.

The official Windows dashboard does not function natively on Linux and required a Windows system in some form (VM or dual-boot) to complete installation. This project exists to remove that requirement entirely.

The goal is simple:

> **Give Linux users a modern and familiar Nolvus installation experience**

---

## Project Status

⚠️ **Beta**  
The application is usable and actively developed, but bugs are expected. Please report issues on GitHub or in Discord.

---

## Features

- **Linux Only, Linux First**
  - No Windows builds
  - Designed specifically for Linux users

- **Upstream Parity**
  - Aims to be **bug-for-bug compatible** with the official Windows Nolvus Dashboard
  - Stays aligned with upstream behavior and workflow

- **Automated Nolvus Installation**
  - Install the Nolvus modlist just like on Windows
  - Minimal manual intervention required

- **Proton Prefix Configuration**
  - Attempts to automatically configure the required Proton prefix
  - Can fall back to manual configuration when needed

- **Launcher Setup**
  - Automatically installs and configures the Nolvus launcher
  - Designed to “just work” with Steam + Proton

- **Improved User Experience**
  - More user-friendly than previous Linux solutions
  - Full GUI application with optional terminal launch

---

## Requirements

### Runtime
- **Linux**
- **.NET 9 Runtime**

### Required tools (must be in `PATH`)
- `protontricks`
- `winetricks`
- `xrandr` (used for resolution detection)

### Additional Notes
- **Wayland users**: `xwayland` is required
- **Wine**:
  - Optional
  - Can be specified manually if not found automatically

---

## Installation

### Prebuilt Binaries (Recommended)

Prebuilt binaries are provided. Download the latest release and run it like a standard Linux application.

```bash
chmod +x NolvusDashboard
./NolvusDashboard
```

Or double click `NolvusDashboard`

### Building from source

You can build from source and/or modify the program freely, but no support will be provided if you choose to do so.

## Design Goals

- Linux users should never need Windows to install or configure Nolvus
- Maintain parity with the upstream Windows version
- Automate setup wherever possible

## Non Goals
- Supporting Windows
- Replacing or competing with the upstream Nolvus project
- Introducing architecture differences unless strictly necessary

## License

This project is licensed under the **GNU General Public License v3 (GPL-3.0).**

## Credits & Acknowledgements

- Vektor for creating Nolvus and open sourcing the dashboard
- The Nolvus Community
- Linux gaming and modding community
- Valve for proton
- Everyone who has contributed to this project with code or testing

## Disclaimer

**This project is provided "as is" without any warranty of any kind**
- Bugs are expected
- Support is best effort
- Use at your own risk


