using System.Collections.Generic;

/// <summary>
/// 外部**状态效果提供者**（扩展点：状态不在敌人身上时的查询入口）。
///
/// 背景：状态效果目前散在三个地方，各有各的形状——
///   • 灼烧 / 疾病：状态与结算在 <c>UpgradeEffectManager</c>（玩家侧持有）；
///   • 冻结：状态在敌人自己的状态机里（<c>EnemyFreeze</c>，现已登记到状态宿主）。
/// 于是「这个敌人是不是在燃烧」只能去问具体的那一个系统，问不到别的状态。
///
/// 现在：所有状态都统一从 <see cref="EnemyStatusRegistry"/> 问——
/// 放在敌人侧的状态（冻结 / 未来的中毒等）走**状态宿主**（<see cref="IStatusEffect"/>），
/// 放在管理器侧的状态（灼烧 / 疾病）走**本接口**。
/// 新机制（例如「中毒的敌人受到额外伤害」）不需要知道状态具体存在哪。
///
/// 约定：本接口只负责**查询与展示**（是否生效 / 进度 / 层数 / 优先级）；
/// 状态的施加与结算仍由各自的所有者负责。
/// </summary>
public interface IEnemyStatusProvider
{
    /// <summary>状态 id（如 burn / disease / frozen）。</summary>
    string StatusId { get; }

    /// <summary>该敌人在此状态效果下是否生效。</summary>
    bool IsActive(Enemy enemy);

    /// <summary>进度（0~1，血条用）。</summary>
    float GetProgress(Enemy enemy);

    /// <summary>层数（无层数概念的返回 0）。</summary>
    int GetStacks(Enemy enemy);

    /// <summary>同屏多个状态时的优先级（大的排前面）。</summary>
    int GetPriority(Enemy enemy);
}

/// <summary>
/// 敌人状态效果的**注册表**（静态，唯一入口）。
///
/// 内置三个：灼烧（burn）、疾病（disease）、冻结（frozen）——
/// 行为与改造前完全一致，只是改成统一查询。
/// </summary>
public static class EnemyStatusRegistry
{
    public const string Burn = "burn";
    public const string Disease = "disease";
    public const string Frozen = "frozen";

    /// <summary>一次状态查询的结果。</summary>
    public struct Snapshot
    {
        public bool active;
        public float progress;
        public int stacks;
        public int priority;

        public static Snapshot Inactive => new Snapshot { active = false, progress = 0f, stacks = 0, priority = 0 };
    }

    /// <summary>一个生效中的状态（查询视图）。</summary>
    public struct ActiveStatus
    {
        public string statusId;
        public float progress;
        public int stacks;
        public int priority;
    }

    private static readonly List<IEnemyStatusProvider> _effects = new List<IEnemyStatusProvider>();
    private static bool _builtinsAdded;

    /// <summary>注册状态效果（扩展点）。同一 id 后注册者覆盖先注册者。</summary>
    public static void Register(IEnemyStatusProvider effect)
    {
        if (effect == null || string.IsNullOrEmpty(effect.StatusId)) return;
        EnsureBuiltins();
        for (int i = 0; i < _effects.Count; i++)
        {
            if (_effects[i].StatusId == effect.StatusId) { _effects[i] = effect; return; }
        }
        _effects.Add(effect);
    }

    /// <summary>注销状态效果。</summary>
    public static void Unregister(string statusId)
    {
        if (string.IsNullOrEmpty(statusId)) return;
        for (int i = _effects.Count - 1; i >= 0; i--)
            if (_effects[i].StatusId == statusId) _effects.RemoveAt(i);
    }

    /// <summary>查询某个状态（未注册 → 返回「未生效」）。</summary>
    public static Snapshot GetStatus(Enemy enemy, string statusId)
    {
        EnsureBuiltins();
        if (enemy == null || string.IsNullOrEmpty(statusId)) return Snapshot.Inactive;

        // ① 敌人身上的状态效果实例（宿主式：如冻结）——新状态走这条路。
        var hosted = enemy.StatusHost.Get(statusId);
        if (hosted != null)
        {
            if (!hosted.IsActive) return Snapshot.Inactive;
            return new Snapshot
            {
                active = true,
                progress = hosted.GetProgress(),
                stacks = hosted.GetStacks(),
                priority = hosted.GetPriority(),
            };
        }

        // ② 外部提供者（管理器式：如灼烧 / 疾病）。
        for (int i = 0; i < _effects.Count; i++)
        {
            var effect = _effects[i];
            if (effect == null || effect.StatusId != statusId) continue;
            if (!effect.IsActive(enemy)) return Snapshot.Inactive;
            return new Snapshot
            {
                active = true,
                progress = effect.GetProgress(enemy),
                stacks = effect.GetStacks(enemy),
                priority = effect.GetPriority(enemy),
            };
        }
        return Snapshot.Inactive;
    }

    /// <summary>该敌人当前生效的状态（按优先级从高到低）。</summary>
    public static List<ActiveStatus> GetActive(Enemy enemy)
    {
        EnsureBuiltins();
        var result = new List<ActiveStatus>();
        if (enemy == null) return result;

        // ① 敌人身上的状态效果实例（宿主式）
        var hosted = enemy.StatusHost.Instances;
        for (int i = 0; i < hosted.Count; i++)
        {
            var instance = hosted[i];
            if (instance == null || !instance.IsActive) continue;
            result.Add(new ActiveStatus
            {
                statusId = instance.StatusId,
                progress = instance.GetProgress(),
                stacks = instance.GetStacks(),
                priority = instance.GetPriority(),
            });
        }

        // ② 外部提供者（管理器式）
        for (int i = 0; i < _effects.Count; i++)
        {
            var effect = _effects[i];
            if (effect == null || !effect.IsActive(enemy)) continue;
            result.Add(new ActiveStatus
            {
                statusId = effect.StatusId,
                progress = effect.GetProgress(enemy),
                stacks = effect.GetStacks(enemy),
                priority = effect.GetPriority(enemy),
            });
        }

        result.Sort((a, b) => b.priority.CompareTo(a.priority));
        return result;
    }

    /// <summary>确保内置状态已注册（惰性：任何时候调用都可用）。</summary>
    private static void EnsureBuiltins()
    {
        if (_builtinsAdded) return;
        _builtinsAdded = true;
        Register(new DotStatusEffect(Burn, isDot: false, priority: 10));
        Register(new DotStatusEffect(Disease, isDot: true, priority: 20));
        // 冻结走敌人侧的状态宿主（EnemyFreeze 登记在 Enemy.StatusHost），不需在这里重复登记。
    }

    /// <summary>灼烧 / 疾病：状态与进度在 <c>UpgradeEffectManager</c>（玩家侧持有）。</summary>
    private sealed class DotStatusEffect : IEnemyStatusProvider
    {
        private readonly string _statusId;
        private readonly bool _isDisease;
        private readonly int _priority;

        public DotStatusEffect(string statusId, bool isDot, int priority)
        {
            _statusId = statusId;
            _isDisease = isDot;
            _priority = priority;
        }

        public string StatusId => _statusId;

        public bool IsActive(Enemy enemy)
        {
            if (enemy == null || UpgradeEffectManager.Instance == null) return false;
            var status = UpgradeEffectManager.Instance.GetDotStatus(enemy);
            return _isDisease ? status.isDiseased : status.isBurning;
        }

        public float GetProgress(Enemy enemy)
        {
            if (enemy == null || UpgradeEffectManager.Instance == null) return 0f;
            var status = UpgradeEffectManager.Instance.GetDotStatus(enemy);
            return _isDisease ? status.diseaseProgress : status.burnProgress;
        }

        public int GetStacks(Enemy enemy)
        {
            if (enemy == null || UpgradeEffectManager.Instance == null) return 0;
            if (!_isDisease) return 0;
            return UpgradeEffectManager.Instance.GetDotStatus(enemy).diseaseLayers;
        }

        public int GetPriority(Enemy enemy) => _priority;
    }
}
