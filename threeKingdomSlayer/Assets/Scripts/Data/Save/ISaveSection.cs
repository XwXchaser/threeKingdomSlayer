using System.Collections.Generic;

/// <summary>
/// **存档分片契约**（2026-10-23，存档分片第一步）。
///
/// 背景：存档模型 <see cref="SaveData"/> 是一个「什么都往里塞」的大对象，
/// 迁移逻辑也堆在模型自己的 <c>MigrateIfNeeded</c> 里 —— 加一个模块就要改中心模型 + 改中心服务 ✅。
///
/// 分片的做法：**存档格式完全不动** ✅（旧档照读 ✅），
/// 只是把「这一片数据归谁管」显式化 —— 每片自己负责迁移 ✅，读写入口也放在片上 ✅。
/// 以后要改某一片的存档结构，只动那一片 ✅，不用碰别的模块 ✅。
///
/// 约束（务必遵守）：
///   • 分片只依赖 <see cref="SaveData"/>（纯数据）✅，不碰 Unity 场景 / 单例 ✅；
///   • 分片的迁移必须**幂等** ✅（读档时每次都会跑一遍 ✅）；
///   • 分片不决定 JSON 字段名 ✅ —— 字段名属于 <see cref="SaveData"/> ✅，改了会毁掉旧档 ✅。
/// </summary>
public interface ISaveSection
{
    /// <summary>分片 id（诊断 / 日志用）。</summary>
    string Id { get; }

    /// <summary>读档后调用：把这一片的数据补齐 / 迁移（必须幂等）。</summary>
    void Migrate(SaveData data);
}

/// <summary>
/// 存档分片登记表：所有分片在这里登记，<c>SaveManager</c> 只负责「读 / 写 / 跑迁移」✅。
/// 新增一片 = 加一个实现 + 在这里登记 ✅（不用改中心模型 ✅）。
/// </summary>
public static class SaveSections
{
    private static readonly ISaveSection[] AllSections =
    {
        new PropSaveSection(),
        new TalentSaveSection(),
        new TutorialSaveSection(),
        new ProgressSaveSection(),
        new RunSnapshotSaveSection(),
        new SupplySaveSection(),
    };

    /// <summary>全部已登记的分片（只读）。</summary>
    public static IReadOnlyList<ISaveSection> All => AllSections;

    /// <summary>跑一遍所有分片的迁移（读档后调用，幂等 ✅）。</summary>
    public static void MigrateAll(SaveData data)
    {
        if (data == null) return;
        for (int i = 0; i < AllSections.Length; i++)
        {
            AllSections[i].Migrate(data);
        }
    }
}
