/// <summary>
/// **内部圈数来源**（2026-11-25）——数据层契约，避免 Combat 反向引用 Flow。
///
/// 装备池的「档位概率」只在**无尽关**按内部圈数分阶段；普通关 / 教学 / V1 没有这个概念。
/// <c>EndlessRunController</c>（Flow）在开局时把自己注册进来，抽池时由 Combat 只读查询。
/// 没有注册方（非无尽关）时，抽取器按第 1 圈处理并记录一条日志，不会凭空发明档位。
/// </summary>
public interface IEndlessCircleSource
{
    /// <summary>当前内部圈数（1 起）。**不进 HUD、不计分**，只影响装备各档概率。</summary>
    int Circle { get; }
}
