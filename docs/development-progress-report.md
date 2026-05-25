# My Cat 开发进度报告

日期：2026-05-22  
工作区：`D:\work\code\my_cat`  
目标远端：`FanWeiyi/my_cat`

## 当前状态

本地原型已经达到既定 `M0-M4` 试玩范围。

当前版本是一个 Windows WPF 桌面陪伴原型：桌面上有一只透明背景的个性化小猫，
已接入完整 PNG 动作包、观察记录、本地持久化、轻量习惯偏好、学习反馈、
安静模式、试玩文档和可重复打包流程。

当前可确认状态：

- 本地 Windows 原型可以运行。
- Release 构建和核心行为检查通过。
- 可在本地生成 framework-dependent 试玩 ZIP。
- 源码还没有从当前工作区成功同步到 GitHub：
  原始 `.git` 元数据在当前会话不可写，GitHub 推送认证返回 `401`。

## 里程碑完成情况

| 里程碑 | 状态 | 说明 |
|---|---|---|
| M0 技术可行性 | 本地完成 | 透明窗口、点击区域、托盘退出、桌面位置管理。 |
| M1 基础陪伴 | 本地完成 | 坐、睡、慢走、点击回应和自动状态切换。 |
| M2 “告诉它”闭环 | 本地完成 | 小猫菜单、托盘记录、三类事件、JSON 持久化、`已记下` 反馈。 |
| M3 轻学习 | 本地完成 | 时间桶、权重画像、行为偏置、一次性学习反馈、安静模式。 |
| M4 试玩准备 | 本地完成 | 动作 ID 接入、试玩说明、反馈表、发布检查清单、打包脚本。 |
| V0.3 身份化美术接入 | 本地完成 | `my-cat` 清单、208 张 PNG 帧、严格资产校验、桌面 PNG 播放。 |

## 当前已有能力

### 桌面壳

- 透明、无边框、置顶的 WPF 桌面窗口，窗口尺寸围绕单只小猫收紧。
- 点击小猫会触发回应动作并打开轻菜单。
- 系统托盘可记录事件、切换安静模式并退出。
- 小猫位置限制在主屏幕工作区内，慢走到边缘会进入停下动作。

关键文件：

- `windows-shell/DesktopCatWindow.cs`
- `windows-shell/TrayIconHost.cs`
- `windows-shell/CatSprite.cs`

### 动作与行为

已接入动作 ID：

| 动作 ID | 当前原型表现 |
|---|---|
| `idle_sit` | 坐着发呆并眨眼。 |
| `rest_sleep` | 趴睡和呼吸起伏。 |
| `walk_slow` | 在桌面工作区内慢慢走动，左右方向各用一套 PNG。 |
| `wake_stretch` | 从休息离开前先醒来伸懒腰。 |
| `edge_stop` | 到桌面边缘后停下看一眼。 |
| `pet_react` | 点击优先回应。 |
| `drag_settle` | 被拖动放下后安顿。 |
| `mouse_notice` | 鼠标靠近后的短暂注意。 |
| `window_linger` | 在窗口边缘停留观察。 |
| 观察回应 | 对睡觉、玩、陪我记录给出对应动作。 |

当前视觉已使用 `cat-assets/cats/my-cat/` 中的透明 PNG 帧序列。启动时会读取
`manifest.json` 并严格检查帧数量、PNG 尺寸和 RGBA 资产格式；资产缺失时不回退
到占位绘制。

关键文件：

- `cat-core/CatBehaviorController.cs`
- `cat-assets/CatAnimationCatalog.cs`
- `windows-shell/CatSprite.cs`

### 观察记录与轻学习

支持记录三类真实猫咪观察：

- 我家猫在睡觉。
- 我家猫在玩。
- 我家猫在陪我。

数据位置：

```text
%LOCALAPPDATA%\MyCat\events.json
%LOCALAPPDATA%\MyCat\learning-state.json
```

学习规则现状：

- 事件会被分到 morning、afternoon、evening、night 四个时间桶。
- 某时间桶内重复记录休息、活动或陪伴，会影响该时间段后续动作选择权重。
- 同类记录在同一时间桶累计到 3 次后，可触发一次学习反馈。
- 安静模式会明显抑制主动慢走。

关键文件：

- `cat-core/CatObservationEvent.cs`
- `cat-core/JsonCatEventStore.cs`
- `cat-core/CatHabitProfile.cs`
- `cat-core/CatLearningFeedbackTracker.cs`

## 运行、验证和打包

### 在 CMD 中本地运行

```cmd
cd /d D:\work\code\my_cat
set DOTNET_CLI_HOME=%CD%\.dotnet-home
set APPDATA=%CD%\.nuget-home
dotnet run --project .\windows-shell\MyCat.WindowsShell.csproj --no-restore
```

### 验证命令

```cmd
cd /d D:\work\code\my_cat
set DOTNET_CLI_HOME=%CD%\.dotnet-home
set APPDATA=%CD%\.nuget-home
dotnet build .\MyCat.sln -c Release --configfile .\NuGet.Config
dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj -c Release --configfile .\NuGet.Config
```

### 生成试玩包

默认自包含包：

```cmd
scripts\package-playtest.cmd
```

受限网络环境备用包：

```cmd
scripts\package-playtest.cmd -FrameworkDependent
```

输出位置：

```text
artifacts\playtest\MyCat-playtest-win-x64\
artifacts\playtest\MyCat-playtest-win-x64.zip
```

试玩相关文档：

- `docs/playtest-guide.md`
- `docs/playtest-feedback.md`
- `docs/playtest-release-checklist.md`
- `docs/playtest-runbook.txt`

## 已完成验证

本地已完成：

- 已安装并识别 `.NET 9 SDK`。
- 2026-05-22 Release 方案构建通过。
- 2026-05-22 核心行为检查通过。
- 桌面壳启动探测中进程保持运行。
- framework-dependent 试玩包已成功生成。
- 打包后可执行文件启动探测中进程保持运行。

大范围分享前仍需手动验证：

- 在真实桌面完成一次不间断 30 分钟运行。
- 对最终发给测试者的包手动确认：点击、托盘、记录、安静模式和退出。
- 收集 5 到 10 位目标用户反馈。

## 已知约束与风险

### 工程约束

- 当前工作区的 GitHub 同步尚未完成。
- 当前源码变更已镜像到临时 Git 元数据目录 `.publish-git`；
  原仓库 `.git` 仍会在 `git add` 时因 `index.lock: Permission denied` 失败。
- 从临时 Git 元数据通过 HTTPS 推送到 GitHub 时返回 `401`；
  当前会话也没有可用的 `gh`。
- 自包含打包需要从 NuGet 恢复 runtime pack；
  当前受限会话无法访问 `api.nuget.org`，因此这里只验证了
  framework-dependent 备用包。

### 产品风险

- 个性化美术包已经接入，但“可爱”和“有陪伴感”仍需要真实桌面试玩检验。
- 当前学习逻辑刻意保持很轻，是否继续加复杂度应由试玩反馈决定。
- 现阶段只面向主屏幕上的单只 Windows 桌面猫；
  多屏、深度个性化和成品级设置项都还不是完成态。

## 建议的下一轮迭代

推荐顺序：

1. 先把当前源码正常同步到 GitHub，恢复普通分支和 PR 流程。
2. 用当前 M4 版本找 5 到 10 位目标用户试玩。
3. 将反馈分为三类：
   桌面干扰、猫的存在感、“告诉它/它会变”的清晰度。
4. 在真实桌面上重点回看坐着、睡觉、点击回应和左右慢走的播放节奏。
5. 根据反馈再决定下一步下注方向：
   身份化/照片生成、更强学习，或先继续做桌面体验打磨。

在试玩还没有回答“单只桌面猫是否值得长期放着”之前，
不建议直接跳到照片生成、多猫、移动端或设备接入。

## 临时提交轨迹

当前临时发布分支已准备好以下里程碑提交：

```text
d270116 add playtest packaging flow
4c0cd3c label playtest scope
9a9b170 prepare m4 playtest prototype
7320369 add habit learning and quiet mode
8312799 add cat observation recording flow
2d77138 verify desktop cat build setup
97b092b build windows desktop cat prototype
27040dc bootstrap repository
```
