# 安装与覆盖升级

双击 `Catcon-1.9.1-Setup.exe` 即可安装，无需手工解压或分别复制资源文件。默认装在 `%LOCALAPPDATA%\Programs\Catcon`，可更换安装位置。安装器创建开始菜单入口、可选桌面快捷方式，并登记 Windows「已安装的应用」卸载入口。

安装器本身为原生 Windows 程序，不依赖 .NET；支持 Windows 10 / 11 的 x86 / x64 系统。猫咪需要 .NET Framework 4.8 或兼容的更高版本。已具备运行库时，安装全程无需联网，也不需要管理员权限。缺少运行库时，安装器先释放随包携带的微软官方 Web 安装程序，再请求管理员授权下载、安装所需组件；这一步需要联网。运行库取消、安装失败或仍未满足要求时，不继续部署猫咪。若微软安装程序要求重启，会提示先保存工作并重启，再次运行安装包后继续。

安装前会提示关闭正在运行的蜜柑，包括已解压使用的便携版；关闭会保存原来的位置和设置，不强行终止进程。

## 后续升级

以后下载新版 `Catcon-版本号-Setup.exe` 并运行即可。安装身份固定为 `Catcon.Desktop`，安装方式保持当前用户、32 位注册模式，因此后续版本会找到同一个安装位置和卸载条目。程序仍为 AnyCPU，运行在 x86 / x64 系统时无需换安装器。

升级、同版本修复安装保留 `%LOCALAPPDATA%\Catcon\settings.json`，其中包括位置、显示大小、情景识别开关与皮肤选择。新增的自定义皮肤目录也不会被清理。默认配置与蜜柑皮肤更新到新版，旧的以下文件先备份到 `%LOCALAPPDATA%\Catcon\Backups\时间戳`：

- `config/behaviors.json`：动作权重、情景概率及交互阈值。
- `config/classifiers.json`：应用识别和智能兜底规则。
- `config/garden.json`：花圃的画布、调色板、种植位、品种、装饰与湿度 / 分株 / 风参数；改了它就能换花的样子。
- `assets/skins/mikan/skin.json`：默认皮肤的原生像素源文件。

如果修改过默认配置，可从备份中挑选需要的改动迁回新版；备份不会自动覆盖新版本的规则和新增字段。建议自定义皮肤另取目录名，避免直接修改默认蜜柑文件。

更旧的安装包会拒绝覆盖更高版本，且不会改动现有程序。相同版本可以重新安装修复文件。当前提供的是通过新版安装包进行原位升级；尚未接入在线版本服务器或后台自动下载。

卸载时移除安装器登记的程序、资源和快捷方式；保留设置、升级备份及用户自行添加的文件。以后重新安装仍可恢复原设置。需要完全重置时，可在退出程序后手工清理 `%LOCALAPPDATA%\Catcon`。

## 构建安装包

先构建便携版，再准备安装工具并编译安装包：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -SkipArt
powershell -ExecutionPolicy Bypass -File scripts/prepare-installer-tools.ps1
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

准备工具的脚本将 Inno Setup 6.7.3 以便携模式释放到项目 `tools/`，不更改全局编译环境。安装器源码在 `installer/Catcon.iss`；中文消息文件来自 Inno Setup 同版本官方源码中的社区翻译，保留原作者信息。

Inno Setup 工具和微软 .NET Web 安装程序均按固定 SHA-256 校验，准备时还校验发布者数字签名。工具与下载文件不进入源码版本管理；微软引导程序作为安装器内嵌临时依赖，只有缺少运行库时才执行。未来更新这些依赖时，应同步验证并更新固定哈希。

`build-installer.ps1` 自动选择 `dist/Catcon-x.y.z` 中版本最高的一项，并从实际 EXE 读取版本，生成 `dist/Catcon-x.y.z-Setup.exe`。也可使用 `-ReleaseDirectory` 指定发布目录、`-Compiler` 指定已安装的 Inno Setup。后续版本须保留脚本内的固定安装身份和安装模式。

版本号只在 `src/AssemblyInfo.cs` 里写一次，`scripts/build.ps1` 通过 `scripts/release-info.ps1` 读取它来决定发布目录名、ZIP 名和校验和清单，`scripts/test.ps1` 与 `scripts/verify-package.ps1` 也从同一处取路径，避免出现「改了一处、别的脚本还指着旧版本」的情况。发版时只改那一行 `AssemblyVersion` / `AssemblyFileVersion`。

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-installer.ps1
```

安装回归使用随机测试身份、工作区内的中文空格路径及测试快捷方式，实际验证首次安装、1.9.0 → 1.9.1 原位升级、配置备份（含 `config/garden.json`）、设置与皮肤保留、同版本修复、阻止降级、卸载与保留数据。报告在 `artifacts/installer-test-results.json`，详细安装日志在其记录的测试目录。测试不启动日常猫咪实例；缺失运行库、UAC 取消和运行库要求重启的流程仍需在相应干净虚拟机中验收。

安装器尚未使用商业代码签名证书签名。构建后 `dist/SHA256SUMS.txt` 包含便携程序、ZIP 和安装 EXE 的校验值。

## 上游资料

- [Inno Setup 下载与便携模式说明](https://jrsoftware.org/ishelp/topic_technotes.htm)
- [Inno Setup 的相同应用识别规则](https://jrsoftware.org/ishelp/topic_sameappnotes.htm)
- [微软 .NET Framework 4.8 下载](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
- [微软运行库部署和返回码](https://learn.microsoft.com/en-us/dotnet/framework/deployment/deployment-guide-for-developers)
