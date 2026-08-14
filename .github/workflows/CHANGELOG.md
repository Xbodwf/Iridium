> [!NOTE]
> Iridium supports multiple mod loaders — see README.md for platform-specific installation instructions.
>
> [!WARNING]
> On Linux/macOS, this mod may require libgdiplus to be installed separately. It is used for UI rendering (icons, buttons, switches) as well as image compression.

### Changes

1. Fixed the Iridium for ADOFAI v3 settings panel not opening under the MelonLoader and BepInEx loaders.
2. Added two new memory optimization options (disabled by default): "Optimize Player Input Allocations" and "Optimize Input Key List Allocations". These reduce how much extra memory the game uses while playing, which can help on weaker machines and lower the chance of stutters.
3. The Optimizer settings tab now groups options into collapsible sections so the panel is easier to read.
4. FerriteCore (https://github.com/adofaiex/FerriteCore-ADOFAI) has been merged into Iridium after being improved, as a standalone memory optimization engine. It is controlled by its own config file (Config/FerriteCore.json) and can be toggled from the Memory Optimization section.
5. Fixed the style issue with the first-launch window and fixed a bug that could cause a black bar to cover the screen.
