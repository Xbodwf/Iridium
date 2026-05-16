> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

## 新增

1. 新增: 特效动画速度随音高变化 (ScaleFilterSpeedWithPitch)
   当降低音高录制谱面时，所有 ffx+ 特效（滤镜/闪光/移动等）动画速度同步跟随音高，防止后期加速后动画过快。

2. 新增: GitHub Issue 模板（Bug 报告 / 功能请求 / 提问）

3. 新增: LGPL v3 许可证

## 优化

1. 性能优化: Logger.TaskRun() 空队列时跳过每帧 Task 创建
2. 性能优化: Localization.Get() 缓存当前语言字典引用
3. 性能优化: Settings 面板 Tab 名称缓存，避免每帧 LINQ 分配
4. 性能优化: Logger.GetIndent() 缩进字符串缓存
5. 性能优化: AsyncPatchManager 防抖计时 DateTime.Now -> Stopwatch
6. 优化: PatchManager 抽取 RegisterNestedPatches 辅助方法，消除 ~70 行重复反射代码
7. 清理: 移除多处无用 using 导入

## 修复

1. 修复: ExitGUIException - GUILayout Mismatched LayoutGroup 错误 (PR #3)
2. 修复: 2.10.0(frontline) 与 2.9.8 版本交叉编译导致的兼容问题
3. 修复: 重设项目结构，统一 UI 样式
