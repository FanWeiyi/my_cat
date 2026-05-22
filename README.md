# My Cat

`My Cat` 是一个 Windows 桌面小猫原型。当前 V0.2 仍只放一只透明背景
小猫在桌面上，但它会在主屏幕工作区里慢慢换地方，能被拖到身边，会轻轻
注意鼠标和当前窗口，也会在点击与观察记录后给出回应。

## 项目结构

```text
cat-core        行为状态、事件模型和轻学习规则
cat-assets      可替换的占位动作片段与帧元数据
windows-shell   WPF 透明桌面窗口、托盘和交互入口
tests           不依赖外部测试包的核心行为检查
```

## 当前原型范围

本地已覆盖 `M0-M4` 与 `V0.2` 桌面生命感原型：

- 透明、无边框、围绕单只小猫尺寸收紧的 WPF 桌面窗口。
- 点击小猫后的回应动作和轻菜单，拖动后可在安全位置安顿。
- 坐着、睡觉、跨区域慢走、窗口边缘停留等自动行为循环。
- 鼠标靠近时的冷却注意回应。
- 托盘中的记录入口、安静模式和退出入口。
- 对“睡觉 / 玩 / 陪我”三类观察记录做本地 JSON 持久化并立即回应。
- 基于时间桶的轻量习惯权重，影响后续休息、走动和陪伴倾向。
- 同类记录累计后的一次性学习反馈。
- 小猫菜单和托盘都可切换安静模式，新增主动互动也会收敛。
- 点击、拖动、告诉它、安静模式、鼠标注意和窗口停留会写入本地试玩计数。
- 已接入稳定动作 ID：
  `idle_sit`、`rest_sleep`、`walk_slow`、`wake_stretch`、`edge_stop`、
  `pet_react`、`drag_settle`、`mouse_notice`、`window_linger` 与观察记录回应。

后续里程碑暂未做：

- 多猫支持。
- 正式动画资产与更高质量视觉稿。

观察记录保存位置：

```text
%LOCALAPPDATA%\MyCat\events.json
```

学习反馈状态保存位置：

```text
%LOCALAPPDATA%\MyCat\learning-state.json
```

试玩互动计数保存位置：

```text
%LOCALAPPDATA%\MyCat\interaction-metrics.json
```

## 文档入口

- [开发进度报告](docs/development-progress-report.md)
- [试玩说明](docs/playtest-guide.md)
- [反馈表](docs/playtest-feedback.md)
- [试玩发布检查清单](docs/playtest-release-checklist.md)

## 本地开发

原型使用 `.NET 9` 和 Windows WPF。构建前需要安装 `.NET 9 SDK`；
仅安装 Windows Desktop Runtime 不足以开发和构建。

构建并运行桌面壳：

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:APPDATA = "$PWD\.nuget-home"
dotnet build .\MyCat.sln --configfile .\NuGet.Config
dotnet run --project .\windows-shell\MyCat.WindowsShell.csproj --no-restore
```

运行核心行为检查：

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet-home"
$env:APPDATA = "$PWD\.nuget-home"
dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj --configfile .\NuGet.Config
```

生成 Windows x64 试玩包：

```cmd
scripts\package-playtest.cmd
```

产物会写入 `artifacts\playtest`。默认打包尝试生成自包含版本，
测试者不需要单独安装 .NET；它会在需要时通过 `NuGet.Publish.Config`
从 NuGet 恢复官方 runtime pack。

如果当前网络环境无法下载 runtime pack，可生成依赖本机 .NET 运行时的版本：

```cmd
scripts\package-playtest.cmd -FrameworkDependent
```

## 手动验收清单

1. 启动程序，确认桌面上只看到小猫内容，没有明显背景块。
2. 点击小猫，确认它先回应，再打开轻菜单。
3. 把小猫拖到屏幕不同区域，确认松手后会安顿且不误开菜单。
4. 鼠标靠近小猫，确认它偶尔注意你，连续经过不会刷屏。
5. 从小猫菜单记录“睡觉 / 玩 / 陪我”，确认小猫和反馈提示都有回应。
6. 从托盘记录同样三类事件。
7. 在同一时间桶连续记录同类事件 3 次，确认学习反馈只出现一次。
8. 重启程序，确认 `%LOCALAPPDATA%\MyCat\events.json` 与试玩计数仍存在。
9. 从小猫菜单或托盘切换安静模式，确认跨区域走动、鼠标和窗口互动明显收敛。
10. 关闭安静模式后观察坐、睡、慢走、醒来伸懒腰、窗口停留和边缘停下动作。
11. 确认移动与拖动落点不会越出主屏幕工作区。
12. 从托盘选择 `退出`，确认程序可靠结束。
13. 在真实桌面连续运行 30 分钟，观察稳定性与干扰感。
