# 无情按键 | SwqClicker

一款基于 [AshClicker](https://github.com/lj2000lj/AshClicker) 修改的剑网 3 按键连发工具。

> 本项目是非官方修改版，与原项目作者没有隶属关系。原项目 Copyright (C) 2024 Baleine，按 GPL-3.0 发布。

## 功能

- 按键映射：按下一个键，自动连发另一个键
- 两种模式：长按连发、按键切换
- 可调连发间隔和抬起间隔
- 精确延迟、音效提示
- 单文件程序，不包含联网和自动更新功能

## 环境要求

- Windows 10 / 11 x64
- .NET Framework 4.8
- 管理员权限
- Interception 驱动（第一次使用时通过程序安装，并重启电脑）

## 使用

1. 从 [Releases](https://github.com/ZhaoQisi/SwqClicker/releases) 下载 `SwqClicker.exe`。
2. 右键选择“以管理员身份运行”，或在 UAC 提示中选“是”。
3. 点击“安装驱动”，等待安装成功后重启电脑。
4. 重启后再次打开程序，点击“添加键位”。
5. 设置“按下的键”“连发的键”和模式，点击“添加按键”。
6. 点击“按键开启”：
   - 长按连发：按住“按下的键”才连发，松开停止。
   - 按键切换：按一下“按下的键”开始，再按一下停止。
7. 不需要时点击“按键关闭”。

配置文件为程序目录下的 `AshConfig.json`，修改会自动保存。

> “按键切换”会把触发键注册为全局热键，可能被系统吃掉，建议使用 `F12`、`` ` `` 这类游戏里不常用的键。

## 从源码构建

需要 Windows 和 .NET 8 SDK：

```powershell
dotnet restore
dotnet build -c Release
```

输出：

```text
bin\Release\net461\SwqClicker.exe
```

也可以直接运行：

```powershell
dotnet run --project AshClicker.csproj
```

## 注意事项

- 程序需要管理员权限，并会安装 Interception 键盘过滤驱动，请从本仓库的 Releases 下载。
- 不要把“按下的键”和“连发的键”设成同一个键。
- 开启期间按键会发送到当前前台窗口，请不要在聊天框、密码框等位置误触。
- 使用本工具可能违反游戏规则，请自行判断并承担风险；开发者不对使用后果负责。
- 本项目不联网、不包含自动更新；驱动和语音素材属于第三方内容，请遵守各自的许可。

## 第三方与许可

本项目使用 [GNU 通用公共许可证第 3 版（或后续版本）](https://www.gnu.org/licenses/gpl-3.0.zh-cn.html) 发布，完整协议见 [LICENSE](LICENSE)。

- 原项目：[AshClicker](https://github.com/lj2000lj/AshClicker)，Copyright (C) 2024 Baleine
- Interception：[oblitum/Interception](https://github.com/oblitum/Interception)，按上游说明使用 LGPL，仅限非商业用途
- 语音素材：VOICEVOX ずんだもん，使用条款见 [这里](https://zunko.jp/con_ongen_kiyaku.html)

更完整的来源和第三方说明见 [NOTICE.md](NOTICE.md)。

## 联系

问题或建议请提交到 [GitHub Issues](https://github.com/ZhaoQisi/SwqClicker/issues)。
