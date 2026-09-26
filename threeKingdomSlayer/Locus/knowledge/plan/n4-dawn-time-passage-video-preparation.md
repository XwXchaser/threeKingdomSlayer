---
id: kd_14e8f168-c1da-4c9f-ae36-55714c78635b
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N4 战后夜景→天亮视频生成准备

## 剧情目的

N4 山道救援战结束后，不立即跳到下一段移动。固定在同一战场机位，表现追兵暂退、张飞与队伍短暂整队，时间从黎明前推进到清晨；天亮后重新行军。它是“战斗余波→短暂整顿→继续撤退”的时间与情绪过渡，不是安全扎营或长时间休息。

## 起点素材

- 来源视频：`Assets/RouteData/FakeStage01/Presentations/Videos/E4toN4.mp4`
- 实际解码：560×752、24fps、145帧、末帧PTS=6.000秒
- N4真实夜景起始帧：`C:/Users/steam/Pictures/gptGen/N4_dawn_first_actual.png`
- 起始帧SHA-256：`49c966e5f33c042da00f621fb7104106111467d7805cee29c46ea89f76153511`

该图片来自已验收并已部署视频的真实最后一帧，不使用N4静态战斗图替代，不拉伸、不重构机位。

## 目标表现

固定同一摄像机位置、同一焦距、同一构图、同一山道和同一战场。只做景色与光照时间变化：深蓝月夜→月光减弱→远方地平线泛灰→冷蓝晨光→柔和金色清晨。山体、道路、碎石、树木、破旗、火把、远方烟柱和战斗痕迹都必须留在原位置。

火把在晨光增强时逐渐变暗，不瞬间熄灭；薄雾缓慢流动并变淡；远处烟柱自然升腾；云层只做极轻微真实变化。不能新增人物、马匹、建筑、敌军、道路、桥梁或其他事件。

## 视频生成关键词

fixed camera / locked composition / same N4 mountain rescue battlefield / same road and mountain silhouettes / night gradually transitioning to dawn / moonlight slowly fading / horizon gradually brightening / cool blue pre-dawn light / soft warm sunrise light / stable environment geometry / torches slowly dimming / thin mist slowly dispersing / distant smoke continuing to rise / subtle cloud movement / quiet post-battle reorganization atmosphere / preserved battle traces / static landscape time passage

## Prompt草案

A single continuous six-second environmental time-passage shot in the exact same N4 mountain rescue battlefield. Use the supplied image as the actual starting frame and preserve its camera position, camera height, focal length, framing, road, rocks, trees, torn unlettered flags, distant mountain silhouettes, torches, smoke columns and battle traces exactly in place. This is not a travel shot and not a new location.

Keep the camera completely locked: no forward movement, no backward movement, no lateral pan, no tilt, no orbit, no zoom, no camera shake, no parallax travel and no reframing. The landscape geometry must remain stable. Only time, light and restrained atmosphere change.

At the beginning, preserve the deep blue moonlit night and visible moon. Over the full duration, let the moonlight gradually weaken, the horizon slowly turn gray, then let cool pre-dawn light spread across the mountains and road, followed by a restrained soft golden early-morning light. The transition must be gradual and continuous, with no sudden daylight flash. The same mountain silhouettes, road surface, rocks, trees and distant valley remain fixed in their exact positions.

Let the roadside torches slowly become less dominant as dawn arrives, fading naturally rather than vanishing. Let thin mist drift gently and dissipate, and let distant smoke continue rising with subtle natural motion. Preserve the quiet aftermath of battle: torn cloth, wheel tracks, scattered stones and damaged roadside objects remain visible. The final image should be the same N4 battlefield in early morning, clear enough to prepare for renewed marching, but still carrying the traces of the night battle.

Environmental changes only. No characters, no soldiers, no horses, no weapons, no hands, no new action, no new structures, no new route, no bridge, no camp, no banners with writing, no subtitles, no UI, no watermark, no black borders.

Strictly forbid hard cut, flash, instant day replacement, crossfade between unrelated images, morphing geometry, moving camera, digital zoom, image sliding, background scrolling, object repositioning, newly generated landmarks, disappearing mountain ranges, teleporting torches or flags, and any sudden final-frame replacement. This must read as the same battlefield waiting through the last dark hour until dawn.

## 生成规则

- 使用一张真实 `first_frame` 硬约束视频起点，不提供last_frame，不混用普通reference_image；模型仍可能让后续几何漂移，必须验收。
- 建议：seedance-2-fast、6秒、480p、adaptive、无音频、无水印。
- 此任务与道路移动视频不同：这里“不移动镜头”是剧情表达，不是失败条件；验收重点是光照连续性和场景几何稳定。
- 检查0秒、2秒、4秒、6秒：山体、道路、岩石、树木和破旗位置不得重构；亮度与色温应连续变化。
- 禁止从一张静态图做简单亮度滤镜式变化；需要有月光衰减、地平线亮起、雾散、火把相对变暗和烟柱微动等环境时间信号。
- 生成后先用户验收，再决定是否作为N4战后节点表现资产接入。N4当前战斗背景仍为N4_Battle，本轮不修改路线配置。
