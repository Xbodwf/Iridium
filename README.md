![Iridium](https://socialify.git.ci/Xbodwf/Iridium/image?custom_description=An+optimized+mod+for+ADOFAI&custom_language=csharp&description=1&font=Lexend&forks=1&issues=1&language=1&name=1&pulls=1&stargazers=1&theme=Auto)

# Iridium

An optimized mod for A Dance of Fire and Ice, focusing on performance, visual customization, and compatibility.

[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](LICENSE)

[中文](README_zh-CN.md)


Welcome to join our discord server!

https://discord.gg/ddndY4xXeK

---

> [!IMPORTANT]
> Iridium is designed to enhance your "A Dance of Fire and Ice" experience through better memory management, extreme performance optimization, and modern visual adjustments.

## Supported Versions

- **main branch**: ADOFAI v2.9.8
- **frontline branch**: ADOFAI v2.10.0+

---

## Features

### Performance Optimization
Improves overall smoothness and reduces lag by optimizing rendering efficiency, enhancing effect performance, and speeding up scene loading.

### Memory Optimization
Automatically releases unused memory during scene transitions, preventing excessive memory usage during long play sessions.

### UI Customization
Offers various interface adjustments including removing the news panel, hiding the beta watermark, repositioning the autoplay text, and displaying the countdown in the editor.

### Lobby Music
Switch between different background music tracks based on speed (BPM), with support for custom music file paths.

### Judge Text Customization
Freely customize judgment text content (e.g. "Perfect", "Too Early"), with rich text tag support and an optional offset display mode.

### Hit Sound
Hit sound pitch follows the music pitch automatically.

### Editor Enhancements
Improves editor workflow in multiple ways: performance optimizations for floor insert/delete operations on large levels (10k+ floors), customizable keyboard shortcuts for decorations and floors, and pause/resume support during auto-play preview.

### Compatibility & Bug Fixes
Provides behavior options for legacy levels (such as legacy Flash and Camera Relative modes), along with fixes for known game issues including portal softlocks, hairpin turn beat detection, and editor replay mistake tracking.

### Patch Mode
Choose between IL Transpiler (performance-oriented) and Prefix/Postfix (compatibility-oriented) patch modes.

---

## Installation

1. Go to [Releases](https://github.com/Xbodwf/Iridium/releases) to download the latest stable release. For new features, download the latest beta or prerelease.

> [!NOTE]
> Each release provides multiple builds for different ADOFAI game versions — please choose the one that matches your game version.

2. Ensure that [UnityModManager](https://www.nexusmods.com/site/mods/21) is installed.

> [!WARNING]
> We do not enforce a specific UnityModManager version. However, for ADOFAI versions 2.10.0 and above, using a UnityModManager version lower than **0.32.5.0** can easily cause crashes. Therefore, in Iridium builds targeting ADOFAI **2.10.0+**, we **require UnityModManager 0.32.5.0** to ensure stability.

3. Extract the downloaded files to: `Game Directory/Mods/Iridium` (same level as `A Dance of Fire And Ice_Data`). Create the folder if it does not exist.

4. Launch the game. If the game is already running, restart it.

> [!CAUTION]
> Unless it is a specially tuned version of the mod released for older game versions, do not attempt to run Iridium on ADOFAI **2.9.7 or below**. We do not guarantee functional stability or compatibility in such cases.

---

## Build from Source

1. Ensure the .NET SDK is installed.
2. Clone this repository (including submodules):
   ```bash
   git clone --recursive https://github.com/Xbodwf/Iridium.git
   cd Iridium
   ```
   If you already cloned without `--recursive`, run:
   ```bash
   git submodule update --init --recursive
   ```
3. Set your game directory path in `Iridium.csproj`.
4. Build and deploy:
   ```bash
   dotnet build
   ```

---

## Special Thanks

Thanks to all contributors:

<a href="https://github.com/Xbodwf/Iridium/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Xbodwf/Iridium&max=200&columns=14" />
</a>

> For other contributors, see [contributors.md](contributors.md)
