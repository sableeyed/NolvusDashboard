using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using Nolvus.Package.Rules;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.RegularExpressions;
using Avalonia.Input;

namespace Nolvus.Package.Mods
{
    public enum IniLevel { IniLow, IniMedium, IniHigh }
    public class ModOrganizer : Software, IModOrganizer
    {
        #region Constants

        #region Mod Organizer Ini
        const string IniFile =
@"[General]
gameName=Skyrim Special Edition
selected_profile=@ByteArray({0})
gamePath=@ByteArray({1})
game_edition=Steam
version=0.3.4
first_start=false

[PluginPersistance]
Python%20Proxy\tryInit=false

[recentDirectories]
size=0

[Widgets]
SettingsDialog_tabWidget_index=0
MainWindow_executablesListBox_index=1
MainWindow_tabWidget_index=0
MainWindow_dataTabShowOnlyConflicts_checked=false
MainWindow_dataTabShowFromArchives_checked=false
MainWindow_groupCombo_index=0
MainWindow_modList_index=DLC: HearthFires, DLC: Dragonborn, DLC: Dawnguard, Overwrite
MainWindow_filters_index = 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
MainWindow_filtersAnd_checked=true
MainWindow_filtersOr_checked=false
MainWindow_filtersSeparators_index=0

[Geometry]SettingsDialog_geometry=""@ByteArray(\x1\xd9\xd0\xcb\0\x3\0\0\0\0\x2%\0\0\0\xcc\0\0\x5Z\0\0\x3;\0\0\x2&\0\0\0\xeb\0\0\x5Y\0\0\x3:\0\0\0\0\0\0\0\0\a\x80\0\0\x2&\0\0\0\xeb\0\0\x5Y\0\0\x3:)""
ProfilesDialog_geometry=@ByteArray(\x1\xd9\xd0\xcb\0\x3\0\0\0\0\x2\xce\0\0\x1N\0\0\x4\xb1\0\0\x2\xb9\0\0\x2\xcf\0\0\x1m\0\0\x4\xb0\0\0\x2\xb8\0\0\0\0\0\0\0\0\a\x80\0\0\x2\xcf\0\0\x1m\0\0\x4\xb0\0\0\x2\xb8)
MainWindow_state=@ByteArray(\0\0\0\xff\0\0\0\0\xfd\0\0\0\x1\0\0\0\x3\0\0\a\x80\0\0\0\xd6\xfc\x1\0\0\0\x1\xfb\0\0\0\xe\0l\0o\0g\0\x44\0o\0\x63\0k\x1\0\0\0\0\0\0\a\x80\0\0\0^\0\xff\xff\xff\0\0\a\x80\0\0\x2\xc5\0\0\0\x4\0\0\0\x4\0\0\0\b\0\0\0\b\xfc\0\0\0\x1\0\0\0\x2\0\0\0\x1\0\0\0\xe\0t\0o\0o\0l\0\x42\0\x61\0r\x1\0\0\0\0\xff\xff\xff\xff\0\0\0\0\0\0\0\0)
MainWindow_geometry=@ByteArray(\x1\xd9\xd0\xcb\0\x3\0\0\xff\xff\xff\xff\xff\xff\xff\xf8\0\0\a\x80\0\0\x4\x10\0\0\x1\xc7\0\0\0\xa5\0\0\x6\xda\0\0\x3\xc4\0\0\0\0\x2\0\0\0\a\x80\0\0\0\0\0\0\0\x17\0\0\a\x7f\0\0\x4\xf)
MainWindow_docks_logDock_size=214
MainWindow_menuBar_visibility=true
MainWindow_statusBar_visibility=true
MainWindow_toolBar_visibility=true
toolbar_size=@Size(42 36)
toolbar_button_style=0
MainWindow_splitter_state=@ByteArray(\0\0\0\xff\0\0\0\x1\0\0\0\x2\0\0\x3\xf3\0\0\x2\xb0\x1\xff\xff\xff\xff\x1\0\0\0\x1\0)
MainWindow_categoriesSplitter_state=@ByteArray(\0\0\0\xff\0\0\0\x1\0\0\0\x2\0\0\x1\b\0\0\x2\xe2\0\xff\xff\xff\xff\x1\0\0\0\x1\0)
MainWindow_monitor=0
MainWindow_categoriesGroup_visibility=false
MainWindow_espList_state=@ByteArray(\0\0\0\xff\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\x2\x1\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\x2\xe9\0\0\0\x4\x1\x1\0\x1\0\0\0\0\0\0\0\0\x1\0\0\0\x64\xff\xff\xff\xff\0\0\0\x81\0\0\0\0\0\0\0\x4\0\0\x1M\0\0\0\x1\0\0\0\0\0\0\0'\0\0\0\x1\0\0\0\0\0\0\0\x31\0\0\0\x1\0\0\0\0\0\0\x1\x44\0\0\0\x1\0\0\0\0\0\0\x3\xe8\0\0\0\0\x44)
MainWindow_downloadView_state=@ByteArray(\0\0\0\xff\0\0\0\0\0\0\0\x1\0\0\0\x1\0\0\0\x1\x1\0\0\0\0\0\0\0\0\0\0\0\b\xf0\0\0\0\x4\0\0\0\x4\0\0\0\x64\0\0\0\x5\0\0\0\x64\0\0\0\x6\0\0\0\x64\0\0\0\a\0\0\0\x64\0\0\x1>\0\0\0\b\x1\x1\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x64\xff\xff\xff\xff\0\0\0\x81\0\0\0\0\0\0\0\b\0\0\0'\0\0\0\x1\0\0\0\0\0\0\0O\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\x3\xe8\x1\0\0\0\0)
MainWindow_savegameList_state=@ByteArray(\0\0\0\xff\0\0\0\0\0\0\0\x1\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\xc8\0\0\0\x2\x1\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\0\x64\xff\xff\xff\xff\0\0\0\x81\0\0\0\0\0\0\0\x2\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\x3\xe8\0\0\0\0\x64)
MainWindow_dataTree_state=@ByteArray(\0\0\0\xff\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\x2X\0\0\0\x5\x1\x1\0\x1\0\0\0\0\0\0\0\0\0\0\0\0\x64\xff\xff\xff\xff\0\0\0\x81\0\0\0\0\0\0\0\x5\0\0\0\xc8\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\0\x64\0\0\0\x1\0\0\0\0\0\0\x3\xe8\0\0\0\0\x64)
MainWindow_modList_state=@ByteArray(\0\0\0\xff\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\t\x1\0\0\0\0\0\0\0\0\0\0\0\vh\x5\0\0\0\x5\0\0\0\x3\0\0\0'\0\0\0\b\0\0\0'\0\0\0\n\0\0\0'\0\0\0\x5\0\0\0'\0\0\0\x6\0\0\0'\0\0\x4\x64\0\0\0\v\x1\x1\0\x1\0\0\0\0\0\0\0\0\x1\0\0\0#\xff\xff\xff\xff\0\0\0\x81\0\0\0\0\0\0\0\v\0\0\x1\xf8\0\0\0\x1\0\0\0\0\0\0\0\x38\0\0\0\x1\0\0\0\0\0\0\0(\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0:\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\0\x31\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\x1\xa1\0\0\0\x1\0\0\0\0\0\0\0\0\0\0\0\x1\0\0\0\0\0\0\x3\xe8\x1\0\0\0\x31)

[Settings]
log_level=3
language=en
compact_downloads=false
meta_downloads=false
autohide_downloads=false
check_for_updates=true
use_prereleases=false
center_dialogs=false
show_change_game_confirmation=true
double_click_previews=true
overwrittenLooseFilesColor=@Variant(\0\0\0\x43\x1@@\0\0\xff\xff\0\0\0\0)
overwritingLooseFilesColor=@Variant(\0\0\0\x43\x1@@\xff\xff\0\0\0\0\0\0)
overwrittenArchiveFilesColor=@Variant(\0\0\0\x43\x1@@\0\0\xff\xff\xff\xff\0\0)
overwritingArchiveFilesColor=@Variant(\0\0\0\x43\x1@@\xff\xff\0\0\xff\xff\0\0)
containsPluginColor=@Variant(\0\0\0\x43\x1@@\0\0\0\0\xff\xff\0\0)
containedColor=@Variant(\0\0\0\x43\x1@@\0\0\0\0\xff\xff\0\0)
colorSeparatorScrollbars=true
display_foreign=true
collapsible_separators_asc=true
collapsible_separators_dsc=true
collapsible_separators_conflicts_from=true
collapsible_separators_conflicts_to=true
collapsible_separators_per_profile=false
save_filters=false
auto_collapse_on_hover=false
autocheck_update_install=true
collapsible_separators_icons_1=true
collapsible_separators_icons_2=true
collapsible_separators_icons_3=true
collapsible_separators_icons_7=true
base_directory={2}
crash_dumps_type=1
crash_dumps_max=5
loot_log_level=2
endorsement_integration=true
tracked_integration=true
hide_api_counter=false
force_enable_core_files=true
lock_gui=true
archive_parsing_experimental=false
offline_mode=false
use_proxy=false
use_custom_browser=false
custom_browser=
executable_blacklist=""Chrome.exe;Firefox.exe;TSVNCache.exe;TGitCache.exe;Steam.exe;GameOverlayUI.exe;Discord.exe;GalaxyClient.exe;Spotify.exe""
filter_regex=false
regex_case_sensitive=false
regex_extended=false
filter_scroll_to_selection=false
style=vs15 Dark-Orange.qss

[Servers]
size=0

[Plugins]
BAIN%20Wizard%20Installer\prefer_fomod=true
BAIN%20Wizard%20Installer\prefer_omod=false
BAIN%20Wizard%20Installer\priority=120
BSA%20Extractor\only_alternate_source=true
BSA%20Packer\blacklisted_files="".txt;.hkx;.xml;.ini""
BSA%20Packer\create_plugins=false
BSA%20Packer\hide_loose_assets=false
Basic%20diagnosis%20plugin\check_alternategames=false
Basic%20diagnosis%20plugin\check_conflict=true
Basic%20diagnosis%20plugin\check_errorlog=true
Basic%20diagnosis%20plugin\check_fileattributes=false
Basic%20diagnosis%20plugin\check_font=true
Basic%20diagnosis%20plugin\check_missingmasters=true
Basic%20diagnosis%20plugin\check_overwrite=true
Basic%20diagnosis%20plugin\ow_ignore_empty=false
Basic%20diagnosis%20plugin\ow_ignore_log=false
Bundle%20Installer\auto_reinstall=true
DDS%20Preview%20Plugin\background%20a=0
DDS%20Preview%20Plugin\background%20b=0
DDS%20Preview%20Plugin\background%20g=0
DDS%20Preview%20Plugin\background%20r=0
DDS%20Preview%20Plugin\log%20gl%20errors=false
Enderal%20Support%20Plugin\sse_downloads=false
FNIS%20Checker\sensitive=false
FNIS%20Integration%20Tool\fnis-path=
FNIS%20Integration%20Tool\initialised=false
FNIS%20Integration%20Tool\output-logs-path=
FNIS%20Integration%20Tool\output-logs-to-mod=true
FNIS%20Integration%20Tool\output-path=
FNIS%20Integration%20Tool\output-to-mod=true
Fomod%20Installer\prefer=true
Fomod%20Installer\see_disabled_mods=false
Fomod%20Installer\use_any_file=false
Fomod%20Installer%20C%23\prefer=true
INI%20Editor\associated=true
INI%20Editor\external=false
Preview%20Base\blacklisted_extensions=
Python%20Proxy\python_dir=
Simple%20Installer\silent=false
Skyrim%20Special%20Edition%20Support%20Plugin\enderal_downloads=false
Skyrim%20Support%20Plugin\sse_downloads=false

[pluginBlacklist]
size=0

[fluorine]
usvfs_exact_query_exhaustion=false
usvfs_shared_context=false
vfs_backend=fuse";

        // Dead under Fluorine: nothing writes nxmhandler.ini any more.
        //private const string nxmhandler = @"[General]
        //noregister=true";

        #endregion

        #region Skyrim.ini SE

        #region Low

        const string SkyrimIniLow = @"[Actor]
bUseNavMeshForMovement=0

[Archive]
bLoadArchiveInMemory=1
sArchiveToLoadInMemoryList=Skyrim - Animations.bsa
sResourceArchiveList=Skyrim - Misc.bsa, Skyrim - Shaders.bsa, Skyrim - Interface.bsa, Skyrim - Animations.bsa, Skyrim - Meshes0.bsa, Skyrim - Meshes1.bsa, Skyrim - Sounds.bsa
sResourceArchiveList2=Skyrim - Voices_{0}0.bsa, Skyrim - Textures0.bsa, Skyrim - Textures1.bsa, Skyrim - Textures2.bsa, Skyrim - Textures3.bsa, Skyrim - Textures4.bsa, Skyrim - Textures5.bsa, Skyrim - Textures6.bsa, Skyrim - Textures7.bsa, Skyrim - Textures8.bsa, Skyrim - Patch.bsa

[Audio]
fMenuModeFadeInTime=1.0
fMenuModeFadeOutTime=3.0
fMusicDuckingSeconds=6.0
fMusicUnDuckingSeconds=8.0

[Camera]
fMouseWheelZoomSpeed=10.00

[Combat]
f1PArrowTiltUpAngle=0.7
f1PBoltTiltUpAngle=0.7
f3PArrowTiltUpAngle=2.5
fMagnetismLookingMult=0.0
fMagnetismStrafeHeadingMult=0.0

[Controls]
fMouseHeadingSensitivityMax=0.0625
fMouseHeadingXScale=0.0200
fMouseHeadingYScale=0.8500

[Decals]
uMaxSkinDecalPerActor=60

[Display]
bEnableSnowMask=0
bEnableSnowRimLighting=0
bLockFrameRate=0
bVolumetricLightingDisableInterior=1
fDDOFFocusCenterweightExt=2
fDecalLifetime=30
fDOFMaxDepthParticipation=10000
fDynamicDOFFarBlur=0.4
fFirstSliceDistance=2000
fGlobalBrightnessBoost=0.0000
fGlobalContrastBoost=0.0000
fGlobalSaturationBoost=0.0000
fLightLODRange=4586
fLightLODStartFade=3500.0
fShadowDirectionalBiasScale=0.3
fSnowGeometrySpecPower=3.00
fSnowNormalSpecPower=2.00
fSnowRimLightIntensity=0.30
fSparklesDensity=0.85
fSparklesSize=6.00
fSunShadowUpdateTime=1
fSunUpdateThreshold=0.5
fTreesMidLODSwitchDist=9999999.0000
sScreenShotBaseName=C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\ScreenShot

[General]
bBackgroundLoadVMData=1
fFlickeringLightDistance=8192
sIntroSequence=
sLanguage={1}
sTestFile1=Dawnguard.esm
sTestFile2=HearthFires.esm
sTestFile3=Dragonborn.esm
uExterior Cell Buffer=36
uGridsToLoad=5

[GeneralWarnings]
SGeneralMasterMismatchWarning=One or more plugins could not find the correct versions of the master files they depend on. Errors may occur during load or game play. Check the ""Warnings.txt"" file for more information.

[Grass]
bAllowCreateGrass=1
bAllowLoadGrass=0
bDrawShaderGrass=1
fGrassFadeRange=2100
fGrassMaxStartFadeDistance=7000.0000
fGrassMinStartFadeDistance=0.0000
fGrassWindMagnitudeMax=0
fGrassWindMagnitudeMin=0
iGrassCellRadius=2
iMaxGrassTypesPerTexure=2
iMinGrassSize=40

[HAVOK]
fMaxTime=0.01666667

[Imagespace]
iRadialBlurLevel=2

[Interface]
bShowTutorials=0

[Launcher]
bEnableFileSelection=1

[LightingShader]
fDecalLODFadeEnd=0.15
fDecalLODFadeStart=0.1
fEnvmapLODFadeEnd=0.25
fEnvmapLODFadeStart=0.2

[LOD]
fDistanceMultiplier=1.00

[MapMenu]
fMapWorldMaxPitch=90
fMapWorldMinPitch=0
fMapWorldYawRange=400

[Menu]
iConsoleSizeScreenPercent=40
iConsoleTextSize=20

[Papyrus]
bEnableLogging=0
bEnableProfiling=0
bEnableTrace=0
bLoadDebugInformation=0
fPostLoadUpdateTimeMS=2000

[SaveGame]
iAutoSaveCount=3

[Trees]
bEnableTreeAnimations=0
bEnableTrees=0
fUpdateBudget=0

[Water]
bReflectLODLand=1
bReflectLODObjects=0
bReflectLODTrees=1
bReflectSky=1";

        #endregion

        #region Medium

        const string SkyrimIniMedium = @"[Actor]
bUseNavMeshForMovement=0

[Archive]
bLoadArchiveInMemory=1
sArchiveToLoadInMemoryList=Skyrim - Animations.bsa
sResourceArchiveList=Skyrim - Misc.bsa, Skyrim - Shaders.bsa, Skyrim - Interface.bsa, Skyrim - Animations.bsa, Skyrim - Meshes0.bsa, Skyrim - Meshes1.bsa, Skyrim - Sounds.bsa
sResourceArchiveList2=Skyrim - Voices_{0}0.bsa, Skyrim - Textures0.bsa, Skyrim - Textures1.bsa, Skyrim - Textures2.bsa, Skyrim - Textures3.bsa, Skyrim - Textures4.bsa, Skyrim - Textures5.bsa, Skyrim - Textures6.bsa, Skyrim - Textures7.bsa, Skyrim - Textures8.bsa, Skyrim - Patch.bsa

[Audio]
fMenuModeFadeInTime=1.0
fMenuModeFadeOutTime=3.0
fMusicDuckingSeconds=6.0
fMusicUnDuckingSeconds=8.0

[Camera]
fMouseWheelZoomSpeed=10.00

[Combat]
f1PArrowTiltUpAngle=0.7
f1PBoltTiltUpAngle=0.7
f3PArrowTiltUpAngle=2.5
fMagnetismLookingMult=0.0
fMagnetismStrafeHeadingMult=0.0

[Controls]
fMouseHeadingSensitivityMax=0.0625
fMouseHeadingXScale=0.0200
fMouseHeadingYScale=0.8500

[Decals]
uMaxSkinDecalPerActor=20

[Display]
bEnableSnowMask=0
bEnableSnowRimLighting=0
bLockFrameRate=0
bVolumetricLightingDisableInterior=1
fDDOFFocusCenterweightExt=2
fDecalLifetime=240
fDOFMaxDepthParticipation=10000
fDynamicDOFFarBlur=0.4
fFirstSliceDistance=2000
fGlobalBrightnessBoost=0.0000
fGlobalContrastBoost=0.0000
fGlobalSaturationBoost=0.0000
fLightLODRange=9623
fLightLODStartFade=3500.0
fShadowDirectionalBiasScale=0.3
fSnowGeometrySpecPower=3.00
fSnowNormalSpecPower=2.00
fSnowRimLightIntensity=0.30
fSparklesDensity=0.85
fSparklesSize=6.00
fSunShadowUpdateTime=1
fSunUpdateThreshold=0.5
fTreesMidLODSwitchDist=9999999.0000
sScreenShotBaseName=C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\ScreenShot

[General]
bBackgroundLoadVMData=1
fFlickeringLightDistance=8192
sIntroSequence=
sLanguage={1}
sTestFile1=Dawnguard.esm
sTestFile2=HearthFires.esm
sTestFile3=Dragonborn.esm
uExterior Cell Buffer=36
uGridsToLoad=5

[GeneralWarnings]
SGeneralMasterMismatchWarning=One or more plugins could not find the correct versions of the master files they depend on. Errors may occur during load or game play. Check the ""Warnings.txt"" file for more information.

[Grass]
bAllowCreateGrass=1
bAllowLoadGrass=0
bDrawShaderGrass=1
fGrassFadeRange=3000
fGrassMaxStartFadeDistance=7000.0000
fGrassMinStartFadeDistance=0.0000
fGrassWindMagnitudeMax=65
fGrassWindMagnitudeMin=5
iGrassCellRadius=2
iMaxGrassTypesPerTexure=2
iMinGrassSize=40

[HAVOK]
fMaxTime=0.01666667

[Imagespace]
iRadialBlurLevel=2

[Interface]
bShowTutorials=0

[Launcher]
bEnableFileSelection=1

[LightingShader]
fDecalLODFadeEnd=0.3
fDecalLODFadeStart=0.2
fEnvmapLODFadeEnd=0.4
fEnvmapLODFadeStart=0.3

[LOD]
fDistanceMultiplier=1.00

[MapMenu]
fMapWorldMaxPitch=90
fMapWorldMinPitch=0
fMapWorldYawRange=400

[Menu]
iConsoleSizeScreenPercent=40
iConsoleTextSize=20

[Papyrus]
bEnableLogging=0
bEnableProfiling=0
bEnableTrace=0
bLoadDebugInformation=0
fPostLoadUpdateTimeMS=2000

[SaveGame]
iAutoSaveCount=3

[Trees]
bEnableTreeAnimations=1
bEnableTrees=1
fUpdateBudget=1.5

[Water]
bReflectLODLand=1
bReflectLODObjects=0
bReflectLODTrees=1
bReflectSky=1";

        #endregion

        #region High

        const string SkyrimIniHigh =
@"[Actor]
bUseNavMeshForMovement=0

[Archive]
bLoadArchiveInMemory=1
sArchiveToLoadInMemoryList=Skyrim - Animations.bsa
sResourceArchiveList=Skyrim - Misc.bsa, Skyrim - Shaders.bsa, Skyrim - Interface.bsa, Skyrim - Animations.bsa, Skyrim - Meshes0.bsa, Skyrim - Meshes1.bsa, Skyrim - Sounds.bsa
sResourceArchiveList2=Skyrim - Voices_{0}0.bsa, Skyrim - Textures0.bsa, Skyrim - Textures1.bsa, Skyrim - Textures2.bsa, Skyrim - Textures3.bsa, Skyrim - Textures4.bsa, Skyrim - Textures5.bsa, Skyrim - Textures6.bsa, Skyrim - Textures7.bsa, Skyrim - Textures8.bsa, Skyrim - Patch.bsa

[Audio]
fMenuModeFadeInTime=1.0
fMenuModeFadeOutTime=3.0
fMusicDuckingSeconds=6.0
fMusicUnDuckingSeconds=8.0

[Camera]
fMouseWheelZoomSpeed=10.00

[Combat]
f1PArrowTiltUpAngle=0.7
f1PBoltTiltUpAngle=0.7
f3PArrowTiltUpAngle=2.5
fMagnetismLookingMult=0.0
fMagnetismStrafeHeadingMult=0.0

[Controls]
fMouseHeadingSensitivityMax=0.0625
fMouseHeadingXScale=0.0200
fMouseHeadingYScale=0.8500

[Decals]
uMaxSkinDecalPerActor=40

[Display]
bEnableSnowMask=0
bEnableSnowRimLighting=0
bLockFrameRate=0
bVolumetricLightingDisableInterior=1
fDDOFFocusCenterweightExt=2
fDecalLifetime=300
fDOFMaxDepthParticipation=10000
fDynamicDOFFarBlur=0.4
fFirstSliceDistance=2800
fGlobalBrightnessBoost=0.0000
fGlobalContrastBoost=0.0000
fGlobalSaturationBoost=0.0000
fLightLODRange=33750
fLightLODStartFade=3500.0
fShadowDirectionalBiasScale=0.15
fSnowGeometrySpecPower=3.00
fSnowNormalSpecPower=2.00
fSnowRimLightIntensity=0.30
fSparklesDensity=0.85
fSparklesSize=6.00
fSunShadowUpdateTime=1
fSunUpdateThreshold=0.5
fTreesMidLODSwitchDist=9999999.0000
sScreenShotBaseName=C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\ScreenShot

[General]
bBackgroundLoadVMData=1
fFlickeringLightDistance=8192
sIntroSequence=
sLanguage={1}
sTestFile1=Dawnguard.esm
sTestFile2=HearthFires.esm
sTestFile3=Dragonborn.esm
uExterior Cell Buffer=36
uGridsToLoad=5

[GeneralWarnings]
SGeneralMasterMismatchWarning=One or more plugins could not find the correct versions of the master files they depend on. Errors may occur during load or game play. Check the ""Warnings.txt"" file for more information.

[Grass]
bAllowCreateGrass=1
bAllowLoadGrass=0
bDrawShaderGrass=1
fGrassFadeRange=7500
fGrassMaxStartFadeDistance=7000.0000
fGrassMinStartFadeDistance=0.0000
fGrassWindMagnitudeMax=125
fGrassWindMagnitudeMin=5
iGrassCellRadius=2
iMaxGrassTypesPerTexure=3
iMinGrassSize=40

[HAVOK]
fMaxTime=0.01666667

[Imagespace]
iRadialBlurLevel=2

[Interface]
bShowTutorials=0

[LightingShader]
fDecalLODFadeEnd=0.6
fDecalLODFadeStart=0.5
fEnvmapLODFadeEnd=0.7
fEnvmapLODFadeStart=0.6

[LOD]
fDistanceMultiplier=1.00

[MapMenu]
fMapWorldMaxPitch=90
fMapWorldMinPitch=0
fMapWorldYawRange=400

[Menu]
iConsoleSizeScreenPercent=40
iConsoleTextSize=20

[Papyrus]
bEnableLogging=0
bEnableProfiling=0
bEnableTrace=0
bLoadDebugInformation=0
fPostLoadUpdateTimeMS=2000

[SaveGame]
iAutoSaveCount=3

[Trees]
bEnableTreeAnimations=1
bEnableTrees=1
fUpdateBudget=1.5

[Water]
bReflectLODLand=1
bReflectLODObjects=1
bReflectLODTrees=1
bReflectSky=1";

        #endregion

        #endregion

        #region Skyrim.ini AE

        #region Low

        const string SkyrimIniLowAE = @"
[Actor]
bUseNavMeshForMovement=0

[Archive]
bLoadArchiveInMemory=1
sArchiveToLoadInMemoryList=Skyrim - Animations.bsa
sResourceArchiveList=Skyrim - Misc.bsa, Skyrim - Shaders.bsa, Skyrim - Interface.bsa, Skyrim - Animations.bsa, Skyrim - Meshes0.bsa, Skyrim - Meshes1.bsa, Skyrim - Sounds.bsa
sResourceArchiveList2=Skyrim - Voices_{0}0.bsa, Skyrim - Textures0.bsa, Skyrim - Textures1.bsa, Skyrim - Textures2.bsa, Skyrim - Textures3.bsa, Skyrim - Textures4.bsa, Skyrim - Textures5.bsa, Skyrim - Textures6.bsa, Skyrim - Textures7.bsa, Skyrim - Textures8.bsa, Skyrim - Patch.bsa

[Audio]
fMenuModeFadeInTime=1.0
fMenuModeFadeOutTime=3.0
fMusicDuckingSeconds=6.0
fMusicUnDuckingSeconds=8.0
uMaxSizeForCachedSound=4096

[Camera]
fMouseWheelZoomSpeed=10.00

[Combat]
f1PArrowTiltUpAngle=0.7
f1PBoltTiltUpAngle=0.7
f3PArrowTiltUpAngle=2.5
fMagnetismLookingMult=0.0
fMagnetismStrafeHeadingMult=0.0

[Controls]
fDialogueHardStopAngle1P=180
fDialogueHardStopAngle3P=180
fDialogueSoftStopAngle1P=150
fDialogueSoftStopAngle3P=150
fMouseHeadingSensitivityMax=0.0626
fMouseHeadingXScale=0.0200
fMouseHeadingYScale=0.8500

[Decals]
uMaxSkinDecalPerActor=2

[Display]
bEnableLandFade=0
bEnableSnowMask=0
bEnableSnowRimLighting=0
bLockFrameRate=0
bVolumetricLightingDisableInterior=1
fDDOFFocusCenterweightExt=2
fDecalLifetime=30
fDOFMaxDepthParticipation=10000
fDynamicDOFFarBlur=0.4
fFirstSliceDistance=2000
fGlobalBrightnessBoost=0.0000
fGlobalContrastBoost=0.0000
fGlobalSaturationBoost=0.0000
fLightLODRange=4352
fLightLODStartFade=3500.0
fShadowDirectionalBiasScale=0.47
fSnowGeometrySpecPower=3.00
fSnowNormalSpecPower=2.00
fSnowRimLightIntensity=0.30
fSparklesDensity=0.85
fSparklesSize=6.00
fTreesMidLODSwitchDist=9999999.0000
fWaterSSRIntensity=0.85
iLandscapeMultiNormalTilingFactor=1
sScreenShotBaseName=D:\Steam\steamapps\common\Skyrim Anniversary Edition\ScreenShot

[General]
bBackgroundLoadVMData=1
fFlickeringLightDistance=8192
sIntroSequence=
sLanguage={1}
sTestFile1=Dawnguard.esm
sTestFile2=HearthFires.esm
sTestFile3=Dragonborn.esm
uExterior Cell Buffer=36
uGridsToLoad=5
uInterior Cell Buffer=0
bBorderRegionsEnabled=0

[GeneralWarnings]
SGeneralMasterMismatchWarning=One or more plugins could not find the correct versions of the master files they depend on. Errors may occur during load or game play. Check the 'Warnings.txt' file for more information.

[Grass]
bAllowCreateGrass=1
bAllowLoadGrass=0
bDrawShaderGrass=1
bEnableGrassFade=0
fGrassFadeRange=1588
fGrassMaxStartFadeDistance=7000.0000
fGrassMinStartFadeDistance=0.0000
fGrassWindMagnitudeMax=0
fGrassWindMagnitudeMin=0
iGrassCellRadius=2
iMaxGrassTypesPerTexure=2
iMinGrassSize=40

[HAVOK]
fMaxTime=0.01666667

[Imagespace]
iRadialBlurLevel=2

[Interface]
bShowTutorials=0
uMaxCustomItemNameLength=64

[Launcher]
bEnableFileSelection=1

[LightingShader]
fDecalLODFadeEnd=0.15
fDecalLODFadeStart=0.1
fEnvmapLODFadeEnd=0.25
fEnvmapLODFadeStart=0.2

[LOD]
fDistanceMultiplier=1.00

[MapMenu]
fMapWorldMaxPitch=90
fMapWorldMinPitch=0
fMapWorldYawRange=400

[Menu]
iConsoleSizeScreenPercent=40
iConsoleTextSize=20

[Papyrus]
bEnableLogging=0
bEnableProfiling=0
bEnableTrace=0
bLoadDebugInformation=0
fPostLoadUpdateTimeMS=2000
iMaxAllocatedMemoryBytes=500000

[SaveGame]
iAutoSaveCount=3

[Trees]
bEnableTreeAnimations=0
bEnableTrees=0
fUpdateBudget=0

[Water]
bReflectLODLand=1
bReflectLODObjects=0
bReflectLODTrees=1
bReflectSky=1";

        #endregion

        #region Medium

        const string SkyrimIniMediumAE = @"
[Actor]
bUseNavMeshForMovement=0

[Archive]
bLoadArchiveInMemory=1
sArchiveToLoadInMemoryList=Skyrim - Animations.bsa
sResourceArchiveList=Skyrim - Misc.bsa, Skyrim - Shaders.bsa, Skyrim - Interface.bsa, Skyrim - Animations.bsa, Skyrim - Meshes0.bsa, Skyrim - Meshes1.bsa, Skyrim - Sounds.bsa
sResourceArchiveList2=Skyrim - Voices_{0}0.bsa, Skyrim - Textures0.bsa, Skyrim - Textures1.bsa, Skyrim - Textures2.bsa, Skyrim - Textures3.bsa, Skyrim - Textures4.bsa, Skyrim - Textures5.bsa, Skyrim - Textures6.bsa, Skyrim - Textures7.bsa, Skyrim - Textures8.bsa, Skyrim - Patch.bsa

[Audio]
fMenuModeFadeInTime=1.0
fMenuModeFadeOutTime=3.0
fMusicDuckingSeconds=6.0
fMusicUnDuckingSeconds=8.0
uMaxSizeForCachedSound=4096

[Camera]
fMouseWheelZoomSpeed=10.00

[Combat]
f1PArrowTiltUpAngle=0.7
f1PBoltTiltUpAngle=0.7
f3PArrowTiltUpAngle=2.5
fMagnetismLookingMult=0.0
fMagnetismStrafeHeadingMult=0.0

[Controls]
fDialogueHardStopAngle1P=180
fDialogueHardStopAngle3P=180
fDialogueSoftStopAngle1P=150
fDialogueSoftStopAngle3P=150
fMouseHeadingSensitivityMax=0.0626
fMouseHeadingXScale=0.0200
fMouseHeadingYScale=0.8500

[Decals]
uMaxSkinDecalPerActor=20

[Display]
bEnableLandFade=0
bEnableSnowMask=0
bEnableSnowRimLighting=0
bLockFrameRate=0
bVolumetricLightingDisableInterior=1
fDDOFFocusCenterweightExt=2
fDecalLifetime=240
fDOFMaxDepthParticipation=10000
fDynamicDOFFarBlur=0.4
fFirstSliceDistance=1991
fGlobalBrightnessBoost=0.0000
fGlobalContrastBoost=0.0000
fGlobalSaturationBoost=0.0000
fLightLODRange=6656
fLightLODStartFade=3500.0
fShadowDirectionalBiasScale=0.47
fSnowGeometrySpecPower=3.00
fSnowNormalSpecPower=2.00
fSnowRimLightIntensity=0.30
fSparklesDensity=0.85
fSparklesSize=6.00
fTreesMidLODSwitchDist=9999999.0000
fWaterSSRIntensity=0.85
iLandscapeMultiNormalTilingFactor=1
sScreenShotBaseName=D:\Steam\steamapps\common\Skyrim Anniversary Edition\ScreenShot

[General]
bBackgroundLoadVMData=1
fFlickeringLightDistance=8192
sIntroSequence=
sLanguage={1}
sTestFile1=Dawnguard.esm
sTestFile2=HearthFires.esm
sTestFile3=Dragonborn.esm
uExterior Cell Buffer=36
uGridsToLoad=5
uInterior Cell Buffer=0
bBorderRegionsEnabled=0

[GeneralWarnings]
SGeneralMasterMismatchWarning=One or more plugins could not find the correct versions of the master files they depend on. Errors may occur during load or game play. Check the 'Warnings.txt' file for more information.

[Grass]
bAllowCreateGrass=1
bAllowLoadGrass=0
bDrawShaderGrass=1
bEnableGrassFade=0
fGrassFadeRange=3175
fGrassMaxStartFadeDistance=7000.0000
fGrassMinStartFadeDistance=0.0000
fGrassWindMagnitudeMax=65
fGrassWindMagnitudeMin=5
iGrassCellRadius=2
iMaxGrassTypesPerTexure=2
iMinGrassSize=40

[HAVOK]
fMaxTime=0.01666667

[Imagespace]
iRadialBlurLevel=2

[Interface]
bShowTutorials=0
uMaxCustomItemNameLength=64

[Launcher]
bEnableFileSelection=1

[LightingShader]
fDecalLODFadeEnd=0.3
fDecalLODFadeStart=0.2
fEnvmapLODFadeEnd=0.4
fEnvmapLODFadeStart=0.3

[LOD]
fDistanceMultiplier=1.00

[MapMenu]
fMapWorldMaxPitch=90
fMapWorldMinPitch=0
fMapWorldYawRange=400

[Menu]
iConsoleSizeScreenPercent=40
iConsoleTextSize=20

[Papyrus]
bEnableLogging=0
bEnableProfiling=0
bEnableTrace=0
bLoadDebugInformation=0
fPostLoadUpdateTimeMS=2000
iMaxAllocatedMemoryBytes=500000

[SaveGame]
iAutoSaveCount=3

[Trees]
bEnableTreeAnimations=1
bEnableTrees=1
fUpdateBudget=1.5

[Water]
bReflectLODLand=1
bReflectLODObjects=0
bReflectLODTrees=1
bReflectSky=1";

        #endregion

        #region High

        const string SkyrimIniHighAE = @"
[Actor]
bUseNavMeshForMovement=0

[Archive]
bLoadArchiveInMemory=1
sArchiveToLoadInMemoryList=Skyrim - Animations.bsa
sResourceArchiveList=Skyrim - Misc.bsa, Skyrim - Shaders.bsa, Skyrim - Interface.bsa, Skyrim - Animations.bsa, Skyrim - Meshes0.bsa, Skyrim - Meshes1.bsa, Skyrim - Sounds.bsa
sResourceArchiveList2=Skyrim - Voices_{0}0.bsa, Skyrim - Textures0.bsa, Skyrim - Textures1.bsa, Skyrim - Textures2.bsa, Skyrim - Textures3.bsa, Skyrim - Textures4.bsa, Skyrim - Textures5.bsa, Skyrim - Textures6.bsa, Skyrim - Textures7.bsa, Skyrim - Textures8.bsa, Skyrim - Patch.bsa

[Audio]
fMenuModeFadeInTime=1.0
fMenuModeFadeOutTime=3.0
fMusicDuckingSeconds=6.0
fMusicUnDuckingSeconds=8.0
uMaxSizeForCachedSound=4096

[Camera]
fMouseWheelZoomSpeed=10.00

[Combat]
f1PArrowTiltUpAngle=0.7
f1PBoltTiltUpAngle=0.7
f3PArrowTiltUpAngle=2.5
fMagnetismLookingMult=0.0
fMagnetismStrafeHeadingMult=0.0

[Controls]
fDialogueHardStopAngle1P=180
fDialogueHardStopAngle3P=180
fDialogueSoftStopAngle1P=150
fDialogueSoftStopAngle3P=150
fMouseHeadingSensitivityMax=0.0626
fMouseHeadingXScale=0.0200
fMouseHeadingYScale=0.8500

[Decals]
uMaxSkinDecalPerActor=40

[Display]
bEnableLandFade=0
bEnableSnowMask=0
bEnableSnowRimLighting=0
bLockFrameRate=0
bVolumetricLightingDisableInterior=1
fDDOFFocusCenterweightExt=2
fDecalLifetime=300
fDOFMaxDepthParticipation=10000
fDynamicDOFFarBlur=0.4
fFirstSliceDistance=2896
fGlobalBrightnessBoost=0.0000
fGlobalContrastBoost=0.0000
fGlobalSaturationBoost=0.0000
fLightLODRange=24576
fLightLODStartFade=3500.0
fShadowDirectionalBiasScale=0.47
fSnowGeometrySpecPower=3.00
fSnowNormalSpecPower=2.00
fSnowRimLightIntensity=0.30
fSparklesDensity=0.85
fSparklesSize=6.00
fTreesMidLODSwitchDist=9999999.0000
fWaterSSRIntensity=0.85
iLandscapeMultiNormalTilingFactor=1
sScreenShotBaseName=D:\Steam\steamapps\common\Skyrim Anniversary Edition\ScreenShot

[General]
bBackgroundLoadVMData=1
fFlickeringLightDistance=8192
sIntroSequence=
sLanguage={1}
sTestFile1=Dawnguard.esm
sTestFile2=HearthFires.esm
sTestFile3=Dragonborn.esm
uExterior Cell Buffer=36
uGridsToLoad=5
uInterior Cell Buffer=0
bBorderRegionsEnabled=0

[GeneralWarnings]
SGeneralMasterMismatchWarning=One or more plugins could not find the correct versions of the master files they depend on. Errors may occur during load or game play. Check the 'Warnings.txt' file for more information.

[Grass]
bAllowCreateGrass=1
bAllowLoadGrass=0
bDrawShaderGrass=1
bEnableGrassFade=0
fGrassFadeRange=6144
fGrassMaxStartFadeDistance=7000.0000
fGrassMinStartFadeDistance=0.0000
fGrassWindMagnitudeMax=125
fGrassWindMagnitudeMin=5
iGrassCellRadius=2
iMaxGrassTypesPerTexure=3
iMinGrassSize=40

[HAVOK]
fMaxTime=0.01666667

[Imagespace]
iRadialBlurLevel=2

[Interface]
bShowTutorials=0
uMaxCustomItemNameLength=64

[Launcher]
bEnableFileSelection=1

[LightingShader]
fDecalLODFadeEnd=0.6
fDecalLODFadeStart=0.5
fEnvmapLODFadeEnd=0.7
fEnvmapLODFadeStart=0.6

[LOD]
fDistanceMultiplier=1.00

[MapMenu]
fMapWorldMaxPitch=90
fMapWorldMinPitch=0
fMapWorldYawRange=400

[Menu]
iConsoleSizeScreenPercent=40
iConsoleTextSize=20

[Papyrus]
bEnableLogging=0
bEnableProfiling=0
bEnableTrace=0
bLoadDebugInformation=0
fPostLoadUpdateTimeMS=2000
iMaxAllocatedMemoryBytes=500000

[SaveGame]
iAutoSaveCount=3

[Trees]
bEnableTreeAnimations=1
bEnableTrees=1
fUpdateBudget=1.5

[Water]
bReflectLODLand=1
bReflectLODObjects=1
bReflectLODTrees=1
bReflectSky=1";

        #endregion
        
        #endregion

        #region SkyrimPref.ini SE        

        #region Low

        const string SkyrimPrefIniLow = @"[AudioMenu]
fAudioMasterVolume=0.5000
fVal0=0.4000
fVal1=0.2000
fVal2=1.0000
fVal3=0.4000
fVal4=0.4000
fVal5=0.3000
fVal6=0.3000
fVal7=0.2000
uID0=94881
uID1=522251
uID2=410705
uID3=106685
uID4=1007612
uID5=402696814
uID6=403069041
uID7=554685

[Bethesda.net]
uPersistentUuidData0=3840114968
uPersistentUuidData1=1094403070
uPersistentUuidData2=2392617889
uPersistentUuidData3=925085131

[Clouds]
fCloudLevel0Distance=16384
fCloudLevel1Distance=32768
fCloudLevel2Distance=262144
fCloudNearFadeDistance=9000

[Controls]
bAlwaysRunByDefault=1
bGamePadRumble=1
bInvertYValues=0
bUseKinect=0
fGamepadHeadingSensitivity=0.6667
fMouseHeadingSensitivity=0.0125

[Decals]
bDecals=1
bSkinnedDecals=1
uMaxDecals=20
uMaxSkinDecals=3

[Display]
bBorderless=1
bDrawLandShadows=0
bEnableImprovedSnow=0
bEnableProjecteUVDiffuseNormals=1
bForceCreateTarget=0
bFull Screen=0
bFXAAEnabled=1
bIBLFEnable=0
bIndEnable=0
bSAO_CS_Enable=0
bSAOEnable=0
bScreenSpaceReflectionEnabled=0
bToggleSparkles=0
bTreesReceiveShadows=0
bUse64bitsHDRRenderTarget=0
bUsePrecipitationOcclusion=0
bUseTAA=0
bVolumetricLightingEnable=0
fDynamicDOFBlurMultiplier=0.8
fFocusShadowMapDoubleEveryXUnit=450
fGamma=1.0000
fInteriorShadowDistance=3000
fLeafAnimDampenDistEnd=4600
fLeafAnimDampenDistStart=3600
fLightLODStartFade=510
fMeshLODFadeBoundDefault=256
fMeshLODFadePercentDefault=1.2
fMeshLODLevel1FadeDist=3840
fMeshLODLevel1FadeTreeDistance=2560
fMeshLODLevel2FadeDist=2048
fMeshLODLevel2FadeTreeDistance=1792
fProjectedUVDiffuseNormalTilingScale=0.2
fProjectedUVNormalDetailTilingScale=0.8
fShadowDistance=2800
fTreesMidLODSwitchDist=2560
iMaxDecalsPerFrame=10
iMaxSkinDecalsPerFrame=3
iNumFocusShadow=1
iNumSplits=2
iReflectionResolutionDivider=2
iSaveGameScreenShotHeight=192
iSaveGameScreenShotHeighWSt=192
iSaveGameScreenShotWidth=256
iSaveGameScreenShotWidthWS=320
iScreenShotIndex=0
iShadowMapResolution=512
iShadowMaskQuarter=4
iSize H={0}
iSize W={1}
iVolumetricLightingQuality=0
iVSyncPresentInterval=0
uBookRatio=2

[GamePlay]
bShowFloatingQuestMarkers=1
bShowQuestMarkers=1
iDifficulty=0

[General]
bEnableStoryManagerLogging=0
fLightingOutputColourClampPostEnv=1
fLightingOutputColourClampPostLit=1
fLightingOutputColourClampPostSpec=1
iStoryManagerLoggingEvent=-1
uLargeRefLODGridSize=5

[Grass]
fGrassMaxStartFadeDistance=4700
fGrassMinStartFadeDistance=0
fGrassStartFadeDistance=700

[Imagespace]
bDoDepthOfField=1
bLensFlare=0

[Interface]
bDialogueSubtitles=1
bGeneralSubtitles=1
bShowCompass=1
fMouseCursorSpeed=1.00

[LOD]
fLODFadeOutMultActors=3.0
fLODFadeOutMultItems=1.5
fLODFadeOutMultObjects=3.0
fLODFadeOutMultSkyCell=1

[MAIN]
bCrosshairEnabled=1
bGamepadEnable=0
bSaveOnPause=0
bSaveOnRest=0
bSaveOnTravel=0
bSaveOnWait=0
fHUDOpacity=1.0000
fSkyCellRefFadeDistance=300000

[NavMesh]
fCoverSideHighAlpha=0.8000
fCoverSideLowAlpha=0.6500
fEdgeDistFromVert=10.0000
fEdgeFullAlpha=1.0000
fEdgeHighAlpha=0.7500
fEdgeLowAlpha=0.5000
fEdgeThickness=10.0000
fLedgeBoxHalfHeight=25.0000
fObstacleAlpha=0.5000
fPointSize=2.5000
fTriangleFullAlpha=0.7000
fTriangleHighAlpha=0.3500
fTriangleLowAlpha=0.2000

[Particles]
iMaxDesired=250

[SaveGame]
fAutosaveEveryXMins=15.0000

[TerrainManager]
bShowLODInEditor=1
fBlockLevel0Distance=12544
fBlockLevel1Distance=21248
fBlockMaximumDistance=72448
fSplitDistanceMult=0.400
fTreeLoadDistance=20224

[Trees]
bRenderSkinnedTrees=0
uiMaxSkinnedTreesToRender=20

[Water]
bUseWaterDepth=1
bUseWaterDisplacements=1
bUseWaterReflections=1
bUseWaterRefractions=1";

        #endregion

        #region Medium

        const string SkyrimPrefIniMedium = @"[AudioMenu]
fAudioMasterVolume=0.5000
fVal0=0.4000
fVal1=0.2000
fVal2=1.0000
fVal3=0.4000
fVal4=0.4000
fVal5=0.3000
fVal6=0.3000
fVal7=0.2000
uID0=94881
uID1=522251
uID2=410705
uID3=106685
uID4=1007612
uID5=402696814
uID6=403069041
uID7=554685

[Bethesda.net]
uPersistentUuidData0=3840114968
uPersistentUuidData1=1094403070
uPersistentUuidData2=2392617889
uPersistentUuidData3=925085131

[Clouds]
fCloudLevel0Distance=16384
fCloudLevel1Distance=32768
fCloudLevel2Distance=262144
fCloudNearFadeDistance=9000

[Controls]
bAlwaysRunByDefault=1
bGamePadRumble=1
bInvertYValues=0
bUseKinect=0
fGamepadHeadingSensitivity=0.6667
fMouseHeadingSensitivity=0.0125

[Decals]
bDecals=1
bSkinnedDecals=1
uMaxDecals=100
uMaxSkinDecals=35

[Display]
bBorderless=1
bDrawLandShadows=1
bEnableImprovedSnow=0
bEnableProjecteUVDiffuseNormals=1
bForceCreateTarget=0
bFull Screen=0
bFXAAEnabled=0
bIBLFEnable=1
bIndEnable=0
bSAO_CS_Enable=0
bSAOEnable=0
bScreenSpaceReflectionEnabled=1
bToggleSparkles=0
bTreesReceiveShadows=0
bUse64bitsHDRRenderTarget=0
bUsePrecipitationOcclusion=1
bUseTAA=1
bVolumetricLightingEnable=1
fDynamicDOFBlurMultiplier=0.8
fFocusShadowMapDoubleEveryXUnit=450
fGamma=1.0000
fInteriorShadowDistance=3000
fLeafAnimDampenDistEnd=4600
fLeafAnimDampenDistStart=3600
fLightLODStartFade=1069
fMeshLODFadeBoundDefault=256
fMeshLODFadePercentDefault=1.2
fMeshLODLevel1FadeDist=5376
fMeshLODLevel1FadeTreeDistance=3584
fMeshLODLevel2FadeDist=3456
fMeshLODLevel2FadeTreeDistance=1920
fProjectedUVDiffuseNormalTilingScale=0.2
fProjectedUVNormalDetailTilingScale=0.8
fShadowDistance=3000
fTreesMidLODSwitchDist=3584
iMaxDecalsPerFrame=60
iMaxSkinDecalsPerFrame=35
iNumFocusShadow=2
iNumSplits=2
iReflectionResolutionDivider=2
iSaveGameScreenShotHeight=192
iSaveGameScreenShotHeighWSt=192
iSaveGameScreenShotWidth=256
iSaveGameScreenShotWidthWS=320
iScreenShotIndex=0
iShadowMapResolution=1024
iShadowMaskQuarter=4
iSize H={0}
iSize W={1}
iVolumetricLightingQuality=1
iVSyncPresentInterval=0
uBookRatio=2

[GamePlay]
bShowFloatingQuestMarkers=1
bShowQuestMarkers=1
iDifficulty=0

[General]
bEnableStoryManagerLogging=0
fLightingOutputColourClampPostEnv=1
fLightingOutputColourClampPostLit=1
fLightingOutputColourClampPostSpec=1
iStoryManagerLoggingEvent=-1
uLargeRefLODGridSize=7

[Grass]
fGrassMaxStartFadeDistance=5000
fGrassMinStartFadeDistance=0
fGrassStartFadeDistance=1000

[Imagespace]
bDoDepthOfField=1
bLensFlare=1

[Interface]
bDialogueSubtitles=1
bGeneralSubtitles=1
bShowCompass=1
fMouseCursorSpeed=1.00

[LOD]
fLODFadeOutMultActors=6.0
fLODFadeOutMultItems=3.0
fLODFadeOutMultObjects=5.0
fLODFadeOutMultSkyCell=1

[MAIN]
bCrosshairEnabled=1
bGamepadEnable=0
bSaveOnPause=0
bSaveOnRest=0
bSaveOnTravel=0
bSaveOnWait=0
fHUDOpacity=1.0000
fSkyCellRefFadeDistance=600000

[NavMesh]
fCoverSideHighAlpha=0.8000
fCoverSideLowAlpha=0.6500
fEdgeDistFromVert=10.0000
fEdgeFullAlpha=1.0000
fEdgeHighAlpha=0.7500
fEdgeLowAlpha=0.5000
fEdgeThickness=10.0000
fLedgeBoxHalfHeight=25.0000
fObstacleAlpha=0.5000
fPointSize=2.5000
fTriangleFullAlpha=0.7000
fTriangleHighAlpha=0.3500
fTriangleLowAlpha=0.2000

[Particles]
iMaxDesired=750

[SaveGame]
fAutosaveEveryXMins=15.0000

[TerrainManager]
bShowLODInEditor=1
fBlockLevel0Distance=20000
fBlockLevel1Distance=42000
fBlockMaximumDistance=90000
fSplitDistanceMult=0.750
fTreeLoadDistance=25600

[Trees]
bRenderSkinnedTrees=1
uiMaxSkinnedTreesToRender=75

[Water]
bUseWaterDepth=1
bUseWaterDisplacements=1
bUseWaterReflections=1
bUseWaterRefractions=1";

        #endregion

        #region High

        const string SkyrimPrefIniHigh =
@"[Clouds]
fCloudLevel0Distance=16384
fCloudLevel1Distance=32768
fCloudLevel2Distance=262144
fCloudNearFadeDistance=9000

[Controls]
fMouseHeadingSensitivity=0.0125

[Decals]
bDecals=1
bSkinnedDecals=1
uMaxDecals=200
uMaxSkinDecals=50

[Display]
bBorderless=1
bDrawLandShadows=1
bEnableImprovedSnow=0
bEnableProjecteUVDiffuseNormals=1
bFXAAEnabled=0
bIBLFEnable=0
bIndEnable=0
bSAO_CS_Enable=0
bSAOEnable=0
bScreenSpaceReflectionEnabled=1
bToggleSparkles=0
bTreesReceiveShadows=1
bUse64bitsHDRRenderTarget=0
bUsePrecipitationOcclusion=1
bUseTAA=1
bVolumetricLightingEnable=1
fDynamicDOFBlurMultiplier=0.8
fFocusShadowMapDoubleEveryXUnit=450
fGamma=1.0000
fInteriorShadowDistance=3000
fLeafAnimDampenDistEnd=4600
fLeafAnimDampenDistStart=3600
fLightLODStartFade=3750
fMeshLODFadeBoundDefault=256
fMeshLODFadePercentDefault=1.2
fMeshLODLevel1FadeDist=10240
fMeshLODLevel1FadeTreeDistance=8960
fMeshLODLevel2FadeDist=7680
fMeshLODLevel2FadeTreeDistance=5376
fProjectedUVDiffuseNormalTilingScale=0.2
fProjectedUVNormalDetailTilingScale=0.8
fShadowDistance=8000
fTreesMidLODSwitchDist=8960
iMaxDecalsPerFrame=120
iMaxSkinDecalsPerFrame=50
iNumFocusShadow=4
iReflectionResolutionDivider=2
iScreenShotIndex=0
iShadowMapResolution=2048
iShadowMaskQuarter=4
iVolumetricLightingQuality=0
iVSyncPresentInterval=0
iSize H={0}
iSize W={1}

[General]
fLightingOutputColourClampPostEnv=1
fLightingOutputColourClampPostLit=1
fLightingOutputColourClampPostSpec=1
uLargeRefLODGridSize=9

[Grass]
fGrassMaxStartFadeDistance=6500
fGrassMinStartFadeDistance=0
fGrassStartFadeDistance=2500

[Imagespace]
bDoDepthOfField=1
bLensFlare=1

[Interface]
fMouseCursorSpeed=1.00

[LOD]
fLODFadeOutMultActors=12.0
fLODFadeOutMultItems=4.5
fLODFadeOutMultObjects=15.0
fLODFadeOutMultSkyCell=1

[MAIN]
fSkyCellRefFadeDistance=600000

[Particles]
iMaxDesired=7000

[TerrainManager]
fBlockLevel0Distance=46443
fBlockLevel1Distance=69665
fBlockMaximumDistance=92887
fSplitDistanceMult=1.089
fTreeLoadDistance=46443

[Trees]
bRenderSkinnedTrees=1
uiMaxSkinnedTreesToRender=120

[Water]
bUseWaterDepth=1
bUseWaterDisplacements=1
bUseWaterReflections=1
bUseWaterRefractions=1";

        #endregion

        #endregion

        #region SkyrimPref.ini AE

        #region Low

        const string SkyrimPrefIniLowAE = @"
[AudioMenu]
fAudioMasterVolume=1.0000
fVal0=0.7000
fVal1=0.7500
fVal2=0.6500
fVal3=0.2000
fVal4=0.4500
fVal5=1.0000
fVal6=0.7000
fVal7=0.2500
uID0=94881
uID1=1007612
uID2=554685
uID3=466532
uID4=522251
uID5=410705
uID6=106685
uID7=755018350

[Bethesda.net]
uPersistentUuidData0=2142338021
uPersistentUuidData1=1211322199
uPersistentUuidData2=2989538343
uPersistentUuidData3=2752784467

[Clouds]
fCloudLevel0Distance=16384.0000
fCloudLevel1Distance=32768.0000
fCloudLevel2Distance=262144.0000
fCloudNearFadeDistance=9000.0000

[Controls]
bAlwaysRunByDefault=1
bGamePadRumble=1
bInvertYValues=0
bUseKinect=0
fGamepadHeadingSensitivity=0.8889
fMouseHeadingSensitivity=0.0126

[Decals]
bDecals=1
bSkinnedDecals=1
uMaxDecals=20
uMaxSkinDecals=3

[Display]
bBorderless=1
bDrawLandShadows=0
bEnableImprovedSnow=0
bEnableProjecteUVDiffuseNormals=1
bForceCreateTarget=0
bFull Screen=0
bFXAAEnabled=1
bIBLFEnable=0
bIndEnable=0
bSAO_CS_Enable=0
bSAOEnable=0
bScreenSpaceReflectionEnabled=0
bToggleSparkles=0
bTreesReceiveShadows=0
bUse64bitsHDRRenderTarget=1
bUsePrecipitationOcclusion=0
bVolumetricLightingEnable=0
fDynamicDOFBlurMultiplier=0.8
ffocusShadowMapDoubleEveryXUnit=450
fGamma=1.0000
fInteriorShadowDistance=3000
fLeafAnimDampenDistEnd=4600
fLeafAnimDampenDistStart=3600
fLightLODStartFade=768
fMeshLODFadeBoundDefault=256
fMeshLODFadePercentDefault=1.2
fMeshLODLevel1FadeDist=2896
fMeshLODLevel1FadeTreeDistance=2896
fMeshLODLevel2FadeDist=4096
fMeshLODLevel2FadeTreeDistance=4096
fProjectedUVDiffuseNormalTilingScale=0.2
fProjectedUVNormalDetailTilingScale=0.8
fShadowDistance=2896
fTreesMidLODSwitchDist=4096
iMaxDecalsPerFrame=10
iMaxSkinDecalsPerFrame=3
iNumFocusShadow=1
iNumSplits=2
iReflectionResolutionDivider=1
iSaveGameScreenShotHeight=192
iSaveGameScreenShotHeighWSt=192
iSaveGameScreenShotWidth=256
iSaveGameScreenShotWidthWS=320
iScreenShotIndex=25
iShadowMapResolution=512
iShadowMaskQuarter=4
iSize H={0}
iSize W={1}
bUseTAA={2}
iVolumetricLightingQuality=0
iVSyncPresentInterval=0
uBookRatio=2

[GamePlay]
bShowFloatingQuestMarkers=1
bShowQuestMarkers=1
iDifficulty=2

[General]
bEnableStoryManagerLogging=0
bFreebiesSeen=1
fLightingOutputColourClampPostEnv=1.0000
fLightingOutputColourClampPostLit=1.0000
fLightingOutputColourClampPostSpec=1.0000
iStoryManagerLoggingEvent=-1
uLargeRefLODGridSize=5

[Grass]
fGrassMaxStartFadeDistance=6144
fGrassMinStartFadeDistance=0
fGrassStartFadeDistance=690

[Imagespace]
bDoDepthOfField=1
bLensFlare=0

[Interface]
bDialogueSubtitles=1
bGeneralSubtitles=0
bShowCompass=1
fMouseCursorSpeed=1.00

[Launcher]
sD3DDevice='NVIDIA GeForce RTX 3090'

[LOD]
fLODFadeOutMultActors=3.0
fLODFadeOutMultItems=1.5
fLODFadeOutMultObjects=3.0
fLODFadeOutMultSkyCell=1

[MAIN]
bCrosshairEnabled=1
bGamepadEnable=1
bSaveOnPause=0
bSaveOnRest=0
bSaveOnTravel=0
bSaveOnWait=0
fHUDOpacity=1.0000
fSkyCellRefFadeDistance=196608

[NavMesh]
fCoverSideHighAlpha=0.8000
fCoverSideLowAlpha=0.6500
fEdgeDistFromVert=10.0000
fEdgeFullAlpha=1.0000
fEdgeHighAlpha=0.7500
fEdgeLowAlpha=0.5000
fEdgeThickness=10.0000
fLedgeBoxHalfHeight=25.0000
fObstacleAlpha=0.5000
fPointSize=2.5000
fTriangleFullAlpha=0.7000
fTriangleHighAlpha=0.3500
fTriangleLowAlpha=0.2000

[Particles]
iMaxDesired=250

[SaveGame]
fAutosaveEveryXMins=15.0000

[TerrainManager]
bShowLODInEditor=1
fBlockLevel0Distance=16384
fBlockLevel1Distance=32768
fBlockMaximumDistance=131072
fSplitDistanceMult=1.000
fTreeLoadDistance=24576

[Trees]
bRenderSkinnedTrees=0
uiMaxSkinnedTreesToRender=20

[Water]
bUseWaterDepth=1
bUseWaterDisplacements=1
bUseWaterReflections=1
bUseWaterRefractions=1";

        #endregion

        #region Medium

        const string SkyrimPrefIniMediumAE = @"
[AudioMenu]
fAudioMasterVolume=1.0000
fVal0=0.7000
fVal1=0.7500
fVal2=0.6500
fVal3=0.2000
fVal4=0.4500
fVal5=1.0000
fVal6=0.7000
fVal7=0.2500
uID0=94881
uID1=1007612
uID2=554685
uID3=466532
uID4=522251
uID5=410705
uID6=106685
uID7=755018350

[Bethesda.net]
uPersistentUuidData0=2142338021
uPersistentUuidData1=1211322199
uPersistentUuidData2=2989538343
uPersistentUuidData3=2752784467

[Clouds]
fCloudLevel0Distance=16384.0000
fCloudLevel1Distance=32768.0000
fCloudLevel2Distance=262144.0000
fCloudNearFadeDistance=9000.0000

[Controls]
bAlwaysRunByDefault=1
bGamePadRumble=1
bInvertYValues=0
bUseKinect=0
fGamepadHeadingSensitivity=0.8889
fMouseHeadingSensitivity=0.0126

[Decals]
bDecals=1
bSkinnedDecals=1
uMaxDecals=100
uMaxSkinDecals=35

[Display]
bBorderless=1
bDrawLandShadows=1
bEnableImprovedSnow=0
bEnableProjecteUVDiffuseNormals=1
bForceCreateTarget=0
bFull Screen=0
bFXAAEnabled=0
bIBLFEnable=0
bIndEnable=0
bSAO_CS_Enable=0
bSAOEnable=0
bScreenSpaceReflectionEnabled=1
bToggleSparkles=0
bTreesReceiveShadows=1
bUse64bitsHDRRenderTarget=1
bUsePrecipitationOcclusion=1
bVolumetricLightingEnable=1
fDynamicDOFBlurMultiplier=0.8
ffocusShadowMapDoubleEveryXUnit=450
fGamma=1.0000
fInteriorShadowDistance=3000
fLeafAnimDampenDistEnd=4600
fLeafAnimDampenDistStart=3600
fLightLODStartFade=6144
fMeshLODFadeBoundDefault=256
fMeshLODFadePercentDefault=1.2
fMeshLODLevel1FadeDist=4096
fMeshLODLevel1FadeTreeDistance=4096
fMeshLODLevel2FadeDist=8192
fMeshLODLevel2FadeTreeDistance=8192
fProjectedUVDiffuseNormalTilingScale=0.2
fProjectedUVNormalDetailTilingScale=0.8
fShadowDistance=3620
fTreesMidLODSwitchDist=8192
iMaxDecalsPerFrame=60
iMaxSkinDecalsPerFrame=35
iNumFocusShadow=2
iNumSplits=2
iReflectionResolutionDivider=1
iSaveGameScreenShotHeight=192
iSaveGameScreenShotHeighWSt=192
iSaveGameScreenShotWidth=256
iSaveGameScreenShotWidthWS=320
iScreenShotIndex=25
iShadowMapResolution=1024
iShadowMaskQuarter=4
iSize H={0}
iSize W={1}
bUseTAA={2}
iVolumetricLightingQuality=1
iVSyncPresentInterval=0
uBookRatio=2

[GamePlay]
bShowFloatingQuestMarkers=1
bShowQuestMarkers=1
iDifficulty=2

[General]
bEnableStoryManagerLogging=0
bFreebiesSeen=1
fLightingOutputColourClampPostEnv=1.0000
fLightingOutputColourClampPostLit=1.0000
fLightingOutputColourClampPostSpec=1.0000
iStoryManagerLoggingEvent=-1
uLargeRefLODGridSize=7

[Grass]
fGrassMaxStartFadeDistance=6144
fGrassMinStartFadeDistance=0
fGrassStartFadeDistance=1381

[Imagespace]
bDoDepthOfField=1
bLensFlare=0

[Interface]
bDialogueSubtitles=1
bGeneralSubtitles=0
bShowCompass=1
fMouseCursorSpeed=1.00

[Launcher]
sD3DDevice='NVIDIA GeForce RTX 3090'

[LOD]
fLODFadeOutMultActors=6.0
fLODFadeOutMultItems=3.0
fLODFadeOutMultObjects=5.0
fLODFadeOutMultSkyCell=1

[MAIN]
bCrosshairEnabled=1
bGamepadEnable=1
bSaveOnPause=0
bSaveOnRest=0
bSaveOnTravel=0
bSaveOnWait=0
fHUDOpacity=1.0000
fSkyCellRefFadeDistance=262144

[NavMesh]
fCoverSideHighAlpha=0.8000
fCoverSideLowAlpha=0.6500
fEdgeDistFromVert=10.0000
fEdgeFullAlpha=1.0000
fEdgeHighAlpha=0.7500
fEdgeLowAlpha=0.5000
fEdgeThickness=10.0000
fLedgeBoxHalfHeight=25.0000
fObstacleAlpha=0.5000
fPointSize=2.5000
fTriangleFullAlpha=0.7000
fTriangleHighAlpha=0.3500
fTriangleLowAlpha=0.2000

[Particles]
iMaxDesired=750

[SaveGame]
fAutosaveEveryXMins=15.0000

[TerrainManager]
bShowLODInEditor=1
fBlockLevel0Distance=32768
fBlockLevel1Distance=81920
fBlockMaximumDistance=196608
fSplitDistanceMult=1.000
fTreeLoadDistance=32768

[Trees]
bRenderSkinnedTrees=1
uiMaxSkinnedTreesToRender=75

[Water]
bUseWaterDepth=1
bUseWaterDisplacements=1
bUseWaterReflections=1
bUseWaterRefractions=1";

        #endregion

        #region High

        const string SkyrimPrefIniHighAE = @"[AudioMenu]
fAudioMasterVolume=1.0000
fVal0=0.7000
fVal1=0.7500
fVal2=0.6500
fVal3=0.2000
fVal4=0.4500
fVal5=1.0000
fVal6=0.7000
fVal7=0.2500
uID0=94881
uID1=1007612
uID2=554685
uID3=466532
uID4=522251
uID5=410705
uID6=106685
uID7=755018350

[Bethesda.net]
uPersistentUuidData0=2142338021
uPersistentUuidData1=1211322199
uPersistentUuidData2=2989538343
uPersistentUuidData3=2752784467

[Clouds]
fCloudLevel0Distance=16384.0000
fCloudLevel1Distance=32768.0000
fCloudLevel2Distance=262144.0000
fCloudNearFadeDistance=9000.0000

[Controls]
bAlwaysRunByDefault=1
bGamePadRumble=1
bInvertYValues=0
bUseKinect=0
fGamepadHeadingSensitivity=0.8889
fMouseHeadingSensitivity=0.0126

[Decals]
bDecals=1
bSkinnedDecals=1
uMaxDecals=200
uMaxSkinDecals=50

[Display]
bBorderless=1
bDrawLandShadows=1
bEnableImprovedSnow=0
bEnableProjecteUVDiffuseNormals=1
bForceCreateTarget=0
bFull Screen=0
bFXAAEnabled=0
bIBLFEnable=0
bIndEnable=0
bSAO_CS_Enable=0
bSAOEnable=0
bScreenSpaceReflectionEnabled=1
bToggleSparkles=0
bTreesReceiveShadows=1
bUse64bitsHDRRenderTarget=1
bUsePrecipitationOcclusion=1
bVolumetricLightingEnable=1
fDynamicDOFBlurMultiplier=0.8000
ffocusShadowMapDoubleEveryXUnit=450.0000
fGamma=1.0000
fInteriorShadowDistance=3000.0000
fLeafAnimDampenDistEnd=4600.0000
fLeafAnimDampenDistStart=3600.0000
fLightLODStartFade=6144.0000
fMeshLODFadeBoundDefault=256.0000
fMeshLODFadePercentDefault=1.2000
fMeshLODLevel1FadeDist=6144.0000
fMeshLODLevel1FadeTreeDistance=6144.0000
fMeshLODLevel2FadeDist=10240.0000
fMeshLODLevel2FadeTreeDistance=10240.0000
fProjectedUVDiffuseNormalTilingScale=0.2000
fProjectedUVNormalDetailTilingScale=0.8000
fShadowDistance=8145.0000
fTreesMidLODSwitchDist=10240.0000
iMaxDecalsPerFrame=500
iMaxSkinDecalsPerFrame=150
iNumFocusShadow=4
iNumSplits=2
iReflectionResolutionDivider=1
iSaveGameScreenShotHeight=192
iSaveGameScreenShotHeighWSt=192
iSaveGameScreenShotWidth=256
iSaveGameScreenShotWidthWS=320
iScreenShotIndex=25
iShadowMapResolution=2048
iShadowMaskQuarter=4
iSize H={0}
iSize W={1}
bUseTAA={2}
iVolumetricLightingQuality=2
iVSyncPresentInterval=0
uBookRatio=2

[GamePlay]
bShowFloatingQuestMarkers=1
bShowQuestMarkers=1
iDifficulty=2

[General]
bEnableStoryManagerLogging=0
bFreebiesSeen=1
fLightingOutputColourClampPostEnv=1.0000
fLightingOutputColourClampPostLit=1.0000
fLightingOutputColourClampPostSpec=1.0000
iStoryManagerLoggingEvent=-1
uLargeRefLODGridSize=9

[Grass]
fGrassMaxStartFadeDistance=6144.0000
fGrassMinStartFadeDistance=0.0000
fGrassStartFadeDistance=6144.0000

[Imagespace]
bDoDepthOfField=1
bLensFlare=0

[Interface]
bDialogueSubtitles=1
bGeneralSubtitles=0
bShowCompass=1
fMouseCursorSpeed=1.0000

[Launcher]
sD3DDevice='NVIDIA GeForce RTX 3090'

[LOD]
fLODFadeOutMultActors=12.0000
fLODFadeOutMultItems=4.5000
fLODFadeOutMultObjects=15.0000
fLODFadeOutMultSkyCell=1.0000

[MAIN]
bCrosshairEnabled=1
bGamepadEnable=1
bSaveOnPause=0
bSaveOnRest=0
bSaveOnTravel=0
bSaveOnWait=0
fHUDOpacity=1.0000
fSkyCellRefFadeDistance=327680.0000

[NavMesh]
fCoverSideHighAlpha=0.8000
fCoverSideLowAlpha=0.6500
fEdgeDistFromVert=10.0000
fEdgeFullAlpha=1.0000
fEdgeHighAlpha=0.7500
fEdgeLowAlpha=0.5000
fEdgeThickness=10.0000
fLedgeBoxHalfHeight=25.0000
fObstacleAlpha=0.5000
fPointSize=2.5000
fTriangleFullAlpha=0.7000
fTriangleHighAlpha=0.3500
fTriangleLowAlpha=0.2000

[Particles]
iMaxDesired=7000

[SaveGame]
fAutosaveEveryXMins=15.0000

[TerrainManager]
bShowLODInEditor=1
fBlockLevel0Distance=80000.0000
fBlockLevel1Distance=150000.0000
fBlockMaximumDistance=262144.0000
fSplitDistanceMult=1.0000
fTreeLoadDistance=140000.0000

[Trees]
bRenderSkinnedTrees=1
uiMaxSkinnedTreesToRender=120

[Water]
bUseWaterDepth=1
bUseWaterDisplacements=1
bUseWaterReflections=1
bUseWaterRefractions=1";

        #endregion
        
        #endregion

        #region Settings.ini

        const string SettingsIni =
@"[General]
LocalSaves=true
LocalSettings=true
AutomaticArchiveInvalidation=false
";

        #endregion

        #region LoadOrder

        const string LoadOrderAE =
@"# This file was automatically generated by Mod Organizer.
Skyrim.esm
Update.esm
Dawnguard.esm
HearthFires.esm
Dragonborn.esm
ccasvsse001-almsivi.esm
ccbgssse001-fish.esm
ccbgssse002-exoticarrows.esl
ccbgssse003-zombies.esl
ccbgssse004-ruinsedge.esl
ccbgssse005-goldbrand.esl
ccbgssse006-stendarshammer.esl
ccbgssse007-chrysamere.esl
ccbgssse010-petdwarvenarmoredmudcrab.esl
ccbgssse011-hrsarmrelvn.esl
ccbgssse012-hrsarmrstl.esl
ccbgssse014-spellpack01.esl
ccbgssse019-staffofsheogorath.esl
ccbgssse020-graycowl.esl
ccbgssse021-lordsmail.esl
ccmtysse001-knightsofthenine.esl
ccqdrsse001-SurvivalMode.esl
cctwbsse001-puzzledungeon.esm
cceejsse001-hstead.esm
ccqdrsse002-firewood.esl
ccbgssse018-shadowrend.esl
ccbgssse035-petnhound.esl
ccfsvsse001-backpacks.esl
cceejsse002-tower.esl
ccedhsse001-norjewel.esl
ccvsvsse002-pets.esl
ccbgssse037-Curios.esl
ccbgssse034-mntuni.esl
ccbgssse045-hasedoki.esl
ccbgssse008-wraithguard.esl
ccbgssse036-petbwolf.esl
ccffbsse001-imperialdragon.esl
ccmtysse002-ve.esl
ccbgssse043-crosselv.esl
ccvsvsse001-winter.esl
cceejsse003-hollow.esl
ccbgssse016-umbra.esm
ccbgssse031-advcyrus.esm
ccbgssse038-bowofshadows.esl
ccbgssse040-advobgobs.esl
ccbgssse050-ba_daedric.esl
ccbgssse052-ba_iron.esl
ccbgssse054-ba_orcish.esl
ccbgssse058-ba_steel.esl
ccbgssse059-ba_dragonplate.esl
ccbgssse061-ba_dwarven.esl
ccpewsse002-armsofchaos.esl
ccbgssse041-netchleather.esl
ccedhsse002-splkntset.esl
ccbgssse064-ba_elven.esl
ccbgssse063-ba_ebony.esl
ccbgssse062-ba_dwarvenmail.esl
ccbgssse060-ba_dragonscale.esl
ccbgssse056-ba_silver.esl
ccbgssse055-ba_orcishscaled.esl
ccbgssse053-ba_leather.esl
ccbgssse051-ba_daedricmail.esl
ccbgssse057-ba_stalhrim.esl
ccbgssse066-staves.esl
ccbgssse067-daedinv.esm
ccbgssse068-bloodfall.esl
ccbgssse069-contest.esl
ccvsvsse003-necroarts.esl
ccvsvsse004-beafarmer.esl
ccbgssse025-AdvDSGS.esm
ccffbsse002-crossbowpack.esl
ccbgssse013-dawnfang.esl
ccrmssse001-necrohouse.esl
ccedhsse003-redguard.esl
cceejsse004-hall.esl
cceejsse005-cave.esm
cckrtsse001_altar.esl
cccbhsse001-gaunt.esl
ccafdsse001-dwesanctuary.esm";

        const string LockedLoadOrder =
@"# This file was automatically generated by Mod Organizer.";

        #endregion

        #region Modlist

        const string ModListAE =
@"# This file was automatically generated by Mod Organizer.
*DLC: Dawnguard
*DLC: Dragonborn
*DLC: HearthFires
*Creation Club: ccbgssse001-fish
*Creation Club: ccbgssse025-advdsgs
*Creation Club: ccbgssse037-curios
*Creation Club: ccqdrsse001-survivalmode
*Creation Club: ccafdsse001-dwesanctuary
*Creation Club: ccasvsse001-almsivi
*Creation Club: ccbgssse002-exoticarrows
*Creation Club: ccbgssse003-zombies
*Creation Club: ccbgssse004-ruinsedge
*Creation Club: ccbgssse005-goldbrand
*Creation Club: ccbgssse006-stendarshammer
*Creation Club: ccbgssse007-chrysamere
*Creation Club: ccbgssse008-wraithguard
*Creation Club: ccbgssse010-petdwarvenarmoredmudcrab
*Creation Club: ccbgssse011-hrsarmrelvn
*Creation Club: ccbgssse012-hrsarmrstl
*Creation Club: ccbgssse013-dawnfang
*Creation Club: ccbgssse014-spellpack01
*Creation Club: ccbgssse016-umbra
*Creation Club: ccbgssse018-shadowrend
*Creation Club: ccbgssse019-staffofsheogorath
*Creation Club: ccbgssse020-graycowl
*Creation Club: ccbgssse021-lordsmail
*Creation Club: ccbgssse031-advcyrus
*Creation Club: ccbgssse034-mntuni
*Creation Club: ccbgssse035-petnhound
*Creation Club: ccbgssse036-petbwolf
*Creation Club: ccbgssse038-bowofshadows
*Creation Club: ccbgssse040-advobgobs
*Creation Club: ccbgssse041-netchleather
*Creation Club: ccbgssse043-crosselv
*Creation Club: ccbgssse045-hasedoki
*Creation Club: ccbgssse050-ba_daedric
*Creation Club: ccbgssse051-ba_daedricmail
*Creation Club: ccbgssse052-ba_iron
*Creation Club: ccbgssse053-ba_leather
*Creation Club: ccbgssse054-ba_orcish
*Creation Club: ccbgssse055-ba_orcishscaled
*Creation Club: ccbgssse056-ba_silver
*Creation Club: ccbgssse057-ba_stalhrim
*Creation Club: ccbgssse058-ba_steel
*Creation Club: ccbgssse059-ba_dragonplate
*Creation Club: ccbgssse060-ba_dragonscale
*Creation Club: ccbgssse061-ba_dwarven
*Creation Club: ccbgssse062-ba_dwarvenmail
*Creation Club: ccbgssse063-ba_ebony
*Creation Club: ccbgssse064-ba_elven
*Creation Club: ccbgssse066-staves
*Creation Club: ccbgssse067-daedinv
*Creation Club: ccbgssse068-bloodfall
*Creation Club: ccbgssse069-contest
*Creation Club: cccbhsse001-gaunt
*Creation Club: ccedhsse001-norjewel
*Creation Club: ccedhsse002-splkntset
*Creation Club: ccedhsse003-redguard
*Creation Club: cceejsse001-hstead
*Creation Club: cceejsse002-tower
*Creation Club: cceejsse003-hollow
*Creation Club: cceejsse004-hall
*Creation Club: cceejsse005-cave
*Creation Club: ccffbsse001-imperialdragon
*Creation Club: ccffbsse002-crossbowpack
*Creation Club: ccfsvsse001-backpacks
*Creation Club: cckrtsse001_altar
*Creation Club: ccmtysse001-knightsofthenine
*Creation Club: ccmtysse002-ve
*Creation Club: ccpewsse002-armsofchaos
*Creation Club: ccqdrsse002-firewood
*Creation Club: ccrmssse001-necrohouse
*Creation Club: cctwbsse001-puzzledungeon
*Creation Club: ccvsvsse001-winter
*Creation Club: ccvsvsse002-pets
*Creation Club: ccvsvsse003-necroarts
*Creation Club: ccvsvsse004-beafarmer
-0. MASTER FILES_separator";

        #endregion

        #region Plugins

        const string Plugins =
@"# This file was automatically generated by Mod Organizer.";

        #endregion

        #endregion

        public static string SoftwareId
        {
            get { return "Mod Organizer 2"; }
        }

        private static readonly Encoding IniEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        #region Methods        

        private void CreateModOrganizerIni(string InstallDir, string Profile, string GameDir, string DataDir)
        {
            var FileName = Path.Combine(InstallDir, "ModOrganizer.ini");

            File.Create(FileName).Dispose();

            string WineGameDir = ToWineIniPath(GameDir);
            string WineDataDir = ToWineIniPath(DataDir);

            NormalizeLineEndings(FileName, string.Format(IniFile, Profile, WineGameDir, WineDataDir));

            //File.WriteAllText(FileName, string.Format(IniFile, Profile, WineGameDir, WineDataDir));
        }

        // $XDG_CONFIG_HOME/Mod Organizer Team/Mod Organizer.conf - the QSettings file MO2 and
        // Fluorine share (organization "Mod Organizer Team", application "Mod Organizer").
        private static string ModOrganizerConfFile
        {
            get
            {
                var ConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

                if (string.IsNullOrWhiteSpace(ConfigHome))
                    ConfigHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

                return Path.Combine(ConfigHome, "Mod Organizer Team", "Mod Organizer.conf");
            }
        }

        /// <summary>
        /// Registers the instance with Fluorine.
        ///
        /// Fluorine locates its instance through the shared Qt settings file MO2 has always used -
        /// organization "Mod Organizer Team", application "Mod Organizer", which resolves to
        /// $XDG_CONFIG_HOME/Mod Organizer Team/Mod Organizer.conf - and not through anything stored
        /// inside the instance itself. Without it Fluorine opens its instance picker rather than the
        /// Nolvus instance, so this has to be written before the manager is ever launched.
        ///
        /// A conf that does not exist yet is created pointing at this instance. One that already
        /// exists only has this instance added to PortableInstances - CurrentInstance is left as it
        /// is, so an existing Fluorine setup is registered with the new instance without having the
        /// instance it currently has open switched out from under it. Any other settings Fluorine
        /// keeps in there are preserved either way.
        /// </summary>
        private static void CreateModOrganizerConf(string ModsDir)
        {
            var ConfFile = ModOrganizerConfFile;

            Directory.CreateDirectory(Path.GetDirectoryName(ConfFile));

            var Parser = ServiceSingleton.Settings.GetIniParser();

            // Qt writes key=value; the parser would pad it to key = value by default.
            Parser.Parser.Configuration.AssigmentSpacer = string.Empty;

            // The parser cannot read what is not there, and it throws outright on a conf left half
            // written by a crash. Neither is worth failing an install over, so fall back to a bare
            // [General] and let the keys below repopulate it. A conf we had to create ourselves does
            // not count as pre-existing, so it gets CurrentInstance too.
            var Existed = File.Exists(ConfFile);

            if (!Existed)
            {
                File.WriteAllText(ConfFile, "[General]" + "\n");
            }

            IniParser.Model.IniData Data;

            try
            {
                Data = Parser.ReadFile(ConfFile);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] {ConfFile} could not be parsed, rewriting it : {ex.Message}");

                File.WriteAllText(ConfFile, "[General]" + "\n");

                Data = Parser.ReadFile(ConfFile);
                Existed = false;
            }

            if (!Existed)
            {
                // Nothing was there to respect, so point Fluorine straight at this instance.
                Data["General"]["CurrentInstance"] = ModsDir;
                Data["General"]["PortableInstances"] = ModsDir;

                ServiceSingleton.Logger.Log($"[FLUORINE] Instance registered in {ConfFile} -> {ModsDir}");
            }
            else
            {
                var Existing = Data["General"]["PortableInstances"] ?? string.Empty;

                var Instances = Existing
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();

                if (Instances.Contains(ModsDir, StringComparer.Ordinal))
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] {ModsDir} already listed in {ConfFile}");
                    return;
                }

                Instances.Add(ModsDir);

                Data["General"]["PortableInstances"] = string.Join(", ", Instances);

                ServiceSingleton.Logger.Log($"[FLUORINE] Instance appended to PortableInstances in {ConfFile} -> {ModsDir}");
            }

            Parser.WriteFile(ConfFile, Data);
        }

        /// <summary>
        /// Points Fluorine at this instance. Fluorine opens whatever CurrentInstance names, so this
        /// has to run before launching or Play opens whichever instance was selected last.
        /// </summary>
        public static void SelectInstance(string InstallDir)
        {
            var ModsDir = Path.Combine(InstallDir, "MODS");
            var ConfFile = ModOrganizerConfFile;

            var Parser = ServiceSingleton.Settings.GetIniParser();

            Parser.Parser.Configuration.AssigmentSpacer = string.Empty;

            var Data = OpenConf(Parser, ConfFile);

            Data["General"]["CurrentInstance"] = ModsDir;

            var Instances = ReadInstances(Data);

            if (!Instances.Contains(ModsDir, StringComparer.Ordinal))
                Instances.Add(ModsDir);

            Data["General"]["PortableInstances"] = string.Join(", ", Instances);

            Parser.WriteFile(ConfFile, Data);

            ServiceSingleton.Logger.Log($"[FLUORINE] Selected instance {ModsDir}");
        }

        /// <summary>
        /// Drops a deleted instance from the shared conf, so Fluorine is not left pointing at a
        /// directory that no longer exists.
        /// </summary>
        public static void UnregisterInstance(string InstallDir)
        {
            var ConfFile = ModOrganizerConfFile;

            if (!File.Exists(ConfFile))
                return;

            var ModsDir = Path.Combine(InstallDir, "MODS");

            var Parser = ServiceSingleton.Settings.GetIniParser();

            Parser.Parser.Configuration.AssigmentSpacer = string.Empty;

            var Data = OpenConf(Parser, ConfFile);

            var Instances = ReadInstances(Data)
                .Where(x => !string.Equals(x, ModsDir, StringComparison.Ordinal))
                .ToList();

            Data["General"]["PortableInstances"] = string.Join(", ", Instances);

            // Fall back to another instance rather than leaving the selection on the deleted one.
            if (string.Equals(Data["General"]["CurrentInstance"], ModsDir, StringComparison.Ordinal))
                Data["General"]["CurrentInstance"] = Instances.FirstOrDefault() ?? string.Empty;

            Parser.WriteFile(ConfFile, Data);

            ServiceSingleton.Logger.Log($"[FLUORINE] Unregistered instance {ModsDir}");
        }

        private static List<string> ReadInstances(IniParser.Model.IniData Data)
        {
            return (Data["General"]["PortableInstances"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
        }

        // Opens the shared conf for editing, creating it or replacing an unparseable one. Note that
        // CreateModOrganizerConf deliberately does not use this - it needs to know whether the file
        // pre-existed in order to decide whether to touch CurrentInstance.
        private static IniParser.Model.IniData OpenConf(IniParser.FileIniDataParser Parser, string ConfFile)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfFile));

            if (!File.Exists(ConfFile))
                File.WriteAllText(ConfFile, "[General]" + "\n");

            try
            {
                return Parser.ReadFile(ConfFile);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] {ConfFile} could not be parsed, rewriting it : {ex.Message}");

                File.WriteAllText(ConfFile, "[General]" + "\n");

                return Parser.ReadFile(ConfFile);
            }
        }

        /// <summary>
        /// Removes the Nemesis symlink. Must run before anything enumerates the instance: it is a
        /// directory symlink, so DirectoryInfo.GetFiles(AllDirectories) never yields it as an entry
        /// yet still recurses through it, which turns a move into a copy of the Nemesis mod's
        /// contents into STOCK GAME/Data.
        /// </summary>
        public static void DeleteNemesisSymlink(string InstallDir)
        {
            var LinkName = Path.Combine(InstallDir, "STOCK GAME", "Data", "Nemesis_Engine");

            var Info = new FileInfo(LinkName);

            if (Info.LinkTarget == null)
                return;

            // File.Delete unlinks the symlink and leaves the mod folder it points at alone.
            File.Delete(LinkName);

            ServiceSingleton.Logger.Log($"Nemesis symlink removed from {LinkName}");
        }

        /// <summary>
        /// STOCK GAME/Data/Nemesis_Engine -> MODS/mods/Nemesis Unlimited Behavior Engine/Nemesis_Engine.
        /// Absolute, so it has to be remade whenever the instance moves.
        /// </summary>
        public static void CreateNemesisSymlink(string InstallDir, bool Replace = false)
        {
            var LinkName = Path.Combine(InstallDir, "STOCK GAME", "Data", "Nemesis_Engine");
            var LinkTarget = Path.Combine(InstallDir, "MODS", "mods", "Nemesis Unlimited Behavior Engine", "Nemesis_Engine");

            // Path.Exists is the only dependable check: File.Exists reports false for a working
            // directory symlink and true for a dangling one, while CreateSymbolicLink throws if
            // anything at all is already at the path.
            if (Path.Exists(LinkName))
            {
                if (!Replace)
                {
                    ServiceSingleton.Logger.Log($"Nemesis symlink already present at {LinkName}");
                    return;
                }

                // File.Delete unlinks the symlink itself and leaves the mod folder it points at
                // alone. Directory.Delete would follow it into the target, so it is not used here
                // even though the link points at a directory.
                File.Delete(LinkName);

                ServiceSingleton.Logger.Log($"Nemesis symlink removed from {LinkName}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LinkName));

            File.CreateSymbolicLink(LinkName, LinkTarget);

            ServiceSingleton.Logger.Log($"Nemesis symlink created {LinkName} -> {LinkTarget}");
        }

        /// <summary>
        /// Moves an instance's registration in the shared conf. Linux-only: Windows MO2 keeps this
        /// in the registry, so there is no upstream equivalent.
        /// </summary>
        public static void RemapConf(string OldInstallDir, string NewInstallDir)
        {
            RemapModOrganizerConf(
                Path.Combine(OldInstallDir, "MODS"),
                Path.Combine(NewInstallDir, "MODS"));
        }

        private static void RemapModOrganizerConf(string OldModsDir, string NewModsDir)
        {
            var ConfFile = ModOrganizerConfFile;

            if (!File.Exists(ConfFile))
            {
                // Nothing registered yet - register the new location instead.
                CreateModOrganizerConf(NewModsDir);
                return;
            }

            var Parser = ServiceSingleton.Settings.GetIniParser();

            Parser.Parser.Configuration.AssigmentSpacer = string.Empty;

            IniParser.Model.IniData Data;

            try
            {
                Data = Parser.ReadFile(ConfFile);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] {ConfFile} could not be parsed, rewriting it : {ex.Message}");

                File.Delete(ConfFile);
                CreateModOrganizerConf(NewModsDir);
                return;
            }

            // CurrentInstance is deliberately left alone; only PortableInstances is rewritten.
            // Play calls SelectInstance, which is what points Fluorine at the right instance.

            var Instances = (Data["General"]["PortableInstances"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Select(x => string.Equals(x, OldModsDir, StringComparison.Ordinal) ? NewModsDir : x)
                .ToList();

            if (!Instances.Contains(NewModsDir, StringComparer.Ordinal))
                Instances.Add(NewModsDir);

            Data["General"]["PortableInstances"] = string.Join(", ", Instances);

            Parser.WriteFile(ConfFile, Data);

            ServiceSingleton.Logger.Log($"[FLUORINE] Remapped {ConfFile} : {OldModsDir} -> {NewModsDir}");
        }

        public static string GetIni(bool Pref, IniLevel Level, INolvusInstance Instance)
        {
            string Result = string.Empty;
            
            #region Anniversary Edition

            if (!Pref)
            {
                switch (Level)
                {
                    case IniLevel.IniLow:
                        Result = string.Format(ModOrganizer.SkyrimIniLowAE, Instance.Settings.LgCode.ToLower(), Instance.Settings.LgName.ToUpper());
                        break;
                    case IniLevel.IniMedium:
                        Result = string.Format(ModOrganizer.SkyrimIniMediumAE, Instance.Settings.LgCode.ToLower(), Instance.Settings.LgName.ToUpper());
                        break;
                    case IniLevel.IniHigh:
                        Result = string.Format(ModOrganizer.SkyrimIniHighAE, Instance.Settings.LgCode.ToLower(), Instance.Settings.LgName.ToUpper());
                        break;
                }
            }
            else
            {
                switch (Level)
                {
                    case IniLevel.IniLow:
                        Result = string.Format(ModOrganizer.SkyrimPrefIniLowAE, Instance.Settings.Height, Instance.Settings.Width, 1);
                        break;
                    case IniLevel.IniMedium:
                        Result = string.Format(ModOrganizer.SkyrimPrefIniMediumAE, Instance.Settings.Height, Instance.Settings.Width, 1);
                        break;
                    case IniLevel.IniHigh:
                        Result = string.Format(ModOrganizer.SkyrimPrefIniHighAE, Instance.Settings.Height, Instance.Settings.Width, 1);
                        break;
                }
            }

            #endregion
                        
            return Result;
        }

        public static bool IsRunning
        {
            get { return Fluorine.IsRunning; }
        }

        public static Process Start(string InstallDir)
        {
            return Fluorine.Start();

            //var exe = Path.Combine(InstallDir, "MO2", "ModOrganizer.exe");
            //return ServiceSingleton.Wine.Run(exe, InstallDir, "");
        }

        public void AppendToIni(string IniDir, string Section, string Key, string Value)
        {        
            Value = CRLF(Value);    
            ServiceSingleton.Settings.StoreIniValue(Path.Combine(IniDir, "ModOrganizer.ini"), Section, Key, Value);
        }

        public void AddExecutable(string IniDir, string Args, string Binary, bool Hide, bool OwnIcon, string Title, bool Toolbar, string WorkingDirectory)
        {
            string IniFilePath = Path.Combine(IniDir, "ModOrganizer.ini");     

            string WineBinary = ModOrganizer.ToWineIniPath(Binary).Replace(@"\\", @"/"); //Fix for binary X:\\ -> X:/
            string WineWorkingDir = ModOrganizer.ToWineIniPath(WorkingDirectory);

            var SizeData = ServiceSingleton.Settings.GetIniValue(Path.Combine(IniDir, "ModOrganizer.ini"), "customExecutables", "size");

            int Size = 0;

            if (SizeData != null)
            {
                Size = System.Convert.ToInt16(SizeData);
            }

            Size++;

            AppendToIni(IniDir, "customExecutables", "size", Size.ToString());

            AppendToIni(IniDir, "customExecutables", Size + "\\arguments", Args);
            AppendToIni(IniDir, "customExecutables", Size + "\\binary", WineBinary);
            AppendToIni(IniDir, "customExecutables", Size + "\\hide", Hide.ToString().ToLower());
            AppendToIni(IniDir, "customExecutables", Size + "\\ownicon", OwnIcon.ToString().ToLower());
            AppendToIni(IniDir, "customExecutables", Size + "\\steamAppID", string.Empty);
            AppendToIni(IniDir, "customExecutables", Size + "\\title", Title);
            AppendToIni(IniDir, "customExecutables", Size + "\\toolbar", Toolbar.ToString().ToLower());
            AppendToIni(IniDir, "customExecutables", Size + "\\workingDirectory", WineWorkingDir);
        }

        //TODO testing required
        public static void ModifyExecutable(string ExecutableName, string IniDir, string Binary, string Args, string WorkingDirectory)
        {
            var Parser = ServiceSingleton.Settings.GetIniParser();

            var IniData = Parser.ReadFile(Path.Combine(IniDir, "ModOrganizer.ini"));

            var Section = IniData.Sections.Where(x => x.SectionName == "customExecutables").FirstOrDefault();

            var KeyIndex = Section.Keys.Where(x => x.KeyName.Contains("title") && x.Value.Contains(ExecutableName)).FirstOrDefault().KeyName.Substring(0, 1);

            IniData["customExecutables"][KeyIndex + "\\" + "binary"] = Binary;
            IniData["customExecutables"][KeyIndex + "\\" + "arguments"] = Args;
            IniData["customExecutables"][KeyIndex + "\\" + "workingDirectory"] = WorkingDirectory;

            Parser.WriteFile(Path.Combine(IniDir, "ModOrganizer.ini"), IniData);
        }

        public static bool CheckIfExecutableExists(string ExecutableName, string IniDir)
        {
            var Parser = ServiceSingleton.Settings.GetIniParser();

            var IniData = Parser.ReadFile(Path.Combine(IniDir, "ModOrganizer.ini"));

            var Section = IniData.Sections.Where(x => x.SectionName == "customExecutables").FirstOrDefault();

            return Section.Keys.Where(x => x.KeyName.Contains("title") && x.Value.Contains(ExecutableName)).FirstOrDefault() != null;
        }


        private void CreateBaseDirectories()
        {
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MO2"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "downloads"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "mods"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "mods", "0. MASTER FILES_separator"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "overwrite"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "profiles"));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name));
            Directory.CreateDirectory(Path.Combine(Instance.InstallDir, "MODS", "webcache"));
        }

        private void CreateProfileBaseFiles()
        {
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            var ProfileFolder = Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name);

            // Fluorine treats MODS as the portable instance directory, so ModOrganizer.ini belongs
            // there rather than next to the MO2 binaries.
            var ModsFolder = Path.Combine(Instance.InstallDir, "MODS");

            //File.WriteAllText(Path.Combine(ProfileFolder, "Skyrim.ini"), ModOrganizer.GetIni(false, (IniLevel)System.Convert.ToInt16(Instance.Performance.IniSettings), Instance));
            //File.WriteAllText(Path.Combine(ProfileFolder, "SkyrimPrefs.ini"), ModOrganizer.GetIni(true, (IniLevel)System.Convert.ToInt16(Instance.Performance.IniSettings), Instance));
            //File.WriteAllText(Path.Combine(ProfileFolder, "loadorder.txt"), LoadOrderAE);
            //File.WriteAllText(Path.Combine(ProfileFolder, "modlist.txt"), ModListAE);
            //File.WriteAllText(Path.Combine(ProfileFolder, "lockedorder.txt"), LockedLoadOrder);
            //File.WriteAllText(Path.Combine(ProfileFolder, "plugins.txt"), Plugins);
            //File.WriteAllText(Path.Combine(ProfileFolder, "settings.ini"), SettingsIni);
            //File.WriteAllText(Path.Combine(ProfileFolder, "skyrimcustom.ini"), string.Empty);
            //File.WriteAllText(Path.Combine(MO2Folder, "nxmhandler.ini"), nxmhandler);

            NormalizeLineEndings(Path.Combine(ProfileFolder, "Skyrim.ini"), ModOrganizer.GetIni(false, (IniLevel)System.Convert.ToInt16(Instance.Performance.IniSettings), Instance));
            NormalizeLineEndings(Path.Combine(ProfileFolder, "SkyrimPrefs.ini"), ModOrganizer.GetIni(true, (IniLevel)System.Convert.ToInt16(Instance.Performance.IniSettings), Instance));
            NormalizeLineEndings(Path.Combine(ProfileFolder, "loadorder.txt"), LoadOrderAE);
            NormalizeLineEndings(Path.Combine(ProfileFolder, "modlist.txt"), ModListAE);
            NormalizeLineEndings(Path.Combine(ProfileFolder, "lockedorder.txt"), LockedLoadOrder);
            NormalizeLineEndings(Path.Combine(ProfileFolder, "plugins.txt"), Plugins);
            NormalizeLineEndings(Path.Combine(ProfileFolder, "settings.ini"), SettingsIni);
            NormalizeLineEndings(Path.Combine(ProfileFolder, "skyrimcustom.ini"), string.Empty);
            // Dead under Fluorine: nothing reads nxmhandler.ini any more.
            //NormalizeLineEndings(Path.Combine(MO2Folder, "nxmhandler.ini"), nxmhandler);

            CreateModOrganizerIni(ModsFolder, Instance.Name, Instance.StockGame, ModsFolder);

            CreateModOrganizerConf(ModsFolder);
        }

        // Dead under Fluorine: NolvusLauncher.exe is not deployed and its call site is already commented out.
        //private void CreateLauncher()
        //{
            //Console.WriteLine("Skipping Creation of NolvusLauncher.exe");
            ////File.WriteAllBytes(Path.Combine(ServiceSingleton.Instances.WorkingInstance.InstallDir, "MO2", "NolvusLauncher.exe"), Properties.Resources.NolvusLauncher);
        //}

        private void AddExecutables()
        {
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            // Executables are recorded in the ini Fluorine loads, which lives in MODS.
            var MO2Folder = Path.Combine(Instance.InstallDir, "MODS");

            // Skip NolvusLauncher.exe entirely
            // Add SKSE, Skyrim, Launcher
            AddExecutable(MO2Folder, string.Empty,
                Path.Combine(Instance.StockGame, "skse64_loader.exe").Replace(@"\", @"/"),
                false, true, "SKSE", true, Instance.StockGame.Replace(@"\", @"/"));

            AddExecutable(MO2Folder, string.Empty,
                Path.Combine(Instance.StockGame, "SkyrimSE.exe").Replace(@"\", @"/"),
                true, true, "Skyrim Special Edition", false, Instance.StockGame.Replace(@"\", @"/"));

            AddExecutable(MO2Folder, string.Empty,
                Path.Combine(Instance.StockGame, "SkyrimSELauncher.exe").Replace(@"\", @"/"),
                true, true, "Skyrim Special Edition Launcher", false, Instance.StockGame.Replace(@"\", @"/"));

            // Explorer++ dropped under Fluorine: it ships in MO2/ and browsing the VFS through it
            // is not useful.
            //AddExecutable(MO2Folder, string.Empty,
            //    Path.Combine(Instance.InstallDir, "MO2", "explorer++", "Explorer++.exe").Replace(@"\", @"/"),
            //    true, true, "Explore Virtual Folder", false,
            //    Path.Combine(Instance.InstallDir, "MO2", "explorer++").Replace(@"\", @"/"));

            // Nemesis
            AddExecutable(MO2Folder, string.Empty,
                Path.Combine(Instance.InstallDir, "MODS", "mods", "Nemesis Unlimited Behavior Engine",
                            "Nemesis_Engine", "Nemesis Unlimited Behavior Engine.exe").Replace(@"\", @"/"),
                false, true, "Nemesis Unlimited Behavior Engine", true,
                Path.Combine(Instance.InstallDir, "MODS", "mods",
                            "Nemesis Unlimited Behavior Engine", "Nemesis_Engine").Replace(@"\", @"/"));
            
            // Nemesis Symlink
            CreateNemesisSymlink(Instance.InstallDir);

            // xEdit
            // The VFS already presents the right Data, ini and plugin list, so the explicit
            // -D:/-I:/-P: arguments are omitted - real paths would point xEdit around the VFS.
            //string dataPath = ToWinePath(Path.Combine(Instance.InstallDir, "STOCK GAME", "Data"));
            //string iniPath = ToWinePath(Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name, "Skyrim.ini"));
            //string pluginPath = ToWinePath(Path.Combine(Instance.InstallDir, "MODS", "profiles", Instance.Name, "plugins.txt"));
            //string Args = "-D:" + MO2String(dataPath) + " " + "-I:" + MO2String(iniPath) + " " + "-P:" + MO2String(pluginPath);
            AddExecutable(MO2Folder, string.Empty,
                Path.Combine(Instance.InstallDir, "TOOLS", "SSE Edit", "SSEEdit.exe").Replace(@"\", @"/"),
                false, true, "xEdit", true,
                Path.Combine(Instance.InstallDir, "TOOLS", "SSE Edit").Replace(@"\", @"/"));

            // xEdit AutoClean
            string ArgsAutoClean = string.Empty;
            AddExecutable(MO2Folder, ArgsAutoClean,
                Path.Combine(Instance.InstallDir, "TOOLS", "SSE Edit", "SSEEditQuickAutoClean.exe").Replace(@"\", @"/"),
                false, true, "xEdit Cleaning", false,
                Path.Combine(Instance.InstallDir, "TOOLS", "SSE Edit").Replace(@"\", @"/"));

            // Bodyslide
            AddExecutable(MO2Folder, string.Empty,
                Path.Combine(Instance.InstallDir, "MODS", "mods", "BodySlide and Outfit Studio",
                            "CalienteTools", "BodySlide", "BodySlide x64.exe").Replace(@"\", @"/"),
                false, true, "Body Slide", true,
                Path.Combine(Instance.InstallDir, "MODS", "mods",
                            "BodySlide and Outfit Studio", "CalienteTools", "BodySlide").Replace(@"\", @"/"));
        }


        private void AddSplash()
        {
            string SplashPath = Path.Combine(ServiceSingleton.Instances.WorkingInstance.InstallDir, "MO2", "splash.png");

            switch (ServiceSingleton.Instances.WorkingInstance.Name)
            {
                case Strings.NolvusAscension:
                    Properties.Resources.splash_v5.Save(SplashPath, new PngEncoder());
                    break;
                case Strings.NolvusAwakening:
                    Properties.Resources.splash_v6.Save(SplashPath, new PngEncoder());
                    break;
            }            
        }

        protected override async Task DoCopy()
        {
            // Fluorine is a prerequisite of the mod list rather than part of it, so it is fetched
            // before the instance is scaffolded.
            await Fluorine.Install(DownloadingProgress, ExtractingProgress);

            var Tsk = Task.Run(() =>
            {
                try
                {
                    var Instance = ServiceSingleton.Instances.WorkingInstance;

                    CreateBaseDirectories();
                    CreateProfileBaseFiles();
                    //CreateLauncher();
                    AddExecutables();

                    var extractDir = Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir);
                    var mo2Dir = Path.Combine(Instance.InstallDir, "MO2");

                    var directoryCopyRule = new DirectoryCopy
                    {
                        Source = "",                // Empty => copy entire extract folder
                        Destination = 2,            // 2 => InstanceDir (but we will override below)
                        DestinationDirectory = "",  // copy to root of MO2
                    };

                    directoryCopyRule.Execute(
                        Instance.StockGame,    // GamePath
                        extractDir,            // ExtractDir
                        mo2Dir,                // ModDir (ignored for MO2)
                        mo2Dir                 // InstanceDir = Destination for MO2
                    );

                    CopyingProgress(1, 1);

                    AddSplash();
                }
                finally
                {
                    ServiceSingleton.Files.RemoveDirectory(
                        Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir),
                        true
                    );
                }
            });

            await Tsk;
        }

        private string GetModListFile()
        {
            return Path.Combine(ServiceSingleton.Instances.WorkingInstance.InstallDir, "MODS", "profiles", ServiceSingleton.Instances.WorkingInstance.Name, "modlist.txt");
        }

        private async Task<List<ModObject>> DoGetModsMetaData(string Profile, Action<string, int> Progress = null)
        {
            return await Task.Run(() =>
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;                
                var Mods = File.ReadAllLines(Path.Combine(Instance.InstallDir, "MODS", "profiles", Profile, "modlist.txt")).ToList();
                                
                Mods.RemoveAt(0);
                Mods.Reverse();

                var Category = string.Empty;
                var Counter = 0;
                var Result = new List<ModObject>();

                foreach (var Line in Mods)
                {
                    var Selected = Line.Substring(0, 1);
                    var Mod = Line.Substring(1);

                    if (Mod.Contains("_separator"))
                    {
                        Category = Mod.Replace("_separator", string.Empty);
                    }
                    else
                    {
                        var ModObject = new ModObject
                        {
                            Selected = Selected == "+" || Selected == "*",
                            Priority = Result.Count,
                            Name = Mod,
                            Category = Category,
                            Version = "NA",
                            StatusText = "OK"
                        };

                        var MetaIniFile = Path.Combine(Instance.InstallDir, "MODS", "mods", Mod, "meta.ini");

                        if (File.Exists(MetaIniFile))
                        {
                            try
                            {
                                ModObject.Version = ServiceSingleton.Settings.GetIniValue(MetaIniFile, "General", "version");

                                if (ModObject.Version == null)
                                {
                                    ModObject.Version = "NA";
                                    ModObject.Status = ModObjectStatus.MetaIniError;
                                    ModObject.StatusText = string.Format("Unable to parse meta.ini data for {0}", Mod);
                                }
                            }
                            catch (Exception ex)
                            {
                                ModObject.Version = "NA";
                                ModObject.Status = ModObjectStatus.MetaIniError;
                                ModObject.StatusText = string.Format("Unable to parse meta.ini data for {0} - {1}", Mod, ex.Message);
                            }
                        }
                        else
                        {
                            if (Directory.Exists(Path.Combine(Instance.InstallDir, "MODS", "mods", Mod)))
                            {
                                ModObject.Version = "NA";
                                ModObject.Status = ModObjectStatus.InstalledIniMissing;
                                ModObject.StatusText = "Installed but meta.ini file is missing";
                            }                           
                        }                       

                        Result.Add(ModObject);
                    }

                    Progress?.Invoke("Loading Mod Organizer data file", System.Convert.ToInt16(Math.Round(((double)++Counter / Mods.Count * 100))));
                }

                return Result;
            });
        }

        public List<string> GetProfiles()
        {
            return new DirectoryInfo(Path.Combine(ServiceSingleton.Instances.WorkingInstance.InstallDir, "MODS", "profiles")).GetDirectories().Where(x => x.GetFiles().Any(y => y.Name == "modlist.txt")).Select(d => {
                return d.Name;
            }).ToList();                                        
        }

        public async Task<List<string>> GetProfilesAsync()
        {
            return await Task.Run(() =>
            {
                return new DirectoryInfo(Path.Combine(ServiceSingleton.Instances.WorkingInstance.InstallDir, "MODS", "profiles")).GetDirectories().Where(x => x.GetFiles().Any(y => y.Name == "modlist.txt")).Select(d =>
                {
                    return d.Name;
                }).ToList();
            });
        }

        public async Task<List<ModObject>> GetModsMetaData(Action<string, int> Progress = null)
        {
            return await DoGetModsMetaData(ServiceSingleton.Instances.WorkingInstance.Name, Progress);
        }

        public async Task<List<ModObject>> GetModsMetaData(string Profile, Action<string, int> Progress = null)
        {
            return await DoGetModsMetaData(Profile, Progress);
        }

        public static string ToWinePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            path = path.Replace("\\", "/").Trim();

            while (path.Contains("//", StringComparison.Ordinal))
                path = path.Replace("//", "/", StringComparison.Ordinal);

            path = path.TrimEnd('/');

            // Fluorine addresses the instance through wine's default Z: mapping (Z: == /), so paths
            // go in whole rather than being rebased onto a drive letter. This also drops the
            // dependency on the X: symlink, which was created by the Proton prefix setup and no
            // longer exists.
            if (path.StartsWith("/", StringComparison.Ordinal))
                return "Z:" + path.Replace("/", "\\");

            return path.Replace("/", "\\");

            // Previous MO2 behaviour: rebase onto X:, which was symlinked to the Nolvus root, to
            // keep paths short. Restore this if MAX_PATH becomes a problem again under Fluorine.
            //
            // int idx = path.IndexOf("/Instances", StringComparison.OrdinalIgnoreCase);
            // if (idx == -1)
            // {
            //     if (path.StartsWith("/", StringComparison.Ordinal))
            //         return "X:" + path.Replace("/", "\\");
            //     return path.Replace("/", "\\");
            // }
            //
            // string trimmed = path.Substring(idx);     // "/Instances/...."
            // return "X:" + trimmed.Replace("/", "\\"); // "X:\\Instances\\...."
        }

        public static string ToWineIniPath(string path)
        {
            return ToWinePath(path).Replace("\\", "\\\\");
        }

        private static void NormalizeLineEndings(string path, string content)
        {
            content = CRLF(content);
            
            using (var sw = new StreamWriter(path, false, IniEncoding))
            {
                sw.NewLine = "\r\n";
                sw.Write(content);
            }
        }

        private static string CRLF(string text)
        {
            if (string.IsNullOrEmpty(text)) 
                return text;

            return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        // Dead under Fluorine: only used to quote the xEdit arguments, which are no longer written.
        //private static string MO2String(string path)
        //{
            //path = path.TrimEnd('\\');
            //var escaped = path.Replace("\\", "\\\\");
            //return "\\\"" + escaped + "\\\"";
        //}

        #endregion                       
    }
}

