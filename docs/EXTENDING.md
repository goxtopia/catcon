# 皮肤、动画与行为扩展

所有 JSON 为 UTF-8，`schemaVersion` 当前为 1。修改后在设置点击「应用 / 重新加载」，校验失败会显示错误并保留原猫咪。先备份素材包；重跑绘制脚本会覆盖默认 `mikan/skin.json`，手工更改应保存在另一个皮肤目录或同步到绘制脚本。

## 新皮肤

复制 `assets/skins/mikan` 为 `assets/skins/my-cat`，修改 `skin.json` 中的 `id`、`name` 与 `palette`，再打开小窝，在设置选择 `my-cat`。皮肤列表在打开小窝时扫描；新增目录后重新打开小窝。

至少保留行为配置引用的动作 ID；可自由更换每个动作的姿态、组件、帧与时长。也可以同步更改行为配置，让新皮肤使用另一组 ID。默认包只有一套橘白猫，接口支持多套皮肤。

调色板的键为单字符，值为 `#RRGGBB` 或 `#AARRGGBB`。`.` 默认为透明像素 `#00000000`。每个 `sprites` 条目声明 `width`、`height` 与等宽的 `rows` 像素字符串。部件原生像素矩阵可直接逐像素编辑。

```json
"tiny_star": {
  "width": 5,
  "height": 5,
  "rows": ["..y..", ".yyy.", "yyyyy", ".yyy.", "..y.."]
}
```

## 基础姿态

`poses` 为有序组件集合。每个组件有唯一 `id`、精灵名 `sprite`、画布位置 `x/y`、绘制层级 `z`，可选 `sx/sy`、`rotation` 和 `visible`。位置在 32px 原生坐标中，旋转围绕部件中心，单位是度。缩放必须大于 0。

```json
"my_pose": [
  {"id":"body","sprite":"torso_sit_0","x":0,"y":0,"z":1},
  {"id":"head","sprite":"face_front_open","x":8,"y":6,"z":4},
  {"id":"effect","sprite":"tiny_star","x":24,"y":3,"z":5}
]
```

## 新动画

在 `clips` 中添加条目。`frames` 只覆盖指定组件的指定属性，每帧以原始姿态开始，**不会继承上一帧覆盖值**。避免动画数据被播放过程污染。

```json
"curious_star": {
  "label":"这是什么呀",
  "pose":"my_pose",
  "seconds":8,
  "frameMs":400,
  "frameDurations":[900,700,100],
  "playback":"loop",
  "frames":[
    {"head":{"sprite":"face_tilt","x":8,"y":5}},
    {"head":{"sprite":"face_tilt_other","x":8,"y":5}},
    {"head":{"sprite":"face_front_closed"}}
  ],
  "deformers":[
    {"type":"float","parts":["effect"],"amplitude":1,"period":2}
  ]
}
```

`frameDurations` 为每帧毫秒数，长度须与 `frames` 一致，每项至少 30ms；省略时回退到统一的 `frameMs`。`playback` 为 `loop` 或 `once`，默认循环；`once` 播完停在最后一帧，其总时长不得超过 `seconds`。单次动作不会在保护时间内重新弹回第一帧。

可选 `previewSeconds` 指定图鉴总览缩略图的取样时刻，范围为 0 到 `seconds`，不影响实际播放。纸箱探头等具有长蓄势的动作可取中段，避免总览只显示开头的空箱子。

组件和帧覆盖可声明 `spriteCycle:["tail_a","tail_b","tail_c","tail_b"]` 与 `cycleMs:400`。它使用动作的连续时间独立循环，身体原画停留时仍能摆尾，也能在单次动作末帧继续播放。周期帧为独立像素图，`cycleMs` 是每张图的毫秒数，须在 30～60000 内。位置、朝向和层级仍由组件及当前身体帧决定；帧覆盖指定新的 `sprite` 会先清掉基础周期，再应用该帧自己的 `spriteCycle`，防止旧姿态的尾巴覆盖新姿态。

默认包站立尾巴为独立后景组件；身体原画中已有卷尾或运动尾巴的，使用同一身体姿态的多张摆尾版本，只改变尾巴，保留身体和尾根。踩键盘的尾巴组件跟随身体的逐帧横移。睡眠和鸡母蹲只小幅摆尾尖，拖拽则使用下垂尾巴。

内置变形器接口仍保留 `breathe` / `float`、`shake`、`hop`、`spin`，`parts:["*"]` 作用于全部部件。`period` 单位秒，`amplitude` 单位原生像素。蜜柑 II 默认包不使用这些变形器；侧身、歪头、扑击和转身通过独立原画替换，避免旋转缩放破坏 32px 轮廓。示例中的 `float` 仅演示可选的装饰组件接口。

移动动作可添加 `motion:"wander"` / `"cursor"` / `"retreat"`、`speed`（屏幕像素/秒）和可选 `onArrival`（到达后的动作 ID）。`seconds` 为行为层的播放保护时间，循环方式由 `playback` 决定。

单次跃进动作使用 `motion:"leap"` 和 `leap:{"distance":26,"height":7,"takeoffFrame":2,"landingFrame":5}`。`distance`、`height` 单位为原生像素，随显示倍数换算为屏幕距离；两个帧号从 0 开始，分别指定腾空开始和接地的原画帧。动作须为 `playback:"once"`，不需要 `speed`。蓄力阶段位置不变，飞行阶段沿直线推进并叠加抛物线高度，接地后固定落点完成缓冲。

`cursor` 的 `onArrival` 指向跃进动作时，会先在目标前预留跳程，再切入跃进。近距离只使用剩余距离；落点和弧线均受工作区约束。跃进期间不重新追踪鼠标；直接从动作菜单播放跃进时，则沿当前朝向跃出指定距离。拖拽或切换动作会取消剩余轨迹。

到达后的动作可以声明 `contactAnchor:[x,y]`，表示原画中真正触碰鼠标的像素。追鼠标时会按该像素中心和当前朝向计算目标位置，避免换了一套前爪原画后仍使用旧的固定偏移。蜜柑 II 扑击的接触点为 `[3,27]`，位于接地帧的前爪上；旧包省略时保留原默认偏移。

## 让动作被自动选择

在 `config/behaviors.json` 的一个或多个情境中加入权重项：

```json
{"clip":"curious_star","weight":2}
```

权重是池内的相对值。`contextChance` 默认 0.7：常规选择有 70% 从当前情境池抽取、30% 从 `idle` 日常池抽取；未知情境使用日常池。禁用自主移动和游戏安静模式会先从两个池过滤移动动作，若某池不可用则使用另一个。情境是概率偏好，不能理解为必播专属动画。0 为只选日常池，1 为只选当前情境池；旧配置省略时默认 0.7。

动作选择会等当前动作结束，`decisionSeconds` 控制最小随机决策间隔。允许自主移动时，`ambientChance` 额外以默认 8% 的概率覆盖常规选择，从 `ambientChoices` 加权抽取散步等动作；所以此时总选择概率为情境池 64.4%、日常池 27.6%、额外散步池 8%。禁止移动或游戏安静模式下没有这个覆盖。`events` 把输入事件映射到动作，可更换受惊、反复受惊、停留、拖拽、松手、摸头和追鼠标的对应动作。不要移除必要事件键。

## 条件后续与独立概率

以下字段可省略，旧配置不增加任何规则。所有动作 ID、概率、权重、时间范围及情境均在加载时校验。

```json
"followUps": {
  "knead": {
    "chance": 0.8,
    "choices": [
      {"clip":"sleep","weight":4},
      {"clip":"sleep_stretch","weight":1}
    ]
  }
},
"independentActions": [
  {
    "clip":"break_sign",
    "intervalSeconds":[1080,1320],
    "chance":0.8,
    "contexts":["idle","coding","video","office","browsing","chat"],
    "quietGames":true
  }
]
```

`followUps` 的键是前一个动作，`chance` 是进入后续分支的概率（0～1）；命中后按 `choices` 权重抽取目标。前一个动作至少完成 `seconds` 后才进行一次尝试，情境变化会等它完成；手动交互可以中断，并取消这条接续。未命中回普通池，因此普通池仍可能选到睡觉。目标若为移动动作，会遵守自主移动开关和游戏安静模式。

`independentActions` 每个动作拥有独立的计时窗口，间隔从 `intervalSeconds` 两端之间均匀抽样，单位为秒，最短 30 秒、最长 7 天。到期只抽一次 `chance`，失败即消耗当前窗口，重新等待；成功产生一个待播放机会，等到安全决策点使用。每个动作最多有一个待播机会，到下一窗口时过期，不积压。多个动作同时待播时均匀选择一个。此概率不受普通池的条目数量或权重稀释。

`contexts` 可省略表示所有情境，指定时必须是非空的已知情境数组；`quietGames` 默认为 true，表示游戏安静模式跳过该规则。到期时情境不适用或禁止此移动动作，会消耗窗口；待播期间切入不适用状态也会丢弃机会，避免退出游戏后补播一串提醒。暂停停表；跨过多个窗口的休眠 / 卡顿最多抽取一次，不追赶旧窗口。计时使用未截断的经过时间，运动与动画仍使用限制后的时间步长。

优先级是：受保护交互 / 位移 → 已完成动作的条件后续 → 待播独立机会 → 原来的情境 / 日常池与散步覆盖。后续和独立动作也会完整保留其 `seconds`，手动操作仍可中断。播放独立动作（包括手动）会取消其待播副本，并从播放时重新计时。计时从每次启动或重载开始，暂停期间不推进，不写入设置文件。

如果希望某动作严格只受独立规则控制，需把它从所有 `contexts`、`ambientChoices` 和条件目标中移除。默认休息牌已从常规概率池移出，仍可从动作图鉴手动预览。

## 新增应用规则

`config/classifiers.json` 中的 `rules` 按顺序匹配；`process` 为不带 `.exe` 的进程名正则，`title` 和 `product` 可选，使用不区分大小写的匹配。同一规则指定的条件须全部成立；至少提供 `process` / `product` 中的一个。`product` 匹配程序产品名与文件说明，适用于进程名变化的应用；元信息无法读取时留空，不阻碍其他规则。

```json
{"category":"video","process":"^my-player$"}
```

浏览器标题的具体规则放在通用浏览器规则前。支持的情境 ID：`idle`、`coding`、`video`、`office`、`browsing`、`chat`、`gaming`。`fullscreenGameFallback` 现在只允许把全屏作为游戏的弱证据；不能单独将未知窗口判成游戏，`fullscreenExclude` 可排除这条弱证据。

## 智能兜底与置信分

`fallback.enabled` 控制数据包是否启用兜底，小窝设置还可在用户侧单独关闭。明确的应用规则优先；通用浏览器规则作为基础类别保留，给未收录网站的关键词推断留出机会。强线索冲突时返回 `idle`。

`fallback.signals` 每项包含 `category`、`group`、`pattern`、`weight`。正则匹配标题、产品信息和进程名；命中进程名与产品信息分别有 1.25 / 1.15 的来源加权，同一信号只取最强来源。每个分类下同一 `group` 的信号也只取最大值，重复词和相关品牌词不会无限叠分。

```json
{"category":"coding","group":"brand","pattern":"\\bmy-ide\\b","weight":5}
```

各组证据相加后，比较得分最高的两类。必须同时满足 `minScore`（默认 3.5）、`minConfidence`（默认 0.68）及 `minMargin`（默认 0.20）。`minMargin` 是前两类得分差相对其总分的比例。置信分随证据增加而饱和，竞争类别会降低置信分；`evidenceScale` 控制饱和速度。置信分是启发式值，不是统计概率。

例如未知编辑器标题 `main.rs — cargo compile` 有源码文件、工具链、编译行为三个独立信号，可以判为 coding；单独重复 `compile` 只计一组弱证据，仍为 idle；`Codex Discord` 这样的冲突强线索也会放弃判断。所有数据在本地匹配，标题不保存、不上传。输入长度、单次正则时间与整段关键词推断时间均有限制。

## 校验与再导出

`scripts/cat-daily-art.js` 和 `scripts/build-daily-animations.js` 分别绘制、编排日常陪伴动作，`scripts/preview-daily.ps1` 导出本组对照预览。暖杯的热气演示了装饰组件独立 `spriteCycle`，且放在头部之后，避免盖住五官；前景爪与道具必须在每帧同步更新位置。

`scripts/cat-extra-art.js` 绘制新故事组件，`scripts/build-extra-animations.js` 编排姿态、组件遮挡和逐帧节奏；由现有生成入口自动载入。新增故事优先复用统一脸部库。`scripts/preview-stories.ps1` 在构建后导出默认坐姿加四个新故事的并列 GIF，以及全部新关键帧图；输出在 `assets/skins/mikan/`，再次构建即可包含进发布包。

数据内所有引用在加载时验证；编译后的 exe 提供 `--self-test report.json`。动画条、GIF、HTML 为导出产物，手工修改运行数据后需要 `--export-art 输出根目录` 才会同步更新。运行时不依赖动画条。

导出的 `frames/manifest.json` 为 schemaVersion 2：每个动作含 `frames`、`durations`（毫秒数组）与 `playback`。导出会合并身体关键帧和组件周期的时间边界，因此 PNG 格数可以多于身体关键帧数；相邻事件间隔最短可为 10ms，源关键帧和组件周期仍至少 30ms。外部预览器须按导出的 `durations` 播放，不能固定按 10fps，也不能再用身体帧数解释 PNG 条。
