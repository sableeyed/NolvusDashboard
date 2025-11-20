# Welcome

This product is for Linux only. If you are looking for the Windows version it is [here]("https://github.com/vektor999/NolvusDashboard)
Upates are being made daily. Please check back periodically.

# Known Issues
- v6 installation is not implemented
- Instance management is not implemented
- This requires Nexus Premium. I do not plan to implement what's needed for free accounts at this time

# Wine
A wine prefix will be created for you in ~/.local/share/NolvusDashboard/prefix
Do not modify this unless you know what you are doing
If you break the prefix you can delete the folder and relaunch the Dashboard to have it create a new one
This prefix is required to run certain tools like BSArch

# Required Software
- Protontricks
- Winetricks
- Wine
- 7zip
- xdelta3
- .NET Runtime 9.x

# Untested
- The entire MO2 stack

# Fully Implemented
- Nolvus.NexusApi
- Nolvus.Package
- Nolvus.StockGame
- Nolvus.Core
- Nolvus.Components
- Nolvus.Service
- Nolvus.Instance

# Partially Implemented
- Nolvus.Dashboard
- Nolvus.Browser

# Unimplemented
- Nolvus.Downgrader
- Nolvus.GrassCache

# Refactor Needed
- Nolvus.Updater

# Won't Implement
- Nolvus.Launcher

# TODO
- Unify the theme and layout of all UI components/frames
- Create desktop entries to allow for icons