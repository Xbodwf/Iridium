![Iridium](https://socialify.git.ci/Xbodwf/Iridium/image?custom_description=An+optimized+mod+for+ADOFAI&custom_language=csharp&description=1&font=Lexend&forks=1&issues=1&language=1&name=1&pulls=1&stargazers=1&theme=Auto)


# Iridium

一个专注于性能优化、视觉调整和兼容性的A Dance of Fire and Ice优化Mod

[![许可证: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](LICENSE)

[English](README.md)

---

## 简体中文

> [!IMPORTANT]
> Iridium 旨在通过更好的内存管理、极致的性能优化和现代化的视觉调整来提升您的《冰与火之舞》体验。

### 通用部署
1.首先去Releases下载最新稳定发行版。如果您想体验新功能，可以下载最新的beta或者prerelease.

> [!NOTE]
> 每个Release会提供适用于不同ADOFAI游戏版本的多个版本 — 请根据您的游戏版本选择对应的版本下载。

2.确保您已安装UnityModManager

> [!WARNING]
> 我们不强制要求特定的 UnityModManager 版本。但对于 2.10.0 及以上版本的 ADOFAI，使用低于 **0.32.5.0** 的 UnityModManager 版本极易导致崩溃。
> 因此，在适用于 **ADOFAI 2.10.0+** 的 Iridium 构建中，我们将**要求 UnityModManager 0.32.5.0**，以确保稳定并提醒用户注意此要求。


3.解压下载好的文件 到 游戏目录(与`A Dance of Fire And Ice_Data`同级)/Mods/Iridium (不存在就创建)

4.启动游戏。若游戏已运行，重启游戏。

> [!Caution]
> 除非是经过维护者推出的适用于旧版游戏的特调版Mod，否则不要轻易在旧版ADOFAI(<=2.9.7)运行Iridium.若出现此行为我们不保障功能稳定性和兼容性。

### 构建部署。

1.首先确保您已安装.NET SDK.

2.克隆本项目
```bash
git clone https://github.com/Xbodwf/Iridium.git Iridium
cd Iridium
```
3.在[Iridium.csproj](Iridium.csproj)中设定游戏目录

4.使用dotnet构建并部署到游戏
```bash
dotnet build
```

## Special Thanks
感谢他们的贡献:

<a href="https://github.com/Xbodwf/Iridium/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Xbodwf/Iridium&max=200&columns=14" />
</a>

> 若要查看其他贡献者，详见 [贡献者名单](contributors.md).
