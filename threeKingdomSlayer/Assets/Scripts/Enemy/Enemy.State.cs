
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Enemy 的**状态机**：状态切换与状态更新两块已按机制分文件。
///
/// 2026-10-22 从本文件拆出：<c>Enemy.StateSwitch.cs</c>（状态切换）与
/// <c>Enemy.StateUpdate.cs</c>（状态更新）。本文件只保留类声明与总览说明。
/// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// 状态枚举见 <c>EnemyState</c>；不可变约束见 design/immutable-constraints.md。
/// </summary>
public partial class Enemy : MonoBehaviour
{
}
