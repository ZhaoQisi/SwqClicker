# 无情按键 | SwqClicker

一款基于 [AshClicker](https://github.com/lj2000lj/AshClicker) 修改的剑网 3 按键连发工具。

> 本项目是非官方修改版，与原项目作者没有隶属关系。原项目 Copyright (C) 2024 Baleine，按 GPL-3.0 发布。

## 功能

- 按键映射：按下一个键，自动连发另一个键
- 方案名：每条映射可以自定义方案名，列表里用 `1|Q|切换` 这种紧凑格式显示绑定关系
- 两种模式：长按连发、按键切换
- 全局快捷键：一键开启 / 关闭连发，不用切回程序窗口
- 暂停键：按住暂停键临时停止所有连发，松开继续；暂停期间键盘恢复原始输入
- 按键拦截：开启后「按下的键」被驱动拦截，不会传给游戏，游戏只会收到连发出来的键
- 最小化状态条：最小化后显示图标 + 状态，绿色表示已开启/正在连发哪个方案，红色表示已关闭/暂停
- 可调连发间隔和抬起间隔
- 精确延迟、音效提示（只响应总开关的开启/关闭）
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
5. 填写“方案名称”（默认按顺序给出“自定义键位1、2、3…”），设置“按下的键”“连发的键”和模式，点击“添加按键”。
6. 可选：点击“开关快捷键”和“暂停键”，按下想使用的按键完成设置（Esc 取消，Backspace 清除）。暂停键可以是 Alt、Ctrl、Shift 这类修饰键。
7. 点击“按键开启”：
   - 长按连发：按住“按下的键”才连发，松开停止。
   - 按键切换：按一下“按下的键”开始，再按一下停止。
8. 连发过程中：
   - 按“开关快捷键”可以直接关闭连发；
   - 按住“暂停键”期间所有连发暂停，此时按键会原样输入给系统/游戏；松开暂停键立刻恢复，之前处于连发或切换状态的键会继续；
   - 暂停键设为修饰键（如 Alt）时，这个键仍然会传给系统，Alt+Tab 等组合可以正常使用；设为普通键时该键会被拦下，不会传给游戏。
9. 最小化窗口后会显示一个小状态条：图标后面显示“已关闭”（红）、“已开启”（绿）或正在连发的方案名（绿色，暂停时变红）；双击状态条恢复主界面，按住可以拖动，右键可以显示主界面或退出。

配置文件为程序目录下的 `AshConfig.json`，修改会自动保存。

> 开关快捷键和暂停键不能与映射里的“按下的键”重复，否则会被拦截逻辑吞掉。
> 开启后“按下的键”由驱动层拦截，不会传给游戏；未安装驱动时无法拦截，按键仍会传到游戏。

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
