using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// BabyPenguinGlobalProjectile —— 改写原版“企鹅宝宝”宠物：攻击状态机调度器
	// ============================================================================
	// 【这是什么】
	//   原版宠物“企鹅宝宝”由 ItemID.Fish（ID 669）召唤，BuffID.BabyPenguin（ID 41），
	//   本体是 ProjectileID.Penguin（ID 112）。本类不替换原版宠物，而是在它原有跟随 AI
	//   之上追加“追踪敌人 + 攻击状态机”。
	//
	// 【三层架构（本次重构）】
	//   调度器（本类）  ：每帧刷新 Context、跑模式门、调用动作包；
	//   状态数据包      ：PenguinContext，每只企鹅独享的全部可变状态；
	//   检测门          ：PenguinGates，只读判断（模式到点 / 超时 / 贴边 / 站位 / 视线…）；
	//   动作包          ：PenguinActions，5 个无状态动作负责移动、计时、生成弹幕。
	//   位置判定与模式切换彻底解耦：模式切换只看计时门，攻击触发只看位置门。
	//
	// 【模式循环】
	//   Ranged（远程）：保持 8~10 格站位丢鱼；5 秒到点切 Melee。
	//   Melee（近战） ：贴敌人边框挥砍；5 秒到点切 Ranged；若 90 帧仍未贴边
	//                  （目标太快追不上），由超时门提前切回 Ranged，避免真空期。
	//   挥砍期间目标移动超过 1 格 → 解除钉位重新贴边；挥完也回接近动作，不永久钉死。
	//
	// 【为什么用 GlobalProjectile】
	//   GlobalProjectile 会挂到所有弹幕（含原版）上；用 AppliesToEntity 精确锁定企鹅宝宝，
	//   就能保留原版游动/跟随，只追加攻击，不用新物品/新贴图。
	//
	// 【涉及的原版 ID】
	//   ItemID.Fish / BuffID.BabyPenguin / ProjectileID.Penguin
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   GlobalProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
	//   随从/宠物指南:       https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide
	//   生成来源 IEntitySource: https://github.com/tModLoader/tModLoader/wiki/IEntitySource
	// ============================================================================
	public class BabyPenguinGlobalProjectile : GlobalProjectile
	{
		// 每只企鹅弹幕独享一个状态数据包（InstancePerEntity = true 保证实例隔离）
		private PenguinContext context;

		public override bool InstancePerEntity => true;

		// AppliesToEntity：弹幕初始化时调用；只认企鹅宝宝弹幕。
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
			return entity.type == ProjectileID.Penguin;
		}

		// 允许玩家用右键（召唤武器）把目标指定给企鹅，配合 OwnerMinionAttackTargetNPC 优先索敌。
		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionTargettingFeature[ProjectileID.Penguin] = true;
		}

		// SetDefaults：企鹅本体不直接造成接触伤害，伤害由挥砍/投掷弹幕结算。
		public override void SetDefaults(Projectile projectile) {
			projectile.damage = PenguinActions.MeleeDamage;
			projectile.knockBack = PenguinActions.MeleeKnockback;
			projectile.DamageType = DamageClass.Summon;
			projectile.friendly = false;
			projectile.penetrate = -1;
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = 10;
		}

		// CanCutTiles：false = 不会割草/砸罐。
		public override bool? CanCutTiles(Projectile projectile) {
			return false;
		}

		// PostAI：原版 AI 每帧执行完后调用。这里只做三件事：
		//   ① 刷新状态数据包（含目标锁定）；② 跑模式门决定是否切换模式；③ 调用动作包执行行为。
		// 参考: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
		public override void PostAI(Projectile projectile) {
			// 首次作用时创建该企鹅的状态数据包
			context ??= new PenguinContext(projectile);

			// ① 刷新引用与目标
			context.Pet = projectile;
			context.Owner = Main.player[projectile.owner];

			// 存在性门：主人失效 / 目标无效 / 离主人过远 → 重置，交回原版跟随 AI
			if (!PenguinGates.OwnerAlive(context)
				|| PenguinGates.TooFarFromOwner(context)) {
				ResetState(projectile);
				return;
			}

			NPC target = PenguinGates.ResolveTarget(context);
			if (!PenguinGates.TargetValid(target)) {
				ResetState(projectile);
				return;
			}
			context.Target = target;

			projectile.friendly = false;

			// ② 模式层：推进计时；到点或近战追击超时 → 切换模式（位置完全不参与此判定）
			if (--context.ModeTimer <= 0 || PenguinGates.MeleeChaseTimedOut(context)) {
				if (context.Mode == PenguinMode.Ranged) {
					PenguinActions.EnterMeleeMode(context);
				}
				else {
					PenguinActions.EnterRangedMode(context);
				}
			}

			// ③ 行为层：执行当前动作（表现 / 移动 / 计时 / 攻击）
			PenguinActions.Run(context);
		}

		// 重置：清理挥砍弹幕与全部状态，回到远程模式入口。
		private void ResetState(Projectile projectile) {
			if (context != null) {
				PenguinActions.KillMeleeSwing(context);
				PenguinActions.KillRangedSwing(context);
			}
			context = new PenguinContext(projectile);
		}
	}
}
