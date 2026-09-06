> [!NOTE]
> Iridium supports multiple mod loaders — see README.md for platform-specific installation instructions.

> [!WARNING]
> On Linux/macOS, this mod may require libgdiplus to be installed separately. It is used for UI rendering (icons, buttons, switches) as well as image compression.

### Performance

1. **Decoration hot-path overhaul.** Charts driven by thousands of `MoveDecorations` events (e.g. 90,000+ events in a single chart) no longer pay a per-event LINQ penalty when resolving tagged decorations, and idle decorations no longer rewrite their rotation/scale transforms every frame. This is the biggest script-side win on decoration-heavy charts.
2. **New: Decoration Shader Cache** (Rendering section). The game rewrites every visible decoration's material (color, opacity, tiling, scale) every frame even when nothing changed. Decorations without filters/masks now skip that work entirely while static. Combine it with "Optimize FFx Decorations" — they cover different halves of the same problem.
3. **"Optimize FFx Decorations" rewritten.** The old version dropped material/filter updates and hitbox state for many decorations — it was fast but visibly wrong (missing fades, broken hitboxes). It now behaves identically to vanilla while still skipping decorations that are persistently invisible, and it no longer rewrites transforms for decorations that are not moving.
4. **Custom easing engine rewritten from scratch.** Tweens are now tracked by (target, property) and can actually be killed when events overlap (previously they fought each other and were never cleaned up). The full ease table is implemented — Flash, Elastic and Bounce easings previously fell back to OutQuad and animated incorrectly. Scrubbing the timeline, restarting and exiting play now reset tween state exactly like vanilla.

### Fixes

5. **White tracks / stuck "half-play" state after exiting play mode.** An exception in the reset path aborted cleanup halfway, leaving tracks white, disappearing animations broken, and the editor in a half-play half-edit state. Fixed.
6. **Tracks no longer refuse to disappear** when the custom easing engine is enabled: the game's Floor Appear/Disappear animations now properly take over from engine tweens instead of fighting them.
7. **Patch mode "IL Transpiler" no longer silently disables texture scaling compensation.** Patches without a transpiler implementation now fall back to prefix/postfix instead of failing — all 121 patches apply in both modes.
8. **Removed placebo features.** The old "memory optimization" switch did nothing; the multithread optimizer, several caches and object pools were never called. Dead switches are gone — everything remaining is real.

### Memory

9. **Memory section reworked.** The scattered switches are now one **Basic Memory Optimization** toggle (GC tuning, scene-switch cleanup, engine tuning). Under **Advanced**, a new **Virtual Memory Optimization** moves inactive memory to the system page file (same principle as PCL2's memory optimization) — Windows only, with a mechanical-drive warning. It can run when loading a level and/or when entering the editor.

### UI

10. **Clickable hyperlinks** are now a proper component — the General tab shows a direct link to the tutorial (learn.modrift.org/mods/iridium).
11. Settings hints for memory features were rewritten to be actually readable, and the memory section now has a clear Basic / Advanced structure.
