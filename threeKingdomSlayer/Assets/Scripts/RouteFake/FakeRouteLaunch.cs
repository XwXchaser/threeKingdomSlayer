/// <summary>MainMenu 时代遗留的跨场景旗标。MainMenu 已删除，待批次 2 随路线收敛一并清掉。</summary>
public static class FakeRouteLaunch
{
	/// <summary>
	/// 「续玩时从存档点节点开始」的**一次性**旗标。
	///
	/// 2026-10-27 修正（BUG：重进游戏落在上一个节点）：
	///   • 之前它是一个**永不复位**的静态开关 —— 只要有一次置位（退出关卡 / 点「重来」），
	///     之后**每一次**进关都从存档点续跑（用户反馈「重进游戏直接落在 N3/E0，关卡前段被跳过」）。
	///   • 现在改成**一次性消费**：<see cref="ConsumeStartFromCheckpoint"/> 读走即清。
	///   • 「退出关卡回落地页」**不再**置位（见 <c>StageRunFlow.GoToMainMenu</c>）——
	///     退出后重新进入 = 从**关卡开头**开始，与 <c>FakeRouteRuntime.ResolveStartNode</c> 的注释意图一致。
	/// </summary>
	public static bool StartFromCheckpoint;

	/// <summary>读走「从存档点续跑」这个标记（读一次就清掉，避免污染后续进入）。</summary>
	public static bool ConsumeStartFromCheckpoint()
	{
		bool v = StartFromCheckpoint;
		StartFromCheckpoint = false;
		return v;
	}
}
