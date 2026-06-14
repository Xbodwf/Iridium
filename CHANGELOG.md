# Iridium Changelog

## r31_beta3

### MelonLoader 面板改进 / MelonLoader Panel Improvements / MelonLoader 패널 개선

- **Iridium UI 风格窗口**：MelonHandler 窗口从 Unity 默认 `GUILayout.Window` 迁移至 `IridiumLayout` 渲染，与主界面风格统一（圆角深色背景、标题样式）
- **关闭按钮**：标题栏右侧添加 × 关闭按钮，点击关闭面板
- **拖拽修复**：拖拽区域限制在标题栏，不再拦截内容区鼠标事件，Tab 切换、Switch 开关、滚动条拖动均恢复正常
- **首次打开卡顿优化**：UI 纹理资源预加载至 mod 初始化阶段，消除首次打开面板时的渲染阻塞

- **Iridium-styled window**: MelonHandler panel migrated from default `GUILayout.Window` to `IridiumLayout` rendering for visual consistency (rounded dark background, title styling)
- **Close button**: Added × close button on the title bar right side
- **Drag fix**: Drag area restricted to title bar; no longer intercepts content-area mouse events — Tab switching, Switch toggles, and scrollbar dragging all work correctly
- **First-open lag fix**: Pre-load UI texture resources during mod initialization to eliminate render blocking on first panel open

- **Iridium 스타일 창**: MelonHandler 패널이 기본 `GUILayout.Window`에서 `IridiumLayout` 렌더링으로 마이그레이션되어 메인 인터페이스와 시각적 일관성 확보 (둥근 어두운 배경, 제목 스타일)
- **닫기 버튼**: 타이틀바 우측에 × 닫기 버튼 추가
- **드래그 수정**: 드래그 영역을 타이틀바로 제한하여 콘텐츠 영역의 마우스 이벤트 차단 해제 — Tab 전환, Switch 토글, 스크롤바 드래그 정상 작동
- **첫 열기 지연 최적화**: UI 텍스처 리소스를 모드 초기화 단계에서 미리 로드하여 첫 패널 열기 시 렌더링 블로킹 제거

### CoopPauseLockFix 版本兼容性修复 / CoopPauseLockFix Version Compatibility Fix / CoopPauseLockFix 버전 호환성 수정

- **运行时目标探测**：移除硬编码 `[HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.LockInput))]`；改为运行时反射探测 `scrPlayer.LockInput` (≥3.1.2) 或 `scrController.LockInput` (≤3.1.1)，自动选择正确目标
- **PatchManager 适配**：`ApplyPatch`/`RemovePatch` 对 `CoopPauseLockFix` 走手动 patch/卸载分支
- **Runtime target detection**: Removed hardcoded `[HarmonyPatch(typeof(scrPlayer))]`; now uses runtime reflection to detect `scrPlayer.LockInput` (≥3.1.2) or `scrController.LockInput` (≤3.1.1) and auto-selects correct target
- **PatchManager adaptation**: `ApplyPatch`/`RemovePatch` use dedicated manual patch/unpatch branches for `CoopPauseLockFix`
- **런타임 대상 감지**: 하드코딩된 `[HarmonyPatch(typeof(scrPlayer))]` 제거; 런타임 리플렉션으로 `scrPlayer.LockInput` (≥3.1.2) 또는 `scrController.LockInput` (≤3.1.1) 감지 및 자동 선택
- **PatchManager 적응**: `ApplyPatch`/`RemovePatch`가 `CoopPauseLockFix`에 대해 수동 패치/언패치 분기 사용
