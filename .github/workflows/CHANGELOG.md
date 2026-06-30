## r41_nightly2

> [!NOTE]
> nightly 版本仅包含部分最新变更，可能存在不稳定的情况。

### UI 系统重写 / UI System Rewrite / UI 시스템 재작성

Iridium 的 UI 系统已全面重写，引入了全新的 **IML (Iridium Markup Language)** 声明式 UI 框架，配合 **Iris** 渲染器实现高效的图形界面绘制。

- **声明式 IML 框架**：现在 UI 布局通过 `.iml` 文件声明，代码量大幅减少，结构清晰，修改 UI 无需重新编译
- **Declarative IML framework**: UI layouts are now defined via `.iml` files — drastically reducing code volume, making structure clearer, and allowing UI changes without recompilation
- **선언적 IML 프레임워크**: `.iml` 파일로 UI 레이아웃을 선언하여 코드량이 대폭 줄고 구조가 명확해지며, 재컴파일 없이 UI를 수정할 수 있습니다

- **Iris 渲染后端**：基于 Unity UIElements/IMGUI 的高效渲染器，支持圆角、边框、阴影等现代 UI 效果
- **Iris render backend**: A high-performance renderer built on Unity UIElements/IMGUI, supporting rounded corners, borders, shadows, and other modern UI effects
- **Iris 렌더링 백엔드**: Unity UIElements/IMGUI 기반의 고성능 렌더러로, 둥근 모서리, 테두리, 그림자 등 현대적인 UI 효과를 지원합니다

- **动态 IML 绑定**：通过 `ImlWindow` 支持运行时动态生成和更新 IML 内容，数据绑定与配置页面无需硬编码
- **Dynamic IML binding**: `ImlWindow` enables runtime generation and live updates of IML content — data binding and config pages without hardcoding
- **동적 IML 바인딩**: `ImlWindow`를 통해 런타임에 IML 콘텐츠를 동적으로 생성하고 업데이트할 수 있으며, 데이터 바인딩과 설정 페이지를 하드코딩 없이 구현합니다

**已迁移的页面 / Migrated pages / 마이그레이션 완료된 페이지:**
- 设置页面 (Settings) — 全新分页式设计，所有选项分类排列
- VRAM 通知 (VRAMNotification) — 轻量嵌入式提示
- 首次运行/升级引导 (FirstRun/Upgrade) — 内联 IML 动态渲染

---

### 其他变更 / Other Changes / 기타 변경사항

- **修复 ImlWindow 数据上下文绑定**：调用方传入 `Dictionary<string, object>` 时能正确绑定数据上下文
- **Fixed ImlWindow data context binding**: data context now correctly resolves when callers pass a `Dictionary<string, object>`
- **ImlWindow 데이터 컨텍스트 바인딩 수정**: 호출자가 `Dictionary<string, object>`를 전달할 때 데이터 컨텍스트가 올바르게 바인딩되도록 수정

- **编辑器优化 (main/frontline)**：优化大型谱面插入/删除操作的性能，支持 10k+ 砖块的无卡顿编辑
- **Editor optimization (main/frontline)**: improved performance for floor insert/delete on large levels (10k+ floors)
- **에디터 최적화 (main/frontline)**: 대형 패턴(10,000개 이상)의 타일 삽입/삭제 성능 최적화

- **移除冗余代码**：清理不再使用的文件和旧版 UI 组件
- **Removed dead code**: cleaned up unused files and legacy UI components
- **불필요한 코드 제거**: 더 이상 사용되지 않는 파일 및 레거시 UI 구성 요소 정리
