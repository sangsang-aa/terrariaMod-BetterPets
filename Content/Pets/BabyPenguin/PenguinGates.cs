using Microsoft.Xna.Framework;
using Terraria;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// PenguinGates —— 检测门（只读判断层）
	// ============================================================================
	// 【这是什么】
	//   三层结构中的“判断层”。全部是静态函数，读取 PenguinContext，只回答“是/否”
	//   （或做一个只读的几何计算），不产生任何行为、不修改企鹅位置、不生成弹幕。
	//   动作包通过组合这些门来决定“做什么”，调度器通过门来决定“是否切换模式”。
	//
	// 【门清单】
	//   存在性：OwnerAlive / TargetValid / TooFarFromOwner
	//   模式层：ModeTimeUp（5 秒到点）/ MeleeChaseTimedOut（近战追击超时阀 90 帧）
	//   走位层：AtStandOff / TooClose / TooFar / AtContact / HasLineOfSight
	//
	// 【为什么把阈值放这里】
	//   所有“多近算贴边、多远算站位、追多久算超时”都是判断口径，统一放在检测层，
	//   调参时只改这一处，动作包不重复硬编码数字。
	//
	// 【涉及的原版 API】
	//   NPC.CanBeChasedBy / Collision.CanHitLine / Projectile.OwnerMinionAttackTargetNPC
	//   https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
	// ============================================================================
	internal static class PenguinGates
	{
		// ---- 判断阈值（统一口径）----
		public const float DetectRange = 600f;             // 索敌半径（像素）
		public const float LeashRange = 1200f;             // 离主人超过该距离放弃追击
		public const float StandOffMin = 8f * 16f;         // 远程站位下限：8 格（128px），更近就后退
		public const float StandOffMax = 10f * 16f;        // 远程站位上限：10 格（160px），更远就靠近
		public const float ContactGap = 4f;                // 近战贴边时与敌人框保留的间隙
		public const float ContactArriveDist = 10f;       // 到贴边点多近算“到位”
		public const int RangedModeTime = 300;            // 远程模式时长（帧，5 秒）
		public const int MeleeModeTime = 300;             // 近战模式时长（帧，5 秒）
		public const int MeleeChaseTimeout = 90;          // 近战追击超时阀（帧，1.5 秒未贴边切回远程）

		// 主人是否存活可用
		public static bool OwnerAlive(PenguinContext ctx) {
			return ctx.Owner != null && ctx.Owner.active && !ctx.Owner.dead;
		}

		// 是否离主人太远（需要交回原版跟随 AI）
		public static bool TooFarFromOwner(PenguinContext ctx) {
			return ctx.Pet != null && ctx.Owner != null
				&& Vector2.Distance(ctx.Owner.Center, ctx.Pet.Center) > LeashRange;
		}

		// 目标本身是否仍然有效
		public static bool TargetValid(NPC target) {
			return target != null && target.active && target.CanBeChasedBy();
		}

		// 模式计时是否到点（5 秒切换的条件）
		public static bool ModeTimeUp(PenguinContext ctx) {
			return ctx.ModeTimer <= 0;
		}

		// 近战追击超时：处于近战模式、正在接近、且 90 帧仍未贴边
		// → 说明目标速度过快追不上，提前切回远程，避免 5 秒全空转。
		public static bool MeleeChaseTimedOut(PenguinContext ctx) {
			return ctx.Mode == PenguinMode.Melee
				&& ctx.Action == PenguinAction.MeleeApproach
				&& ctx.ChaseTimer >= MeleeChaseTimeout;
		}

		// 远程是否已进入站位区间（8~10 格）
		public static bool AtStandOff(PenguinContext ctx) {
			float d = Vector2.Distance(ctx.Pet.Center, ctx.Target.Center);
			return d >= StandOffMin && d <= StandOffMax;
		}

		// 远程站位过近 / 过远（用于接近动作决定靠近还是后退）
		public static bool TooClose(PenguinContext ctx) {
			return Vector2.Distance(ctx.Pet.Center, ctx.Target.Center) < StandOffMin;
		}

		public static bool TooFar(PenguinContext ctx) {
			return Vector2.Distance(ctx.Pet.Center, ctx.Target.Center) > StandOffMax;
		}

		// 视线是否通畅（隔墙不丢鱼）
		public static bool HasLineOfSight(PenguinContext ctx) {
			return Collision.CanHitLine(ctx.Pet.position, ctx.Pet.width, ctx.Pet.height,
				ctx.Target.position, ctx.Target.width, ctx.Target.height);
		}

		// 近战是否已贴边：到接触点足够近，或碰撞框已经相交
		public static bool AtContact(PenguinContext ctx, Vector2 contact) {
			return Vector2.Distance(ctx.Pet.Center, contact) <= ContactArriveDist
				|| ctx.Pet.Hitbox.Intersects(ctx.Target.Hitbox);
		}

		// 目标锁定：玩家标记目标最高优先；否则沿用锁定目标直到失效；失效才重选最近。
		// 这是判断层里唯一会写 ctx.LockedTarget 的地方（缓存选中结果，避免每帧乱跳）。
		public static NPC ResolveTarget(PenguinContext ctx) {
			Projectile pet = ctx.Pet;

			NPC marked = pet.OwnerMinionAttackTargetNPC;
			if (TargetValid(marked)) {
				ctx.LockedTarget = marked.whoAmI;
				return marked;
			}

			if (ctx.LockedTarget >= 0 && ctx.LockedTarget < Main.maxNPCs) {
				NPC locked = Main.npc[ctx.LockedTarget];
				if (TargetValid(locked)
					&& Vector2.Distance(locked.Center, pet.Center) <= DetectRange) {
					return locked;
				}
			}

			NPC result = null;
			float best = DetectRange;
			foreach (var npc in Main.ActiveNPCs) {
				if (!TargetValid(npc)) {
					continue;
				}

				float d = Vector2.Distance(npc.Center, pet.Center);
				if (d < best) {
					best = d;
					result = npc;
				}
			}

			ctx.LockedTarget = result != null ? result.whoAmI : -1;
			return result;
		}

		// 几何计算（只读）：从敌人中心朝企鹅方向作射线，算与“敌人框 + 企鹅框”
		// 边界的交点，再加 ContactGap 间隙。企鹅中心落在该点即为贴边，不重合。
		public static Vector2 GetContactPoint(PenguinContext ctx) {
			Projectile pet = ctx.Pet;
			NPC target = ctx.Target;
			Vector2 dir = (pet.Center - target.Center).SafeNormalize(-Vector2.UnitY);
			float ax = dir.X < 0f ? -dir.X : dir.X;
			float ay = dir.Y < 0f ? -dir.Y : dir.Y;

			// slab 法：射线自矩形中心出发，到边界的距离取两轴较小者
			float enemyTx = ax > 1e-6f ? target.width * 0.5f / ax : float.MaxValue;
			float enemyTy = ay > 1e-6f ? target.height * 0.5f / ay : float.MaxValue;
			float enemyReach = enemyTx < enemyTy ? enemyTx : enemyTy;

			float petTx = ax > 1e-6f ? pet.width * 0.5f / ax : float.MaxValue;
			float petTy = ay > 1e-6f ? pet.height * 0.5f / ay : float.MaxValue;
			float petReach = petTx < petTy ? petTx : petTy;

			return target.Center + dir * (enemyReach + petReach + ContactGap);
		}
	}
}
