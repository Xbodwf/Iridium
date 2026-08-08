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

- **v2 branch**: ADOFAI v2
- **v3 branch**: ADOFAI v3

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

Select your modloader below for installation instructions:

- [UnityModManager](docs/loader/umm.md)
- [MelonLoader](docs/loader/melonloader.md)
- [BepInEx](docs/loader/bepinex.md)

> [!CAUTION]
> Unless it is a specially tuned version of the mod released for older game versions, do not attempt to run Iridium on ADOFAI **2.9.7 or below**. We do not guarantee functional stability or compatibility in such cases.

---

## Build from Source

1. Ensure the .NET SDK is installed.
2. Clone this repository with submodules:
   ```bash
   git clone --recursive https://github.com/Xbodwf/Iridium.git
   cd Iridium
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
