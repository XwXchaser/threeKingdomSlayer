using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 攻击命中帧校验器：逐敌人/逐步骤校验 AttackStep.hitFrames 的声明，并输出推导出的时序。
///
/// 输出内容：攻击 clip、命中帧 → 关键帧序号/时刻/归一化值、前冲窗口、预警窗口、收招时长。
/// 告警内容：精灵名找不到、keyIndex 与 spriteName 不一致、spawnDuration 与命中帧时刻偏差、
///          没有任何 resolve=true 的命中帧、多段连招（整段时长无法从单个 clip 推导）。
///
/// 用法：Tools/三国杀戮/校验攻击命中帧
/// 说明：运行时出伤按「屏幕上显示的精灵名」匹配，本校验器只做静态推导与偏差提示，不修改任何数据。
/// </summary>
public static class AttackHitFrameValidator
{
    private const float SpawnTolerance = 0.05f;

    [MenuItem("Tools/三国杀戮/校验攻击命中帧")]
    public static void Validate()
    {
        var sb = new StringBuilder();
        var controllers = new List<RuntimeAnimatorController>();
        int warningCount = 0;
        int stepCount = 0;

        foreach (var path in EnemyPrefabPaths())
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null) continue;

            var animator = go.GetComponentInChildren<Animator>(true);
            var controller = animator != null ? animator.runtimeAnimatorController : null;
            if (controller != null && !controllers.Contains(controller))
                controllers.Add(controller);

            sb.AppendLine();
            sb.AppendLine($"== {System.IO.Path.GetFileName(path)} (enemyId={enemy.enemyId}, isRanged={enemy.isRanged}) ==");
            ValidateSequence(enemy.attackSequence, controller, controllers, sb, ref warningCount, ref stepCount);

            var cavalry = go.GetComponent<CavalryEnemy>();
            if (cavalry != null)
                ValidateCavalryStrike(cavalry, AttackHitFrameResolver.ResolveClipForTrigger(controller, "Attack"),
                    sb, ref warningCount, ref stepCount);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:BossPhaseData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var phase = AssetDatabase.LoadAssetAtPath<BossPhaseData>(path);
            if (phase == null) continue;

            sb.AppendLine();
            sb.AppendLine($"== BossPhaseData {System.IO.Path.GetFileName(path)} ==");
            ValidateSequence(phase.attackSequence, null, controllers, sb, ref warningCount, ref stepCount);
        }

        sb.AppendLine();
        sb.AppendLine($"[命中帧校验] 步骤 {stepCount} 个，警告 {warningCount} 条（运行时按精灵名匹配，未声明命中帧的步骤沿用 spawnDuration 出伤）");
        if (warningCount > 0)
            Debug.LogWarning(sb.ToString());
        else
            Debug.Log(sb.ToString());
    }

    private static void ValidateSequence(List<AttackStep> sequence, RuntimeAnimatorController ownController,
        List<RuntimeAnimatorController> allControllers, StringBuilder sb, ref int warningCount, ref int stepCount)
    {
        if (sequence == null || sequence.Count == 0)
        {
            sb.AppendLine("   (attackSequence 为空)");
            return;
        }

        for (int s = 0; s < sequence.Count; s++)
        {
            var step = sequence[s];
            stepCount++;

            if (step.hitFrames == null || step.hitFrames.Count == 0)
            {
                sb.AppendLine($"   step{s}{(step.isCAttack ? " [C技]" : "")}: 未声明命中帧 → 沿用 spawnDuration={step.spawnDuration:F3}s 出伤，" +
                              $"预警窗口 {Mathf.Max(0f, step.spawnDuration - 0.3f):F3}–{step.spawnDuration:F3}s");
                continue;
            }

            string trigger = !string.IsNullOrEmpty(step.animationTrigger)
                ? step.animationTrigger
                : (step.isCAttack ? "CAttack" : "Attack");
            AnimationClip clip = ResolveClip(ownController, allControllers, trigger);
            string clipLabel = clip != null ? $"{clip.name}({clip.length:F3}s)" : "未找到攻击 clip";

            sb.AppendLine($"   step{s}{(step.isCAttack ? " [C技]" : "")}: clip={clipLabel} spawn={step.spawnDuration:F3}s " +
                          $"命中帧 {step.hitFrames.Count} 条 → 预警窗口 {Mathf.Max(0f, step.spawnDuration - 0.3f):F3}–{step.spawnDuration:F3}s");

            if (clip == null)
            {
                warningCount++;
                sb.AppendLine("      [WARN] 解析不到攻击 clip（检查 trigger 名与控制器状态接线）");
                continue;
            }

            var keys = GetSpriteKeys(clip);
            if (keys == null || keys.Length == 0)
            {
                warningCount++;
                sb.AppendLine("      [WARN] clip 没有精灵关键帧");
                continue;
            }

            float firstResolveTime = -1f;
            int resolveCount = 0;
            for (int i = 0; i < step.hitFrames.Count; i++)
            {
                var frame = step.hitFrames[i];
                if (string.IsNullOrEmpty(frame.spriteName))
                {
                    warningCount++;
                    sb.AppendLine($"      命中帧[{i}] [WARN] 未填 spriteName（keyIndex={frame.keyIndex}）；运行时按精灵名匹配，请回填");
                    continue;
                }

                int keyIndex = -1;
                if (frame.spriteName != null)
                {
                    for (int k = 0; k < keys.Length; k++)
                        if (keys[k].value != null && keys[k].value.name == frame.spriteName) { keyIndex = k; break; }
                    if (keyIndex < 0)
                        for (int k = 0; k < keys.Length; k++)
                            if (keys[k].value != null && keys[k].value.name.EndsWith(frame.spriteName)) { keyIndex = k; break; }
                }

                if (keyIndex < 0)
                {
                    warningCount++;
                    sb.AppendLine($"      命中帧[{i}] sprite={frame.spriteName} [WARN] 在该 clip 里找不到这个精灵");
                    continue;
                }

                float time = keys[keyIndex].time;
                float norm = clip.length > 0f ? time / clip.length : 0f;
                string spriteAtKey = keys[keyIndex].value != null ? keys[keyIndex].value.name : "(null)";
                sb.AppendLine($"      命中帧[{i}] sprite={frame.spriteName} → 键 {keyIndex} t={time:F3}s norm={norm:F3} resolve={frame.resolve}");

                if (frame.keyIndex >= 0 && frame.keyIndex != keyIndex)
                {
                    warningCount++;
                    sb.AppendLine($"         [WARN] keyIndex={frame.keyIndex} 与 spriteName 解析出的键 {keyIndex} 不一致");
                }
                if (frame.resolve)
                {
                    resolveCount++;
                    if (firstResolveTime < 0f) firstResolveTime = time;
                }
            }

            if (resolveCount == 0)
            {
                warningCount++;
                sb.AppendLine("      [WARN] 没有任何 resolve=true 的命中帧 → 本步不会结算伤害（只会作为对齐标记）");
            }
            else if (Mathf.Abs(firstResolveTime - step.spawnDuration) > SpawnTolerance)
            {
                sb.AppendLine($"      [INFO] spawnDuration={step.spawnDuration:F3}s 与首个命中帧 {firstResolveTime:F3}s 偏差 " +
                              $"{Mathf.Abs(firstResolveTime - step.spawnDuration):F3}s：已声明命中帧时运行时以命中帧时间为准，spawnDuration 仅保留兼容/预冲备用值");
            }

            if (clip.length > step.spawnDuration)
                sb.AppendLine($"      推导：前冲 0–{firstResolveTime:F3}s，命中 {firstResolveTime:F3}s，" +
                              $"收招 {firstResolveTime:F3}–{clip.length:F3}s（命中帧步骤以命中帧为唯一时序基准）");

            if (AttackHitFrameResolver.CountFamilyClips(ownController, clip) > 1)
            {
                warningCount++;
                sb.AppendLine("      [WARN] 该攻击家族有多个 clip（多段连招）：整段时长无法从单个 clip 推导，" +
                              "位移/收招时长需按段确认（103 这类）");
            }
        }
    }

    private static AnimationClip ResolveClip(RuntimeAnimatorController own, List<RuntimeAnimatorController> all, string trigger)
    {
        var clip = AttackHitFrameResolver.ResolveClipForTrigger(own, trigger);
        if (clip != null) return clip;

        foreach (var controller in all)
        {
            clip = AttackHitFrameResolver.ResolveClipForTrigger(controller, trigger);
            if (clip != null) return clip;
        }
        return null;
    }

    /// <summary>
    /// 骑兵刺击命中帧（109）：与 1011 的 hitFrames 同一口径——按精灵名声明命中帧，
    /// 这里把名字解析到 Attack clip 的关键帧，核对 hitTime 并输出前摇/伤判/收尾三段拆分。
    /// 只做静态推导与提示，不修改数据。
    /// </summary>
    private static void ValidateCavalryStrike(CavalryEnemy cavalry, AnimationClip attackClip,
        StringBuilder sb, ref int warningCount, ref int stepCount)
    {
        stepCount++;
        if (!cavalry.HasStrikeHitFrame)
        {
            warningCount++;
            sb.AppendLine("   [骑兵刺击] [WARN] 未声明命中帧精灵名 → 伤判只能落在前摇计时结束，无法保证与画面同帧");
            return;
        }

        float windup = cavalry.StrikeWindupDuration;
        float recover = cavalry.StrikeRecoverDuration;

        if (attackClip == null)
        {
            warningCount++;
            sb.AppendLine($"   [骑兵刺击] 命中帧 {cavalry.StrikeHitSpriteName} [WARN] 解析不到攻击 clip（trigger=Attack）");
            return;
        }

        var keys = GetSpriteKeys(attackClip);
        int keyIndex = -1;
        if (keys != null)
        {
            for (int k = 0; k < keys.Length; k++)
                if (keys[k].value != null && keys[k].value.name == cavalry.StrikeHitSpriteName) { keyIndex = k; break; }
            if (keyIndex < 0)
                for (int k = 0; k < keys.Length; k++)
                    if (keys[k].value != null && keys[k].value.name.EndsWith(cavalry.StrikeHitSpriteName)) { keyIndex = k; break; }
        }

        if (keyIndex < 0)
        {
            warningCount++;
            sb.AppendLine($"   [骑兵刺击] clip={attackClip.name}({attackClip.length:F3}s) [WARN] 命中帧 {cavalry.StrikeHitSpriteName} 在 clip 里找不到");
            return;
        }

        float hitTime = keys[keyIndex].time;
        float tailTime = Mathf.Max(0f, attackClip.length - hitTime);
        string tailSprite = keys[keys.Length - 1].value != null ? keys[keys.Length - 1].value.name : "null";

        sb.AppendLine($"   [骑兵刺击] clip={attackClip.name}({attackClip.length:F3}s) 命中帧 {cavalry.StrikeHitSpriteName} → key[{keyIndex}] t={hitTime:F4}s norm={hitTime / attackClip.length:F4}");
        sb.AppendLine($"      前摇 {windup:F3}s（{keyIndex + 1} 帧 / clip {hitTime:F4}s）→ 伤判 → 收尾 {recover:F3}s（{keys.Length - 1 - keyIndex} 帧 / clip {tailTime:F4}s，末帧={tailSprite}）");

        if (Mathf.Abs(cavalry.StrikeHitTime - hitTime) > 0.0005f)
        {
            warningCount++;
            sb.AppendLine($"      [WARN] strikeHitFrame.hitTime={cavalry.StrikeHitTime:F4}s 与关键帧 {hitTime:F4}s 不一致 → 前摇/收尾切分点会错位，应改为 {hitTime:F4}");
        }

        if (keyIndex == keys.Length - 1)
        {
            warningCount++;
            sb.AppendLine("      [WARN] 命中帧就是最后一帧 → 没有收尾段，Attack5..N 之后无帧可播");
        }
    }

    private static ObjectReferenceKeyframe[] GetSpriteKeys(AnimationClip clip)
    {
        if (clip == null) return null;
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        if (bindings == null || bindings.Length == 0) return null;
        return AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
    }

    private static IEnumerable<string> EnemyPrefabPaths()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/EnemyPrefabs" }))
            yield return AssetDatabase.GUIDToAssetPath(guid);
    }
}
