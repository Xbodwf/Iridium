> [!IMPORTANT]
> 若您在使用Iridium Beta版时出现问题，请及时向维护者报告。

## r22

### 新功能

1. 新增: 粒子装饰物优化（对象池、远离更新跳过、视锥剔除、LOD优化）
2. 新增: 粒子优化开关配置项
3. 新增: 传送卡死修复、飙速模式背景同步修复 (frontline v2.10.0)
4. 新增: 编辑器暂停快捷键，支持 Ctrl/Shift/Alt/Win 组合键 (frontline v2.10.0)
5. 新增: 自定义关卡谱面读取优化 — 将 `Json.Deserialize` 替换为 `Json.DeserializePartially(str, "actions")`，在读取谱面元数据时跳过整个 `actions` 数组的解析（main + frontline 的 `LevelDataCLS.LoadLevel`，frontline 额外包含 `LevelData.GetCustomLevelName`）
6. 新增: 设置开关 `customLevelReadOptimization` + i18n 多语言支持

### 修复

1. 修复: 移除导致编译错误的旧粒子优化实现，替换为更稳定的方案
