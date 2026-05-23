using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 经验曲线配置 - ScriptableObject
/// 定义每一级升级所需的经验值。
/// 索引0 = 0级→1级所需经验，索引1 = 1级→2级所需经验，以此类推。
/// </summary>
[CreateAssetMenu(fileName = "ExpCurveConfig", menuName = "一夫当关/经验曲线配置")]
public class ExpCurveConfig : ScriptableObject
{
    [Tooltip("每级所需经验，索引0为升至1级所需")]
    public List<int> expRequiredPerLevel;
}
