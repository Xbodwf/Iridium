> [!WARNING]
> Iridium 从 1.3.0_beta1 开始支持 MelonLoader，从 1.4.0_beta5 开始支持 BepInEx。有关在使用它们的 ADOFAI 上安装 Iridium，请查看对应的使用文档。
>
> Iridium has supported MelonLoader since 1.3.0_beta1, and BepInEx since 1.4.0_beta5. Please refer to the corresponding documentation for installing Iridium on ADOFAI using these loaders.
>
> Iridium은 1.3.0_beta1부터 MelonLoader를, 1.4.0_beta5부터 BepInEx를 지원합니다. 해당 로더를 사용하는 ADOFAI에 Iridium을 설치하는 방법은 각각의 사용 문서를 참고하세요.

### 变更 / Changes / 변경 사항

1. **重构了 IMGUI 滑条渲染器，修复了错误嵌套布局导致的滑条无法交互问题，并对齐了 UGUI 滑条布局结构。**
1. **Refactored the IMGUI slider renderer to fix an incorrect nested layout that prevented slider interaction, and aligned the UGUI slider layout structure.**
1. **IMGUI 슬라이더 렌더러를 재구성하여 잘못된 중첩 레이아웃으로 인해 슬라이더를 조작할 수 없던 문제를 수정하고, UGUI 슬라이더 레이아웃 구조를 정렬했습니다.**

2. **修复了因浮点数赋给整数变量导致的滑块无法拖动问题。**
2. **Fixed slider being undraggable due to assigning a float value to an integer field.**
2. **부동 소수점 값을 정수 필드에 할당하여 슬라이더를 드래그할 수 없던 문제를 수정했습니다.**

3. **修复了压缩装饰导致的装饰物大小不正确与装饰错位问题。**
3. **Fixed incorrect decoration size and misalignment caused by compressed decorations.**
3. **압축된 장식으로 인한 장식 크기 오류 및 위치 어긋남 문제를 수정했습니다.**

4. **补齐了 main 构建对 v2 Runtime 系列程序集的依赖，现在 `dotnet build` 会自动打包 Iridium.Runtime.Abstractions/Mono/Il2Cpp。**
4. **Added missing v2 Runtime assembly dependencies to the main build; `dotnet build` now automatically packages Iridium.Runtime.Abstractions/Mono/Il2Cpp.**
4. **main 빌드에 v2 Runtime 어셈블리 종속성을 추가했습니다. 이제 `dotnet build`가 Iridium.Runtime.Abstractions/Mono/Il2Cpp를 자동으로 패키징합니다.**
