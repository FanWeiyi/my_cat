# My Cat

一个 Windows 桌面小猫陪伴原型。它会把一只基于真实猫咪美术资产制作的小猫放在透明桌面窗口中：可以坐着、睡觉、慢走、被点击回应、被抱起拖动，也会对鼠标、窗口边缘和任务栏区域做轻量感知。

当前版本重点是 **My Cat V0.4：桌面环境感知互动**。这一版的目标不是增加大量“桌宠功能”，而是让小猫更像真的生活在桌面里：看见鼠标、被拖动时像被抱起、靠近窗口或任务栏边缘时能更自然地停留和反馈。

V0.4 产品方案见：[docs/v0.4-desktop-environment-interaction-plan.md](docs/v0.4-desktop-environment-interaction-plan.md)。

## 当前能力

- 透明、无边框的 WPF 桌面小猫窗口。
- 点击小猫后先播放摸摸回应，再进入约 3 秒视线关注。
- 视线关注支持上、下、左、右四向，会按鼠标相对小猫中心的位置实时选择方向。
- 鼠标靠近时，小猫会在合适时机短暂注意你，并保留冷却，避免频繁打扰。
- 支持拖动小猫；移动超过阈值后进入抱起状态，拖动中保持被提起姿态，松手后播放放下/安顿动作。
- 用户拖动时可进入完整主屏幕范围，包括 Windows 底部任务栏区域。
- 自动行为包括坐着、睡觉、慢走、醒来伸懒腰、窗口边缘停留、边缘停下观察。
- 已加入桌面环境感知基础：可读取主屏幕、任务栏和前台窗口信息。
- 支持窗口让开、任务栏短暂停留等 V0.4 行为基础。
- 可记录三类真实猫观察：睡觉、玩、陪我。
- 观察记录会保存到本地，并影响后续行为倾向。
- 支持安静模式，减少主动走动和打扰。
- 美术资产使用严格 manifest 校验，缺失或格式错误会启动失败，不静默回退。
- 已处理主要动作 PNG 的半透明白边问题。

## 动作资产

当前美术包路径：

```text
cat-assets/cats/my-cat
```

已覆盖动作：

```text
idle_sit
rest_sleep
walk_slow_left
walk_slow_right
wake_stretch
edge_stop
pet_react
drag_settle
drag_lift
drag_hold
drag_drop
mouse_notice
mouse_track
mouse_track_left
mouse_track_right
mouse_track_up
mouse_track_down
window_linger
window_startle
window_avoid
taskbar_sit
taskbar_lie
observation_rest
observation_activity
observation_accompany
```

资产要求：

- PNG
- RGBA
- 透明背景
- 512 x 512
- 每个动作 16 帧
- 通过 `manifest.json` 严格声明
- 程序启动时会校验资产，缺失或格式错误会直接报错，不会回退到占位猫

## 近期美术调整

- `idle_sit` 已接入确认版眨眼动作。
- 坐姿第 `5-10` 帧为连续眨眼段，不再单帧固定。
- `mouse_track_left`、`mouse_track_up`、`mouse_track_down` 已接入确认版方向动作。
- 除坐姿和睡觉外，多数短动作已缩短帧间隔，让反馈更连贯。
- 对主要动作帧做了去白边处理，降低透明边缘在深色背景上的白圈感。

## 项目结构

```text
cat-core        小猫行为状态、观察记录、轻量习惯学习
cat-assets      美术包 manifest、PNG 动作帧、资产校验和动画 catalog
windows-shell   Windows WPF 桌面窗口、托盘菜单、鼠标、拖拽和桌面环境感知
tests           不依赖外部测试框架的核心行为检查
scripts         美术稳定性检查与试玩包打包脚本
docs            试玩说明、反馈表和发布检查文档
png             原始美术图输入
```

## 运行环境

开发环境需要：

- Windows
- .NET 9 SDK

只运行已打包程序时：

- 自包含包：解压后直接运行，不需要安装 .NET
- 框架依赖包：需要安装 `.NET 9 Desktop Runtime`

.NET 9 Desktop Runtime 官方下载页：

```text
https://dotnet.microsoft.com/download/dotnet/9.0
```

下载时请选择：

```text
.NET Desktop Runtime 9.0
Windows x64 Installer
```

## 本地运行

在项目根目录执行：

```powershell
dotnet build .\MyCat.sln --configfile .\NuGet.Config
dotnet run --project .\windows-shell\MyCat.WindowsShell.csproj --no-restore
```

启动后，桌面上会出现小猫。可以点击、拖动，或通过托盘菜单进行记录和退出。

如果已经构建到 `artifacts/build-check`，也可以直接打开：

```text
artifacts/build-check/MyCat.WindowsShell.exe
```

## 测试与校验

运行核心行为和资产加载检查：

```powershell
dotnet run --project .\tests\MyCat.CatCore.Tests\MyCat.CatCore.Tests.csproj --configfile .\NuGet.Config
```

运行美术资产稳定性检查：

```powershell
python .\scripts\stabilize-cat-art.py --check
```

运行完整构建：

```powershell
dotnet build .\MyCat.sln --configfile .\NuGet.Config
```

构建 Windows shell 到临时输出目录：

```powershell
dotnet build .\windows-shell\MyCat.WindowsShell.csproj --no-restore -o .\artifacts\build-check
```

## 打包给别人使用

推荐发布 Windows x64 自包含包。对方解压后可直接运行，不需要安装 .NET。

注意：自包含发布需要能访问 NuGet 官方源。当前项目根目录的 `NuGet.Config` 为了离线/受控还原清空了 package source，如果发布时报 `NU1100`，请使用下面带 `RestoreSources` 的命令。

```powershell
dotnet publish .\windows-shell\MyCat.WindowsShell.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\artifacts\release\my-cat-v0.4-win-x64 `
  /p:PublishReadyToRun=true `
  /p:RestoreSources=https://api.nuget.org/v3/index.json
```

压缩发布目录：

```powershell
Compress-Archive `
  -Path .\artifacts\release\my-cat-v0.4-win-x64\* `
  -DestinationPath .\artifacts\release\my-cat-v0.4-win-x64.zip `
  -Force
```

把这个 zip 发给别人：

```text
artifacts/release/my-cat-v0.4-win-x64.zip
```

对方使用方式：

1. 解压 zip。
2. 双击 `MyCat.WindowsShell.exe`。
3. 不需要安装 .NET。

如果当前网络环境无法下载自包含运行时包，可以生成框架依赖版：

```cmd
scripts\package-playtest.cmd -FrameworkDependent
```

框架依赖版可以发给别人测试，但对方电脑需要安装 `.NET 9 Desktop Runtime`。

## 本地数据

程序会把原型数据保存在：

```text
%LOCALAPPDATA%\MyCat
```

主要文件：

```text
events.json                观察记录
learning-state.json        学习反馈状态
interaction-metrics.json   试玩互动计数
```

删除这个目录可以重置本地原型数据。

## 手动验收清单

1. 启动程序，确认桌面上只看到小猫，没有粉色背景块或旧占位猫。
2. 点击小猫，确认先播放摸摸回应，再进入短暂视线关注。
3. 在视线关注期间，把鼠标分别移到小猫左、右、上、下，确认方向会实时变化。
4. 轻点小猫不会误触发拖动。
5. 按住并移动超过阈值后，确认小猫进入抱起/拖动中姿态。
6. 松手后确认播放放下或安顿动作。
7. 将小猫拖到底部任务栏区域，确认不会被工作区边界拦住。
8. 观察坐姿，确认第 `5-10` 帧眨眼连续自然。
9. 观察睡觉，确认整体安静，偶尔有轻微呼吸感。
10. 鼠标靠近小猫，确认它会短暂注意你，但不会频繁刷动作。
11. 记录“真猫在睡觉 / 玩 / 陪我”，确认小猫有对应回应。
12. 切换安静模式，确认主动行为明显减少。
13. 托盘菜单可以正常记录、切换安静模式和退出。
14. 连续运行 30 分钟，观察是否稳定、是否干扰桌面工作。

## 推送到远端

当前仓库远端为：

```text
origin  https://github.com/FanWeiyi/my_cat.git
```

提交前先检查改动：

```powershell
git status --short
```

建议不要把 `artifacts` 构建产物提交到仓库。提交源码和美术资源：

```powershell
git add README.md docs cat-assets cat-core tests windows-shell
git commit -m "Update My Cat V0.4 desktop interactions"
git push origin main
```

如果只想提交 V0.4 程序和美术，不提交文档草稿，可以去掉 `docs`：

```powershell
git add README.md cat-assets cat-core tests windows-shell
git commit -m "Update My Cat V0.4 desktop interactions"
git push origin main
```

如果推送时提示需要登录 GitHub，请按终端提示完成浏览器登录或输入 GitHub token。

如果要发布可下载 zip，推荐在 GitHub 网页创建 Release，并上传：

```text
artifacts/release/my-cat-v0.4-win-x64.zip
```
