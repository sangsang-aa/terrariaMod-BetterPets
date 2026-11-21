using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// PenguinActions —— 动作包（行为执行层）
	// ============================================================================
	// 【这是什么】
	//   三层结构中的“行为层”。每个动作只负责两件事：① 本帧怎么动（移动/计时/生成弹幕）；
	//   ② 满足检测门时切换到下一个动作（写 ctx.Action）。全部是静态无状态函数，
	//   唯一可变的真相源是传入的 PenguinContext。
	//
	// 【5 个动作】
	//   RangedApproach  远程站位：太远靠近 / 太近后退，进站位区间且视线通畅 → Windup
	//   RangedWindup    挥鱼前摇 6 帧 → 丢鱼 → Recover
	//   RangedRecover   投掷冷却 24 帧，继续维持站位 → Approach
	//   MeleeApproach   每帧重算贴边接触点去追，追击超时门可强制切回远程 → Swing
	//   MeleeSwing      钉在贴边点挥砍 16 帧；目标移动或挥完则解除钉位 → Approach
	//
	// 【模式切换】
	//   不在动作包内做。由调度器（BabyPenguinGlobalProjectile）读检测门的 ModeTimeUp /
	//   MeleeChaseTimedOut 后，调用本类的 EnterRangedMode / EnterMeleeMode 进入对应模式入口。
	//
	// 【涉及的原版 ID】
	//   ItemID.Fish / ProjectileID.Penguin
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   IEntitySource（弹幕来源）: https://github.com/tModLoader/tModLoader/wiki/IEntitySource
	//   ModProjectile API:          https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html
	// ============================================================================
	internal static class PenguinActions
	{
		// ---- 行为参数 ----
		public const int MeleeDamage = 20;
		public const float MeleeKnockback = 4f;
		public const int RangedDamage = 15;
		public const float RangedKnockback = 3f;

		private const float ChaseSpeed = 15f;       // 追击速度（像素/帧）
		private const float ChaseInertia = 15f;    // 追击惯性（越大越平滑）
		private const float ThrowGravity = 0.3f;   // 抛物线重力（像素/帧²）
		private const int ThrowFlightTime = 35;    // 预计飞行帧数，用于反推初速度

		private const int MeleeSwingTime = PenguinFishSwing.MeleeLifetime;     // 近战挥砍 16 帧
		private const int RangedSwingTime = PenguinFishSwing.RangedLifetime;   // 远程挥鱼动画 12 帧
		private const int RangedThrowFrame = RangedSwingTime / 2;             // 前摇 6 帧（鱼正对目标时丢出）
		private const int RangedRecoverTime = 24;                             // 投掷冷却 24 帧

		// 每帧入口：先跑通用表现，再按当前动作分发。
		public static void Run(PenguinContext ctx) {
			UpdateVisuals(ctx);

			switch (ctx.Action) {
				case PenguinAction.RangedApproach:
					RangedApproach(ctx);
					break;
				case PenguinAction.RangedWindup:
					RangedWindup(ctx);
					break;
				case PenguinAction.RangedRecover:
					RangedRecover(ctx);
					break;
				case PenguinAction.MeleeApproach:
					MeleeApproach(ctx);
					break;
				case PenguinAction.MeleeSwing:
					MeleeSwing(ctx);
					break;
			}
		}

		// ---- 模式入口：由调度器在切换模式时调用 ----

		public static void EnterRangedMode(PenguinContext ctx) {
			KillMeleeSwing(ctx);
			ctx.Mode = PenguinMode.Ranged;
			ctx.ModeTimer = PenguinGates.RangedModeTime;
			EnterRangedApproach(ctx);
		}

		public static void EnterMeleeMode(PenguinContext ctx) {
			KillRangedSwing(ctx);
			ctx.Mode = PenguinMode.Melee;
			ctx.ModeTimer = PenguinGates.MeleeModeTime;
			EnterMeleeApproach(ctx);
		}

		// ---- 动作 1：远程接近 / 站位 ----
		private static void RangedApproach(PenguinContext ctx) {
			MaintainStandOff(ctx);

			// 检测门：进入站位区间 + 视线通畅，才开始挥鱼。
			if (PenguinGates.AtStandOff(ctx) && PenguinGates.HasLineOfSight(ctx)) {
				EnterRangedWindup(ctx);
			}
		}

		// ---- 动作 2：远程挥鱼前摇 ----
		private static void RangedWindup(PenguinContext ctx) {
			MaintainStandOff(ctx);
			if (--ctx.ActionTimer > 0) {
				return;
			}

			// 前摇结束：仅由主机生成投掷物，避免各端重复生成。
			if (Main.myPlayer == ctx.Pet.owner) {
				ThrowFish(ctx);
			}
			KillRangedSwing(ctx);
			EnterRangedRecover(ctx);
		}

		// ---- 动作 3：远程投掷冷却 ----
		private static void RangedRecover(PenguinContext ctx) {
			MaintainStandOff(ctx);
			if (--ctx.ActionTimer <= 0) {
				EnterRangedApproach(ctx);
			}
		}

		// ---- 动作 4：近战接近（追贴边点）----
		private static void MeleeApproach(PenguinContext ctx) {
			ctx.ChaseTimer++;

			// 每帧重算接触点：目标移动时接触点跟着变，企鹅不会钉死在旧位置。
			Vector2 contact = PenguinGates.GetContactPoint(ctx);
			SteerTo(ctx.Pet, contact, ChaseSpeed, ChaseInertia);

			if (PenguinGates.AtContact(ctx, contact)) {
				EnterMeleeSwing(ctx, contact);
			}
		}

		// ---- 动作 5：近战挥砍 ----
		private static void MeleeSwing(PenguinContext ctx) {
			// 钉住位置，避免原版跟随 AI 把企鹅往玩家方向拉。
			ctx.Pet.position = ctx.HoldPosition;
			ctx.Pet.velocity = Vector2.Zero;

			// 挥砍期间固定挥满整段（忽略目标移动）：命中造成的击退会让目标位移，
			// 若此时用 TargetMovedAway 提前转出，会与再次贴边生成的新弹幕叠加，
			// 形成“自我击退 -> 提前转出 -> 又生成弹幕”的自激，导致一次挥砍出现
			// 多个弹幕并存（多次判定 + 多次动画）。挥完后再回接近动作重新走位。
			if (--ctx.ActionTimer <= 0) {
				KillMeleeSwing(ctx); // 转出前先清掉本次挥砍弹幕，杜绝孤儿弹幕残留
				EnterMeleeApproach(ctx);
			}
		}

		// ---- 各动作的进入初始化 ----
		private static void EnterRangedApproach(PenguinContext ctx) {
			ctx.Action = PenguinAction.RangedApproach;
			ctx.ActionTimer = 0;
		}

		private static void EnterRangedWindup(PenguinContext ctx) {
			ctx.Action = PenguinAction.RangedWindup;
			ctx.ActionTimer = RangedThrowFrame;
			ctx.RangedBaseAngle = (ctx.Target.Center - ctx.Pet.Center).ToRotation();

			if (Main.myPlayer == ctx.Pet.owner) {
				ctx.RangedSwingIndex = SpawnSwing(ctx, RangedSwingTime);
			}
		}

		private static void EnterRangedRecover(PenguinContext ctx) {
			ctx.Action = PenguinAction.RangedRecover;
			ctx.ActionTimer = RangedRecoverTime;
		}

		private static void EnterMeleeApproach(PenguinContext ctx) {
			ctx.Action = PenguinAction.MeleeApproach;
			ctx.ActionTimer = 0;
			ctx.ChaseTimer = 0; // 重开追击计时（超时阀重新计时）
		}

		private static void EnterMeleeSwing(PenguinContext ctx, Vector2 contact) {
			ctx.Action = PenguinAction.MeleeSwing;
			ctx.ActionTimer = MeleeSwingTime;

			// 生成新弹幕前先清一次旧的（双保险）：即便某条异常路径留下了上一发，
			// 也能保证任意时刻最多只有一个近战挥砍弹幕，避免重复判定与动画。
			KillMeleeSwing(ctx);

			// 精确钉到贴边点（敌人框外 + 间隙），再记录钉位。
			ctx.Pet.Center = contact;
			ctx.Pet.velocity = Vector2.Zero;
			ctx.Pet.netUpdate = true;
			ctx.HoldPosition = ctx.Pet.position;

			if (Main.myPlayer == ctx.Pet.owner) {
				ctx.MeleeSwingIndex = SpawnSwing(ctx, MeleeSwingTime);
			}
		}

		// ---- 通用表现：面朝目标 + 原版飞行帧段与尾焰 ----
		private static void UpdateVisuals(PenguinContext ctx) {
			Projectile pet = ctx.Pet;
			NPC target = ctx.Target;

			// 企鹅贴图天然朝左：目标在右翻转 -1，目标在左不翻转 1。
			if (target.Center.X > pet.Center.X) {
				pet.spriteDirection = -1;
			}
			else if (target.Center.X < pet.Center.X) {
				pet.spriteDirection = 1;
			}

			pet.frameCounter++;
			if (pet.frame < 3) {
				pet.frame = 3;
				pet.frameCounter = 0;
			}
			if (pet.frameCounter > 3) {
				pet.frameCounter = 0;
				pet.frame++;
				if (pet.frame >= Main.projFrames[pet.type]) {
					pet.frame = 3;
				}
			}
			pet.rotation = pet.velocity.X * 0.125f;

			// 原版 AI_026 飞行段尾焰：75% Torch(6)、25% Smoke(31)，位置在背后 12px。
			int dustType = Main.rand.Next(4) == 0 ? DustID.Smoke : DustID.Torch;
			Vector2 dustOffset = new Vector2(12f * pet.spriteDirection, 4f).RotatedBy(pet.rotation);
			int dustIndex = Dust.NewDust(
				pet.Center + dustOffset + new Vector2(-5f, -5f),
				10, 10, dustType,
				-pet.velocity.X * 0.5f, pet.velocity.Y * 0.5f,
				0, default, 1.3f);

			if (dustIndex >= 0 && dustIndex < Main.maxDust) {
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X *= 0.2f;
				dust.velocity.Y = dust.velocity.Y * 0.2f - 0.2f;
				dust.noGravity = true;
			}
		}

		// ---- 远程站位：太远靠近、太近后退、区间内减速悬停 ----
		private static void MaintainStandOff(PenguinContext ctx) {
			Projectile pet = ctx.Pet;

			if (PenguinGates.TooFar(ctx)) {
				SteerTo(pet, ctx.Target.Center, ChaseSpeed, ChaseInertia);
			}
			else if (PenguinGates.TooClose(ctx)) {
				// 后退：把“远离目标的方向”作为目标点做平滑加速。
				Vector2 away = (pet.Center - ctx.Target.Center).SafeNormalize(-Vector2.UnitY);
				SteerTo(pet, pet.Center + away * 100f, ChaseSpeed, ChaseInertia);
			}
			else {
				pet.velocity *= 0.82f; // 进入站位区间，减速悬停
			}
		}

		// 朝目标平滑加速（惯性越大越平滑）。
		private static void SteerTo(Projectile pet, Vector2 targetCenter, float speed, float inertia) {
			Vector2 direction = (targetCenter - pet.Center).SafeNormalize(-Vector2.UnitY) * speed;
			pet.velocity = (pet.velocity * (inertia - 1f) + direction) / inertia;
		}

		// 生成挥鱼弹幕：近战挥砍(16 帧)与远程前摇(12 帧)共用。
		private static int SpawnSwing(PenguinContext ctx, int duration) {
			Projectile pet = ctx.Pet;
			int damage = (int)ctx.Owner.GetTotalDamage(DamageClass.Summon).ApplyTo(MeleeDamage);
			float knockback = ctx.Owner.GetKnockback(DamageClass.Summon).ApplyTo(MeleeKnockback);
			float baseAngle = (ctx.Target.Center - pet.Center).ToRotation();

			int index = Projectile.NewProjectile(pet.GetSource_FromAI(), pet.Center, Vector2.Zero,
				ModContent.ProjectileType<PenguinFishSwing>(), damage, knockback, pet.owner,
				pet.whoAmI, baseAngle, duration);

			if (index >= 0 && index < Main.maxProjectiles) {
				Main.projectile[index].timeLeft = duration;
				Main.projectile[index].ai[2] = duration; // 动画用 ai[2] 归一化进度
				Main.projectile[index].netUpdate = true;
			}

			return index;
		}

		// 投掷：从鱼当前所在位置（圆心前方 OrbitRadius）朝目标丢出抛物线鱼。
		private static void ThrowFish(PenguinContext ctx) {
			Projectile pet = ctx.Pet;
			int damage = (int)ctx.Owner.GetTotalDamage(DamageClass.Summon).ApplyTo(RangedDamage);
			float knockback = ctx.Owner.GetKnockback(DamageClass.Summon).ApplyTo(RangedKnockback);

			Vector2 baseDirection = ctx.RangedBaseAngle.ToRotationVector2();
			Vector2 start = pet.Center + baseDirection * PenguinFishSwing.OrbitRadius;
			Vector2 velocity = CalculateLob(start, ctx.Target.Center, ThrowGravity, ThrowFlightTime);

			Projectile.NewProjectile(pet.GetSource_FromAI(), start, velocity,
				ModContent.ProjectileType<ThrownFish>(), damage, knockback, pet.owner);
		}

		// 反推抛物线初速度：x = vx*t；y = vy*t + 0.5*g*t²。
		private static Vector2 CalculateLob(Vector2 start, Vector2 end, float gravity, int time) {
			Vector2 delta = end - start;
			float vx = delta.X / time;
			float vy = (delta.Y - 0.5f * gravity * time * time) / time;
			return new Vector2(vx, vy);
		}

		// 结束当前挥鱼弹幕（索引有效且类型匹配才 Kill）。
		public static void KillRangedSwing(PenguinContext ctx) {
			KillSwingByIndex(ctx.RangedSwingIndex);
			ctx.RangedSwingIndex = -1;
		}

		public static void KillMeleeSwing(PenguinContext ctx) {
			KillSwingByIndex(ctx.MeleeSwingIndex);
			ctx.MeleeSwingIndex = -1;
		}

		private static void KillSwingByIndex(int index) {
			if (index < 0 || index >= Main.maxProjectiles) {
				return;
			}

			Projectile swing = Main.projectile[index];
			if (swing.active && swing.type == ModContent.ProjectileType<PenguinFishSwing>()) {
				swing.Kill();
			}
		}
	}
}
