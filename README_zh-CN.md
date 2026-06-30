![Iridium](https://socialify.git.ci/Xbodwf/Iridium/image?custom_description=An+optimized+mod+for+ADOFAI&custom_language=csharp&description=1&font=Lexend&forks=1&issues=1&language=1&name=1&pulls=1&stargazers=1&theme=Auto)

# Iridium

一个专注于性能优化、视觉调整和兼容性的 A Dance of Fire and Ice 优化 Mod

[![许可证: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](LICENSE)

[English](README.md)

欢迎加入我们的Discord服务器!

https://discord.gg/ddndY4xXeK

---

> [!IMPORTANT]
> Iridium 旨在通过更好的内存管理、极致的性能优化和现代化的视觉调整来提升您的《冰与火之舞》体验。

## 版本支持

- **main 分支**：适用于 ADOFAI v2.9.8
- **frontline 分支**：适用于 ADOFAI v2.10.0+

---

## 功能

### 性能优化

让游戏运行更加流畅，减少卡顿和掉帧。主要体现在画面的渲染效率提升、特效性能改善以及场景加载速度加快。

### 内存优化

自动在场景切换时释放不需要的内存，减少长时间游戏后出现的内存占用过高问题。

### 界面自定义

提供多种界面调整选项，包括移除首页新闻、隐藏测试版水印、调整自动播放文字的位置、在编辑器中也显示倒计时等。

### 大厅音乐替换

支持在不同转速下切换不同的背景音乐，也可以使用自定义音乐文件。

### 判定文字自定义

可以自由修改游戏中的判定文字内容（如"完美"、"过早"等），支持富文本标签，也可以切换为显示输入偏移毫秒数。

### 打击音

打击音的音高会跟随音乐音高同步变化。

### 编辑器优化

从多个方面改善编辑器的使用体验：对大型谱面（上万砖块）的插入和删除操作进行性能优化；支持自定义快捷键用于快速操作装饰物和砖块；可在自动播放预览中使用暂停/继续功能。

### 兼容性与问题修复

针对旧版谱面提供行为兼容选项（如旧版闪烁和摄像机相对模式），同时修复了游戏本身存在的一些问题，包括传送卡死、发卡弯节拍检测、编辑器播放失误重置等。

### 补丁模式

提供 IL Transpiler 和 Prefix/Postfix 两种补丁模式，用户可根据自身需求在性能与兼容性之间选择。

---

## 安装方法

1. 前往 [Releases](https://github.com/Xbodwf/Iridium/releases) 下载最新稳定版本。如需体验新功能，可下载最新的 beta 或 prerelease 版本。

> [!NOTE]
> 每个 Release 提供适用于不同 ADOFAI 游戏版本的多个构建包，请根据您的游戏版本选择对应的版本下载。

2. 确保已安装 [UnityModManager](https://www.nexusmods.com/site/mods/21)。

> [!WARNING]
> 我们不强制要求特定的 UnityModManager 版本。但对于 2.10.0 及以上版本的 ADOFAI，使用低于 **0.32.5.0** 的 UnityModManager 版本极易导致崩溃。因此，在适用于 **ADOFAI 2.10.0+** 的 Iridium 构建中，我们将**要求 UnityModManager 0.32.5.0**，以确保稳定并提醒用户注意此要求。

3. 将下载的文件解压到游戏目录下的 `Mods/Iridium` 文件夹（与 `A Dance of Fire And Ice_Data` 同级）。如文件夹不存在请手动创建。

4. 启动游戏。若游戏已在运行，请重启游戏。

> [!CAUTION]
> 除非是维护者推出的针对旧版游戏的特调版本，否则请勿在 ADOFAI **2.9.7 及以下**版本运行 Iridium。我们不保障此情况下的功能稳定性和兼容性。

---

## 自行构建

1. 确保已安装 .NET SDK。
2. 克隆本仓库（包含子模块）：
   ```bash
   git clone --recursive https://github.com/Xbodwf/Iridium.git
   cd Iridium
   ```
   如果你已经克隆但没有使用 `--recursive`，请执行：
   ```bash
   git submodule update --init --recursive
   ```
3. 在 `Iridium.csproj` 中设置游戏目录路径。
4. 使用 dotnet 构建并部署：
   ```bash
   dotnet build
   ```

---

## 致谢

感谢所有贡献者的支持：

<a href="https://github.com/Xbodwf/Iridium/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Xbodwf/Iridium&max=200&columns=14" />
</a>

> 完整贡献者名单见 [contributors.md](contributors.md)
