# My Cat

一个 Windows 桌面小猫陪伴原型。它会把一只基于真实猫咪美术资产制作的小猫放在透明桌面窗口中：可以坐着、睡觉、慢走、被点击回应、被拖动后安顿，也可以根据你记录的“真猫在睡觉 / 玩 / 陪我”给出反馈。

当前版本重点是 **My Cat V0.3：现实小猫身份化全动作美术接入**。程序已接入 `my-cat` PNG 序列帧美术包，不再使用代码绘制的占位猫。

## 当前能力

- 透明、无边框的 WPF 桌面小猫窗口。
- 点击小猫后触发摸摸回应，并打开轻菜单。
- 支持拖动小猫，松手后播放安顿动作。
- 自动行为包括坐着、睡觉、慢走、醒来伸懒腰、窗口边缘停留、边缘停下观察。
- 鼠标靠近时，小猫会在合适时机短暂注意你。
- 可记录三类真实猫观察：睡觉、玩、陪我。
- 观察记录会保存到本地，并影响后续行为倾向。
- 支持安静模式，减少主动走动和打扰。
- 已接入 13 组动作美术资产，每组 16 帧。
- 坐姿和睡觉动作已做降抖处理：坐姿约每 12 秒轻微动一下，睡觉约每 30 秒轻微呼吸一次。

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
mouse_notice
window_linger
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

## 项目结构

```text
cat-core        小猫行为状态、观察记录、轻量习惯学习
cat-assets      美术包 manifest、PNG 动作帧、资产校验和动画 catalog
windows-shell   Windows WPF 桌面窗口、托盘菜单、鼠标和拖拽交互
tests           不依赖外部测试框架的核心行为检查
scripts         美术稳定性检查与试玩包打包脚本
docs            试玩说明、反馈表和发布检查文档
png             原始美术图输入
```

## 运行环境

开发环境需要：

- Windows
- .NET 9 SDK

只运行已打包程序时，需要：

- 如果是自包含包：解压后直接运行
- 如果是框架依赖包：需要安装 `.NET 9 Desktop Runtime`

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

## 打包

生成 Windows x64 试玩包：

```cmd
scripts\package-playtest.cmd
```

产物会输出到：

```text
artifacts/playtest/MyCat-playtest-win-x64.zip
```

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
2. 点击小猫，确认先播放摸摸回应，再打开菜单。
3. 拖动小猫到不同区域，确认松手后播放安顿动作。
4. 观察坐姿，确认身体稳定，偶尔轻微动一下。
5. 观察睡觉，确认整体安静，偶尔有轻微呼吸感。
6. 鼠标靠近小猫，确认它会短暂注意你，但不会频繁刷动作。
7. 记录“真猫在睡觉 / 玩 / 陪我”，确认小猫有对应回应。
8. 切换安静模式，确认主动行为明显减少。
9. 托盘菜单可以正常记录、切换安静模式和退出。
10. 连续运行 30 分钟，观察是否稳定、是否干扰桌面工作。

## 推送到远端

当前仓库远端为：

```text
origin  https://github.com/FanWeiyi/my_cat.git
```

第一次提交并推送可以执行：

```powershell
git add .
git commit -m "接入 My Cat V0.3 个性化小猫美术包"
git branch -M main
git push -u origin main
```

之后日常更新可以执行：

```powershell
git add .
git commit -m "更新小猫动画节奏"
git push
```

如果推送时提示需要登录 GitHub，请按终端提示完成浏览器登录或输入 GitHub token。
