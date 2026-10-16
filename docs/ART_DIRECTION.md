# 蜜柑 II：32px 逐帧重绘

本轮针对初版的轮廓、视角和动作问题重做，原生尺寸保持 32 × 32。

1.1.1 进一步修整四肢接入身体的位置：肩部和髋部采用连续的毛色色块，去掉叠加部件时形成的封闭深色接头；胸腹颜色延续进前肢，后腿内侧保留较浅的遮挡阴影。扑击前景爪的根部开放，让底下的身体和颈胸区域自然衔接。该调整覆盖奔跑、扑击、追尾、洗脸、玩球、坐姿等动作的 45 个部件。

1.1.2 小修侧面识别轮廓：鼻尖收回 2px，口鼻改短、下颊更圆，两只尖耳分开表现；奔跑背腰加入轻微曲线和收腹，运动尾巴加厚根部、增加暖色条纹和弯起的尾尖。保留四肢接缝融合与原有动作节奏，避免尖长口鼻、直背和细尾组合产生啮齿动物的观感。

## 实际运行素材

1.2.3 将默认 `face_front_open` / `sit` 作为外观基准，原始默认坐姿渲染保持逐像素一致。三分之四视角改为同样的 16px 头宽，近眼、远眼都使用与正面一致的 2px 眼簇和高光，口鼻收回为圆润的粉色鼻头；额头三道虎斑、奶油色口鼻、粉色内耳遵循同一组标记。纯侧面仍保留一只可见眼睛，但与侧坐使用同一张脸型构造；15px 高度可容纳最低的扑击压缩帧，不再靠缩小眼睛和脸颊适配画布。低头吃喝保留这组五官，只改变耳朵、脸颊和口鼻的朝向。压耳表情保留耳尖，不再变成平顶头。全部 28 个动作、152 个关键帧展开对照默认姿态，检查闭眼、转头、跑跳、前景爪遮挡及尾根接点。

1.2.2 玩球前爪拆为前景组件，分别绘制伸爪、拨球、抬爪、收爪；支撑前腿和完整胸腹保留。鸡母蹲取消平躺的白色腹带，立起胸口、抬高头位，四爪完全收入圆背身体下。全部动作补充独立尾巴周期：坐姿轻摆、卷睡尾尖微动、拖拽下垂，运动尾巴变体保留当前身体姿态和尾根接点。

1.2.1 为侧坐补绘同一 16px 头部规格的睁眼、半闭眼与闭眼表情，转头动作改用等规格的三分之四脸；奔跑与伸展姿态仍使用单独的运动侧脸。正面、三分之四与侧坐身体收窄腰腹，前爪保持落地位置。蜷睡尾根通过身体毛色融合，前景包裹的尾尖和外轮廓继续保留；鸡母蹲也去掉尾根封闭深线。

`assets/skins/mikan/skin.json` 是最终素材与动作数据。当前包含 218 个像素组件、18 个基础姿态、28 个动作、152 个配置关键帧。组件数包含表情变体、腿部前景层和左右方向镜像，不等于 218 个完全不同的动作。

- 正面、三分之四侧面、纯侧面、背面和后侧面分别定义脸部轮廓；纯侧面有一个可见眼睛和突出的口鼻，背面没有五官。
- 歪头有独立的像素轮廓、耳位和五官位置，不使用位图旋转。
- 奔跑六拍：接地、压缩、收腿、腾空、伸展、落地。每拍重绘脊线、腹线与四肢位置；近侧后腿有独立的大腿体积和脚掌。
- 扑击八拍：蓄力、压低、蹬地、上升、伸爪、接地、压缩缓冲、恢复。前爪独立前景层，避免被头部覆盖。
- 追尾使用八个朝向阶段，包含纯背面；不通过整体旋转或左右翻转同一张正脸模拟转圈。左右对称朝向可复用镜像，前、侧、后视角分别绘制。
- 坐姿、侧坐、鸡母蹲、蜷睡、侧睡、弓背受惊、低头进食、伸爪玩球、洗脸、耳朵变化、哈欠和被拎起均有对应原画或部件变体。
- 本套素材不使用 `rotation` / `sx` / `sy` 或 `spin` / `hop` 等变形器。引擎仍保留这些接口，供其他素材包选择使用。

绘制直接在最终原生像素网格上完成，没有二倍草稿抽样。`scripts/pixel-canvas.js` 提供像素画笔，`scripts/cat-art.js` 定义最终轮廓和色块，`scripts/build-animation-pack.js` 组织组件与逐帧节奏，`scripts/draw-skin.js` 为构建入口。

## 节奏

`frameDurations` 逐帧定义毫秒数，`playback` 区分 `loop` 与 `once`。扑击、炸毛、哈欠只播放一遍，之后停在末帧，等待行为系统切换。跑动帧较短，眨眼短促，观察与睡眠有较长停顿。旧包只提供 `frameMs` 时仍可播放。

导出的透明 PNG 条合并身体关键帧与尾巴组件周期的时间边界，不再以固定 10fps 重采样；格数可以多于身体原画数。`frames/manifest.json` 的第 2 版格式记录每帧时长。离线 HTML 图鉴、GIF 和实际程序使用相同的时序来源。

可检查：

- `assets/skins/mikan/motion-keyframes.png`：奔跑、扑击、追尾的所有关键帧，附每帧时长。
- `assets/skins/mikan/locomotion-preview.gif`：从左到右为奔跑、扑击、追尾的并列预览。
- `assets/skins/mikan/contact-sheet.png`：全部动作一览。
- `docs/gallery.html`：按原时序播放，支持半速观察。

## 生成参考与来源

使用内置 **imagegen** 生成了姿态参考图，保存在 `assets/reference/mikan-pose-study.png`。该图不是实际运行图集，也没有直接缩小切片后放入程序。最终 32px 像素部件在工程中另外绘制，以保证每个像素、视角、肢体遮挡和帧时序可控。没有使用 CLI / API fallback。

参考图的完整提示词如下：

```text
Use case: stylized-concept. Asset type: professional 2D pixel-art cat game animation reference sheet, for a native 32x32 Windows desktop companion. Draw one consistent small orange-and-cream tabby cat with a warm dark-brown single-pixel silhouette, amber orange fur, pale muzzle/chest/white socks, coral inner ears, expressive tiny dark eyes. Actual deliberately LOW resolution pixel art: each individual cat should plausibly fit a 32x32 pixel tile, enlarged with perfectly hard nearest-neighbor edges. Sophisticated clusters and silhouette, no smooth curves, no 3D, no vector aesthetic. Transparent background. A perfectly aligned 4-column by 3-row sheet of 12 poses with generous transparent spacing, no text and no grid lines. Row 1: front seated cat, three-quarter seated cat looking left, true LEFT PROFILE seated cat with one visible eye and protruding muzzle, REAR view seated cat with back of both ears and no face. Row 2: left-profile cat run cycle contact pose (four legs anatomically readable), compressed passing pose, fully airborne extended running pose, hind-leg push-off pose. Row 3: crouched pounce anticipation facing left, extended airborne pounce reaching both front paws left, low landing with paws extended, curled sleeping cat with tail wrapping face. Same identity and consistent proportions, lively but believable feline anatomy. Compact, clean professional indie-game sprite art. Do not reuse a front-facing head on side-facing body. Do not fake different poses by rotating the same sprite. No shadows outside each cat. No labels, watermarks, scenery, UI, gradients, anti-aliasing, or texture.
```
