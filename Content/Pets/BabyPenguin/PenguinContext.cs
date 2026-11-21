using Microsoft.Xna.Framework;
using Terraria;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// PenguinContext —— 人物状态数据包（每只企鹅弹幕独享一份）
	// ============================================================================
	// 【这是什么】
	//   状态机三层结构（动作包 / 状态数据包 / 检测门）中的“数据层”。
	//   本类不含任何行为逻辑，只保存一只企鹅在当前帧的全部可变状态：
	//   引用（pet / owner / target）、模式与当前动作、各类计时器、挥砍/投掷缓存。
	//
	// 【为什么用 Context 集中管理】
	//   BabyPenguinGlobalProjectile 声明 InstancePerEntity = true，每只企鹅弹幕拥有
	//   独立的 GlobalProjectile 实例。把状态收进一个 Context 对象，动作包与检测门
	//   都只读写同一个对象，避免状态散落在不同字段导致互相不同步。
	//
	// 【谁在读写】
	//   - 调度器（BabyPenguinGlobalProjectile.PostAI）：每帧刷新 pet/owner/target，
	//     前置检查失败时调用 Reset()。
	//   - 检测门（PenguinGates）：只读本类做判断，不修改任何字段。
	//   - 动作包（PenguinActions）：读写本类执行行为与状态切换。
	//
	// 【调用时机】
	//   构造：GlobalProjectile 首次作用于该弹幕时（PostAI 第一帧）。
	//   Reset：召唤初始化 / 主人失效 / 目标丢失 / 离主人过远时。
	//
	// 【相关文档】
	//   GlobalProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
	//   随从/宠物指南:       https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide
	// ============================================================================

	// 攻击模式：远程站位丢鱼 / 近战贴边挥砍（每 5 秒由检测门触发切换）
	internal enum PenguinMode
	{
		Ranged, // 远程：保持站位丢鱼
		Melee   // 近战：贴边挥砍
	}

	// 动作包标识：状态机在第二层的 5 个具体动作
	internal enum PenguinAction
	{
		RangedApproach, // 远程接近 / 站位
		RangedWindup,   // 远程挥鱼前摇
		RangedRecover,  // 远程投掷冷却
		MeleeApproach,  // 近战追向贴边接触点
		MeleeSwing      // 近战挥砍（钉在原地）
	}

	internal sealed class PenguinContext
	{
		// ---- 引用：每帧由调度器刷新 ----
		public Projectile Pet;   // 企鹅本体弹幕（ProjectileID.Penguin，ID 112）
		public Player Owner;     // 主人
		public NPC Target;       // 当前锁定的敌人

		// ---- 模式与动作 ----
		public PenguinMode Mode = PenguinMode.Ranged;
		public PenguinAction Action = PenguinAction.RangedApproach;
		public int ModeTimer = PenguinGates.RangedModeTime; // 模式剩余帧数（到 0 触发切换）
		public int ActionTimer;                             // 当前动作的剩余帧数（前摇/冷却/挥砍用）
		public int ChaseTimer;                              // 近战接近已用帧数（追击超时阀用）

		// ---- 缓存 ----
		public int LockedTarget = -1;         // 锁定的目标 whoAmI，-1 表示尚未锁定；锁定期不每帧重选
		public Vector2 HoldPosition;          // 近战挥砍时企鹅被钉住的位置
		public int RangedSwingIndex = -1;     // 远程挥鱼弹幕的索引（-1 表示无）
		public int MeleeSwingIndex = -1;      // 近战挥砍弹幕的索引（-1 表示无）
		public float RangedBaseAngle;         // 进入前摇时锁定的丢鱼基准角度

		public PenguinContext(Projectile pet) {
			Reset(pet);
		}

		// 重置到初始状态：召唤 / 主人失效 / 目标丢失 / 超出牵引范围时调用。
		public void Reset(Projectile pet) {
			Pet = pet;
			Owner = pet != null ? Main.player[pet.owner] : null;
			Target = null;
			Mode = PenguinMode.Ranged;
			Action = PenguinAction.RangedApproach;
			ModeTimer = PenguinGates.RangedModeTime;
			ActionTimer = 0;
			ChaseTimer = 0;
			LockedTarget = -1;
			HoldPosition = Vector2.Zero;
			RangedSwingIndex = -1;
			MeleeSwingIndex = -1;
			RangedBaseAngle = 0f;
		}
	}
}
