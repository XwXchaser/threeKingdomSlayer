using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Locus.EditorTools
{
    /// <summary>
    /// 抡斧劈砍（AxeSmash）测试装配工具。**仅编辑器**，不会进包。
    ///
    /// 背景：抡斧是主动技能（ActiveSkill_AxeSmash），它的 gestureId 为空 ⇒ 只能由
    /// <c>ActiveSkillInventory.TryActivate</c> 触发（技能栏点图标 / 三选一拿到后点）。
    /// 而它**不在 ActiveSkillPoolConfig 的任何池里** ⇒ 正常流程拿不到，只能测试装配。
    ///
    /// 用法（Play 模式）：
    ///   Tools/测试/抡斧/授予到主动技能栏   → 立刻塞进库存，技能栏出现图标，点它即可抡
    ///   Tools/测试/抡斧/立即释放一次       → 不进库存，直接演一次抡斧（绕开冷却）
    ///   Tools/测试/抡斧/写入场景初始装配   → Battle 场景的 ActiveSkillInventory 开局自动赠送
    ///   Tools/测试/抡斧/清除场景初始装配   → 还原（教学关当初就是关掉这个开关的）
    /// </summary>
    public static class AxeSkillTestRig
    {
        private const string SkillPath = "Assets/ScriptableObjects/ActiveSkills/ActiveSkill_AxeSmash.asset";
        private const string MenuRoot = "Tools/测试/抡斧/";

        [MenuItem(MenuRoot + "授予到主动技能栏", false, 10)]
        private static void GrantToInventory()
        {
            var def = Load();
            if (def == null) return;
            var inv = ActiveSkillInventory.Instance;
            if (inv == null)
            {
                Debug.LogWarning("[AxeTestRig] 找不到 ActiveSkillInventory（要进战斗后才有）");
                return;
            }
            if (!inv.UsesActiveSkills)
            {
                Debug.LogWarning("[AxeTestRig] 当前不是主动技能规则（V2），库存不会生效");
                return;
            }
            if (!inv.AcquireOrUpgrade(def, out int level))
            {
                Debug.LogWarning("[AxeTestRig] 授予失败（库存已满或规则不符）");
                return;
            }
            Debug.Log($"[AxeTestRig] 已授予「{def.displayName}」Lv{level} → 技能栏点图标即可释放");
        }

        [MenuItem(MenuRoot + "立即释放一次", false, 11)]
        private static void FireOnce()
        {
            var def = Load();
            if (def == null) return;
            var runner = ActiveSkillRunner.Instance;
            if (runner == null)
            {
                Debug.LogWarning("[AxeTestRig] 找不到 ActiveSkillRunner");
                return;
            }
            if (!runner.TryActivate(def, 1))
                Debug.LogWarning("[AxeTestRig] 释放失败（不在战斗状态 / 输入被拦 / 找不到目标）");
            else
                Debug.Log("[AxeTestRig] 已释放一次抡斧");
        }

        [MenuItem(MenuRoot + "写入场景初始装配", false, 20)]
        private static void WriteSceneAssembly()
        {
            var def = Load();
            if (def == null) return;
            var inv = Object.FindObjectOfType<ActiveSkillInventory>();
            if (inv == null)
            {
                Debug.LogWarning("[AxeTestRig] 当前场景没有 ActiveSkillInventory");
                return;
            }
            var so = new SerializedObject(inv);
            so.FindProperty("_grantDefaultSkills").boolValue = true;
            var list = so.FindProperty("_defaultSkills");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inv);
            EditorSceneManager.MarkSceneDirty(inv.gameObject.scene);
            Debug.Log("[AxeTestRig] 已写入场景初始装配：开局自动赠送抡斧劈砍（场景需保存）");
        }

        [MenuItem(MenuRoot + "清除场景初始装配", false, 21)]
        private static void ClearSceneAssembly()
        {
            var inv = Object.FindObjectOfType<ActiveSkillInventory>();
            if (inv == null) return;
            var so = new SerializedObject(inv);
            so.FindProperty("_grantDefaultSkills").boolValue = false;
            so.FindProperty("_defaultSkills").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inv);
            EditorSceneManager.MarkSceneDirty(inv.gameObject.scene);
            Debug.Log("[AxeTestRig] 已清除场景初始装配（还原成教学关当初的状态）");
        }

        private static ActiveSkillDefinition Load()
        {
            var def = AssetDatabase.LoadAssetAtPath<ActiveSkillDefinition>(SkillPath);
            if (def == null) Debug.LogError("[AxeTestRig] 找不到 " + SkillPath);
            return def;
        }
    }
}
