---
id: kd_b0ffff49-c4e1-45d0-a98e-0e459b64bff9
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N4 → E11 下山与撤军讯号视频准备

## 真实起始帧

- 来源：已验收的 `Assets/RouteData/FakeStage01/Presentations/Videos/N4_night_to_dawn_v1.mp4`
- 输出：`C:/Users/steam/Pictures/gptGen/N4_dawn_to_E11_first_actual.png`
- 实测：560×752、24fps、145帧、末帧PTS=6.000秒
- SHA-256：`f01b48fb99cb885e9f145ac8e1ebfb4cdb17562d5836fed695091103123321ac`
- 该帧是新制夜→晨视频的真实尾帧，已确认作为N4战后天亮环境的起点；不是E4→N4夜景尾帧，也不是N4静态背景图。

## 路线与剧情职责

目标边：`N4 → E11`。E11是“撤军讯号”，不是平原终点。

N4战斗结束、对话和夜→晨演出完成后，张飞从已经整顿完毕的山地战场出发下山。镜头沿现有山道向前、向下，离开残破战场；在下山过程中逐渐看到山脚方向的撤退烟尘、远处旗语或号角信号，确认刘备军已经先行过桥，桥头需要张飞断后。视频末尾应停在山脚出口/下山道路的开阔入口，仍未到达平原，更不能直接出现长坂桥。

## 空间连续性

起点画面：天亮后的N4开阔山道战场，中央碎石道路向远方山谷延伸，两侧岩肩、破旗、残木和战斗痕迹，远方山脉与烟柱。

移动路径：沿中央道路真实前进并缓慢下坡；道路是同一条路，不转入新分叉。近处碎石、破木和路边残旗向下方掠过；两侧岩壁逐渐降低、道路视野逐渐放宽；中景山脚和撤退烟尘逐渐变大；远山仅产生符合前进的轻微视差，不整片背景平移。

目标位置：山脚道路出口/较宽的下山口。保留连续道路，前方可见通往更开阔地带的方向和远处撤退信号；不出现平原完整全景、长坂桥、桥面、河流、桥头战斗区或N6静态背景。

## Prompt草案

A single continuous six-second forward traveling shot from the exact final view of the supplied approved N4 dawn-after-battle frame. This is the next route segment N4 to E11, the descent and retreat-signal segment. Start from the same bright morning mountain rescue battlefield, with the same central rocky road, broken flags, roadside debris, low rock shoulders, distant mountains and rising smoke.

The camera must physically move FORWARD and gently DOWNHILL along the existing central road. Leave the N4 battlefield behind; do not keep the opening composition fixed. The road remains one continuous route with no new fork. Use clear depth-dependent parallax: foreground stones, broken wood and torn flags approach and pass out of the lower and side edges; the nearby rock shoulders slide past and gradually become lower; the road perspective advances continuously; the midground mountain-foot exit and retreat dust slowly grow larger; distant mountains shift only subtly.

During the middle of the shot, as the camera descends, reveal a restrained distant retreat signal at the lower route: a thin line of moving dust, a small far-off unlettered flag signal or a faint plume from the direction of the bridge. It must already exist beyond the road and become visible through real forward travel and changing occlusion, never pop into existence. The signal communicates that Liu Bei's army has moved ahead and the bridge route needs Zhang Fei to follow, but do not show characters or a readable flag symbol.

By the final seconds, arrive at the broad lower-road exit at the foot of the mountain. The road should widen naturally and lead onward toward an unseen plain. Keep the destination as a mountain-foot transition, not the final plain and not the bridge. Preserve warm clear morning light, rocky earth, sparse trees, smoke and restrained battle traces. The final view must still contain continuous road geometry for the next E11 to E12 segment.

No characters, soldiers, horses, hands, weapons, text, subtitles, UI, watermark, bridge, river, bridgehead, fortress, camp, new fork, sudden plain panorama or N6 battlefield. No hard cut, flash, dissolve, morph, teleport, pop-in, background replacement, static image sliding, flat 2D pan, zoom-only motion, or last-second destination replacement. The entire shot must show visible physical forward/downhill movement from the first second through the final second.

## 关键词

physical forward travel / gentle downhill descent / continuous central mountain road / leave the N4 battlefield / foreground stones pass camera / rock shoulders lower and recede / road perspective advances / mountain-foot exit / retreat dust in the distance / faint existing signal gradually revealed / warm morning light / stable mountain geography / depth-dependent parallax / one continuous route / E11 retreat signal

强禁：static shot / no movement / zoom-only / flat panning / background scrolling / hard cut / crossfade / morph / teleport / pop-in / instant plain reveal / instant bridge reveal / N6 battlefield replacement / new fork / readable text or banners / characters / horses / weapons。

## 生成与验收

- 建议使用一张真实 `first_frame`，不使用目标尾帧强制对齐；起点必须使用上面提取的N4天亮真实尾帧。
- 建议seedance-2-fast、6秒、480p、adaptive、无音频、无水印。
- 0–2秒确认已经前进并离开N4原构图；2–4秒确认下坡、岩肩降低、远方撤军讯号随空间推进显露；4–6秒确认抵达山脚出口但没有跳到平原或桥。
- 该段不能与E11→E12合并；E11负责撤军讯号与山脚出口，下一段才进入平原。不要在本段生成N6桥面。
