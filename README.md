# My Cat

My Cat 是一个 Windows 桌面小猫陪伴原型。它会把一只基于真实猫咪绘制的透明小猫放在桌面上：可以坐着、睡觉、慢走、被点击回应、被提起拖动，也会对鼠标、窗口边缘、任务栏区域和真实猫观察记录做轻量反应。

当前版本：**My Cat V0.5：行为节奏可视化设置 + 新版拖拽美术**。

这一版在 V0.4 桌面环境感知互动基础上，加入了“行为节奏设置”窗口，并重绘了 `drag_lift / drag_hold / drag_drop` 三段拖拽动作。现在拖动开始、拖住、松手落下会保持同一套露肚皮悬空姿态，不再混入旧的提起动作。

V0.4 产品方案见：[docs/v0.4-desktop-environment-interaction-plan.md](docs/v0.4-desktop-environment-interaction-plan.md)。

## 当前能力

- 透明、无边框的 WPF 桌面小猫窗口。
- 自动行为包括坐着、睡觉、慢走、醒来伸懒腰、窗口边缘停留、边缘停下观察。
- 点击小猫后先播放摸摸回应，再进入约 3 秒视线关注。
- 视线关注支持上、下、左、右四向，会按鼠标相对小猫中心的位置实时选择方向。
- 鼠标靠近时，小猫会在合适时机短暂注意你，并保留冷却，避免频繁打扰。
- 支持拖动小猫；移动超过阈值后进入新版抱起姿态，拖动中保持新版露肚皮悬空动作，松手后播放新版落下动作。
- 用户拖动时可进入完整主屏幕范围，包括 Windows 底部任务栏区域。
- 已加入桌面环境感知基础：可读取主屏幕、任务栏和前台窗口信息。
- 支持窗口让开、任务栏短暂停留等 V0.4 行为基础。
- 可记录三类真实猫观察：睡觉、玩、陪我。
- 观察记录会保存到本地，并影响后续行为倾向。
- 可从小猫菜单或托盘打开“行为节奏设置”，按早晨、下午、晚上、夜间调整休息/活动/陪伴比例。
- 未手动设置的时间段继续使用观察记录学习到的默认值；手动保存后，该时间段以用户设置为准。
- 支持安静模式，减少主动走动和打扰。
- 美术资产使用严格 manifest 校验，缺失或格式错误会启动失败，不静默回退。

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
- `512 x 512`
- 每个动作 16 帧
- 通过 `manifest.json` 严格声明
- 程序启动时会校验资产，缺失或格式错误会直接报错，不会回退到占位猫

近期美术更新：

- `drag_lift` 已重绘为整只小猫被提起、肚皮朝前露出的动作。
- `drag_hold` 已重绘为拖动中循环悬空动作，只有轻微呼吸、爪子小幅晃动和尾巴轻摆。
- `drag_drop` 已重绘为从新版悬空姿态软落下并回到坐姿的动作。
- `drag_hold` 与 `drag_drop` 的源图保存在 `cat-assets/cats/my-cat/source/drag_hold-sheet-*` 和 `drag_drop-sheet-*`。

## 项目结构

```text
cat-core        小猫行为状态、观察记录、轻量习惯学习、行为节奏设置数据
cat-assets      美术包 manifest、PNG 动作帧、资产校验和动画 catalog
windows-shell   Windows WPF 桌面窗口、托盘菜单、鼠标、拖拽、设置窗口和桌面环境感知
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

.NET 9 Desktop Runtime 下载页：

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

如果刚刚构建过，也可以直接运行调试版：

```text
windows-shell/bin/Debug/net9.0-windows/MyCat.WindowsShell.exe
```

启动后，桌面上会出现小猫。可以点击、拖动，或通过小猫菜单/托盘菜单记录真实猫状态、打开行为节奏设置、切换安静模式和退出。

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

## 打包给别人使用

推荐使用脚本生成 Windows x64 试玩包：

```cmd
scripts\package-playtest.cmd
```

默认输出自包含包，对方解压后可直接运行，不需要安装 .NET：

```text
artifacts/playtest/MyCat-playtest-win-x64/
artifacts/playtest/MyCat-playtest-win-x64.zip
```

如果当前网络环境无法下载自包含运行时包，可以生成框架依赖版：

```cmd
scripts\package-playtest.cmd -FrameworkDependent
```

框架依赖版可以发给别人测试，但对方电脑需要安装 `.NET 9 Desktop Runtime`。

`artifacts/` 已在 `.gitignore` 中，打包产物不建议提交到 Git；需要公开下载时，建议上传到 GitHub Release。

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
behavior-settings.json     手动行为节奏设置
```

删除这个目录可以重置本地原型数据。

## 手动验收清单

1. 启动程序，确认桌面上只看到小猫，没有粉色背景块或旧占位猫。
2. 点击小猫，确认先播放摸摸回应，再进入短暂视线关注。
3. 在视线关注期间，把鼠标分别移到小猫左、右、上、下，确认方向会实时变化。
4. 轻点小猫不会误触发拖动。
5. 按住并移动超过阈值后，确认小猫进入新版露肚皮抱起姿态。
6. 继续拖动 2 秒以上，确认拖住期间仍保持新版悬空姿态，没有跳回旧提起动作。
7. 松手后确认播放新版落下动作，并最终回到坐姿或后续自动动作。
8. 将小猫拖到底部任务栏区域，确认不会被工作区边界拦住。
9. 观察坐姿，确认眨眼连续自然。
10. 观察睡觉，确认整体安静，偶尔有轻微呼吸感。
11. 从小猫菜单或托盘打开“行为节奏设置”，切换四个时间段并调整休息/活动/陪伴比例。
12. 保存某个时间段设置后重启程序，确认设置仍然存在。
13. 鼠标靠近小猫，确认它会短暂注意你，但不会频繁刷动作。
14. 记录“真猫在睡觉 / 玩 / 陪我”，确认小猫有对应回应。
15. 切换安静模式，确认主动行为明显减少。
16. 托盘菜单可以正常记录、打开设置、切换安静模式和退出。
17. 连续运行 30 分钟，观察是否稳定、是否干扰桌面工作。

## 推送到远端

当前仓库远端为：

```text
origin  https://github.com/FanWeiyi/my_cat.git
```

提交前先检查改动：

```powershell
git status --short
```

建议提交源码、美术资源和文档，不提交 `artifacts/` 构建产物：

```powershell
git add README.md docs cat-assets cat-core tests windows-shell
git commit -m "Update desktop cat behavior settings and drag art"
git push origin main
```

如果你当前不在 `main` 分支，先查看分支名：

```powershell
git branch --show-current
```

然后把最后一行改成：

```powershell
git push origin <当前分支名>
```

如果推送时提示需要登录 GitHub，请按终端提示完成浏览器登录，或使用 GitHub token。若要发布可下载 zip，推荐在 GitHub 网页创建 Release，并上传：

```text
artifacts/playtest/MyCat-playtest-win-x64.zip
```
