> [!NOTE]
> Iridium supports multiple mod loaders — see README.md for platform-specific installation instructions.
>
> [!WARNING]
> On Linux/macOS, this mod may require libgdiplus to be installed separately. It is used for UI rendering (icons, buttons, switches) as well as image compression.

### Changes

1. Fixed the editor keyboard shortcuts not working. The default combinations pointed to keys that could never be detected, so pressing them did nothing.
2. Fixed the shortcut settings showing the wrong modifier keys: the panel could show "Ctrl+Alt+A" while the mod was actually listening for "Ctrl+Shift+A". The panel now shows exactly the combination that triggers each action, and newly bound shortcuts save precisely what you press.
3. Shortcut settings saved by older versions are repaired automatically when the game starts — no need to reset or rebind anything manually.
