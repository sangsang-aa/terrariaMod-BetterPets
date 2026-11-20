using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// BabyPenguinGlobalProjectile —— 改写原版“企鹅宝宝”宠物（近战挥鱼 / 远程先挥后丢）
	// ============================================================================
	// 【这是什么】
	//   原版宠物“企鹅宝宝”由物品 ItemID.Fish（ID 669）召唤，玩家挂 BuffID.BabyPenguin（ID 41），
	//   宠物本体是 ProjectileID.Penguin（ID 112）。本类不替换原版宠物，而是在它原有的跟随 AI
	//   之上追加“追踪敌人 + 攻击状态机”。
	//
	// 【模式循环（每 5 秒 = 300 帧）】
	//   Ranged：只丢鱼，不挥砍；5 秒后若宠物距目标 <= 10 格（160px），切 Melee。
	//           若在 10 格外，取消切换并重置 5 秒，继续 Ranged。
	//   Melee：只挥砍，不丢鱼；5 秒后切回 Ranged，并向“目标→企鹅”方向
	//          用 60 帧慢速后撤 10 格（160px，带物块碰撞修正）；这 1 秒计入远程 5 秒。
	//   近战命中不再反弹（后撤只发生在 Melee -> Ranged 的模式切换）。
	//
	//   Approach（追击）
	//     ├─ Melee 模式且距离 <= MeleeRange：进入 MeleeSwing
	//     └─ Ranged 模式且距离 <= DetectRange：进入 RangedWindup
	//   MeleeSwing（挥砍 16 帧，原地停住） -> MeleeRecover（回气 20 帧，原地停住） -> Approach
	//   RangedWindup（挥鱼 6 帧后丢出） -> RangedRecover（24 帧） -> Approach
	//   Retreat（60 帧慢速后撤 10 格，计入远程计时） -> Approach
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
		private enum PenguinState
		{
			Approach,      // 追击目标
			MeleeSwing,    // 近战挥砍
			MeleeRecover,  // 近战回气（原地停住）
			RangedWindup,  // 远程挥舞前摇
			RangedRecover, // 远程投掷冷却（仍继续追击）
			Retreat        // 近战转远程的慢速后撤（10 格 / 60 帧）
		}

		private enum PenguinMode
		{
			Ranged, // 远程：只丢鱼
			Melee   // 近战：只挥砍
		}

		private const int MeleeRange = 70;       // 进入近战的距离（像素，16 像素 = 1 格）
		private const int DetectRange = 600;     // 索敌半径（像素）
		private const int MeleeSwingTime = PenguinFishSwing.MeleeLifetime;   // 近战挥砍 16 帧
		private const int MeleeRecoverTime = 20; // 近战回气（帧）
		private const int RangedSwingTime = PenguinFishSwing.RangedLifetime; // 远程挥舞总长 12 帧
		private const int RangedThrowFrame = RangedSwingTime / 2;            // 鱼转到正对目标（半程）时丢出 = 6 帧
		private const int RangedRecoverTime = 24; // 远程投掷冷却（帧）

		// 模式循环（60 帧 = 1 秒）
		private const int RangedModeTime = 300; // 远程模式 5 秒（含近战转远程的慢速后撤）
		private const int MeleeModeTime = 300;  // 近战模式 5 秒
		private const float MeleeTransitionRange = 10f * 16f; // 转换区域：目标周围 10 格（160px）
		private const float RetreatDistance = 10f * 16f;      // 近战转远程慢速后撤 10 格（160px）
		private const int RetreatTime = 60;                   // 后撤用 60 帧（1 秒）完成
		private const float RetreatStep = RetreatDistance / RetreatTime; // 每帧后撤距离

		private const int MeleeDamage = 20;
		private const float MeleeKnockback = 4f;
		private const int RangedDamage = 15;
		private const float RangedKnockback = 3f;

		private const float ChaseSpeed = 7f;      // 追击速度（像素/帧）
		private const float ChaseInertia = 15f;   // 追击惯性（越大越平滑）
		private const float LeashRange = 1200f;   // 离主人超过该距离就放弃追击（像素）
		private const float ThrowGravity = 0.3f;  // 抛物线下落加速度（像素/帧²）
		private const int ThrowFlightTime = 35;   // 预计飞行时间（帧），用于反推初速度

		// 需要给每只企鹅单独保存状态
		public override bool InstancePerEntity => true;

		private PenguinState state = PenguinState.Approach;
		private int stateTimer;
		private int rangedSwingIndex = -1;
		private int meleeSwingIndex = -1;
		private float rangedBaseAngle;
		private Vector2 meleeHoldPosition; // 近战挥砍/回气时保持的位置

		// 模式与计时器
		private PenguinMode mode = PenguinMode.Ranged;
		private int modeTimer = RangedModeTime;
		private Vector2 retreatDirection;
		private int retreatTimer;

		// AppliesToEntity：弹幕初始化时调用；只认企鹅宝宝弹幕。
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
			return entity.type == ProjectileID.Penguin;
		}

		// 允许玩家用右键（召唤武器）把目标指定给企鹅，配合 OwnerMinionAttackTargetNPC 优先索敌。
		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionTargettingFeature[ProjectileID.Penguin] = true;
		}

		// SetDefaults：弹幕创建时调用；企鹅本体不直接造成接触伤害，伤害由挥砍/投掷弹幕结算。
		public override void SetDefaults(Projectile projectile) {
			projectile.damage = MeleeDamage;
			projectile.knockBack = MeleeKnockback;
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

		// PostAI：原版 AI 每帧执行完后调用（各端都会执行），在这里跑攻击状态机。
		// 参考: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
		public override void PostAI(Projectile projectile) {
			Player owner = Main.player[projectile.owner];
			if (!owner.active || owner.dead) {
				ResetState();
				return;
			}

			NPC target = FindNearestTarget(projectile, DetectRange);
			if (target == null) {
				ResetState();
				return;
			}

			// 离主人太远就放弃追击，交回原版跟随 AI
			if (Vector2.Distance(owner.Center, projectile.Center) > LeashRange) {
				ResetState();
				return;
			}

			// 面朝目标（企鹅贴图天然朝左：朝右翻转 -1，朝左不翻转 1）
			if (target.Center.X > projectile.Center.X) {
				projectile.spriteDirection = -1;
			}
			else if (target.Center.X < projectile.Center.X) {
				projectile.spriteDirection = 1;
			}

			// 飞起来攻击时使用原版“飞行”帧段（3..5）与随速度倾斜的动画
			SetFlyingAnimation(projectile);

			projectile.friendly = false;
			float distance = Vector2.Distance(projectile.Center, target.Center);

			// 每帧推进模式计时；后撤也计入远程 5 秒。
			UpdateMode(projectile, target, distance);
			if (state == PenguinState.Retreat) {
				UpdateRetreat(projectile);
				return;
			}

			switch (state) {
				case PenguinState.Approach:
					// 追击；攻击方式由当前模式决定（Ranged 丢鱼 / Melee 挥砍）。
					SteerTo(projectile, target.Center, ChaseSpeed, ChaseInertia);
					if (mode == PenguinMode.Melee) {
						if (distance <= MeleeRange) {
							EnterMeleeSwing(projectile, owner, target);
						}
					}
					else if (distance <= DetectRange && CanHitTarget(projectile, target)) {
						EnterRangedWindup(projectile, owner, target);
					}
					break;

				case PenguinState.MeleeSwing:
					// 挥砍时原地停住，避免原版跟随 AI 把企鹅往玩家方向拉。
					projectile.position = meleeHoldPosition;
					projectile.velocity = Vector2.Zero;
					if (--stateTimer <= 0) {
						state = PenguinState.MeleeRecover;
						stateTimer = MeleeRecoverTime;
					}
					break;

				case PenguinState.MeleeRecover:
					// 回气：保持同一位置，原地停住。
					projectile.position = meleeHoldPosition;
					projectile.velocity = Vector2.Zero;
					if (--stateTimer <= 0) {
						state = PenguinState.Approach;
					}
					break;

				case PenguinState.RangedWindup:
					// 远程前摇：继续追击；计时到“鱼正对目标”的那一帧就丢出
					SteerTo(projectile, target.Center, ChaseSpeed, ChaseInertia);
					if (--stateTimer <= 0) {
						if (Main.myPlayer == projectile.owner) {
							ThrowFish(projectile, owner, target);
							KillRangedSwing();
						}
						state = PenguinState.RangedRecover;
						stateTimer = RangedRecoverTime;
					}
					break;

				case PenguinState.RangedRecover:
					// 投掷冷却：仍继续追击，缩短与目标的距离
					SteerTo(projectile, target.Center, ChaseSpeed, ChaseInertia);
					if (--stateTimer <= 0) {
						state = PenguinState.Approach;
					}
					break;
			}
		}

		private void ResetState() {
			KillMeleeSwing();
			KillRangedSwing();
			state = PenguinState.Approach;
			stateTimer = 0;
			rangedSwingIndex = -1;
			meleeSwingIndex = -1;
			meleeHoldPosition = Vector2.Zero;
			mode = PenguinMode.Ranged;
			modeTimer = RangedModeTime;
			retreatTimer = 0;
		}

		// 推进 5 秒模式计时；到点时处理 Ranged <-> Melee 切换。
		private void UpdateMode(Projectile projectile, NPC target, float distance) {
			if (--modeTimer > 0) {
				return;
			}

			if (mode == PenguinMode.Ranged) {
				// 区域外：取消这次切换，重新等 5 秒继续远程。
				if (distance > MeleeTransitionRange) {
					modeTimer = RangedModeTime;
					return;
				}

				// 进入近战：清掉可能正在播放的远程前摇。
				mode = PenguinMode.Melee;
				modeTimer = MeleeModeTime;
				state = PenguinState.Approach;
				stateTimer = 0;
				KillRangedSwing();
				return;
			}

			// 近战 -> 远程：清掉挥砍，向远离目标的方向慢速后撤 10 格；
			// 后撤的 60 帧计入接下来的远程 5 秒。
			mode = PenguinMode.Ranged;
			modeTimer = RangedModeTime;
			KillMeleeSwing();
			KillRangedSwing();
			StartRetreat(projectile, target);
		}

		// 开始 60 帧慢速后撤：方向 = 目标指向企鹅，距离 = 10 格。
		private void StartRetreat(Projectile projectile, NPC target) {
			retreatDirection = (projectile.Center - target.Center).SafeNormalize(Vector2.UnitX);
			retreatTimer = RetreatTime;
			state = PenguinState.Retreat;
			stateTimer = 0;
			projectile.velocity = Vector2.Zero; // 后撤期间不受惯性影响
			projectile.netUpdate = true;
		}

		// 后撤每帧移动 RetreatStep；撞到物块时停在该帧最近的空位。
		private void UpdateRetreat(Projectile projectile) {
			MoveWithCollision(projectile, retreatDirection, RetreatStep);
			projectile.velocity = Vector2.Zero;
			projectile.netUpdate = true;

			if (--retreatTimer <= 0) {
				state = PenguinState.Approach;
				stateTimer = 0;
			}
		}

		// 直接移动 distance；只有落点卡进物块时，才沿同一方向逐步退回最近空位。
		private static void MoveWithCollision(Projectile projectile, Vector2 direction, float distance) {
			Vector2 start = projectile.position;
			projectile.position += direction * distance;

			if (!Collision.SolidCollision(projectile.position, projectile.width, projectile.height)) {
				return;
			}

			const float Step = 2f;
			for (float d = distance; d >= 0f; d -= Step) {
				Vector2 candidate = start + direction * d;
				if (!Collision.SolidCollision(candidate, projectile.width, projectile.height)) {
					projectile.position = candidate;
					return;
				}
			}

			projectile.position = start;
		}

		// 结束当前近战挥砍（模式切换时调用）。
		private void KillMeleeSwing() {
			if (meleeSwingIndex < 0 || meleeSwingIndex >= Main.maxProjectiles) {
				return;
			}

			Projectile swing = Main.projectile[meleeSwingIndex];
			if (swing.active && swing.type == ModContent.ProjectileType<PenguinFishSwing>()) {
				swing.Kill();
			}

			meleeSwingIndex = -1;
		}

		// 原版企鹅飞行时的动画：帧段 3..5 循环 + 随水平速度轻微倾斜。
		private static void SetFlyingAnimation(Projectile projectile) {
			projectile.frameCounter++;
			if (projectile.frame < 3) {
				projectile.frame = 3;
				projectile.frameCounter = 0;
			}

			if (projectile.frameCounter > 3) {
				projectile.frameCounter = 0;
				projectile.frame++;
				if (projectile.frame >= Main.projFrames[projectile.type]) {
					projectile.frame = 3;
				}
			}

			projectile.rotation = projectile.velocity.X * 0.125f;

			// 原版 Projectile.AI_026 在“飞行/返回”段每帧生成 1 颗喷射器尘：
			// 75% 用 DustID.Torch(6)，25% 用 DustID.Smoke(31)；
			// 位置在企鹅背后 12px，并随 rotation 旋转。
			int dustType = Main.rand.Next(4) == 0 ? DustID.Smoke : DustID.Torch;
			Vector2 dustOffset = new Vector2(12f * projectile.spriteDirection, 4f).RotatedBy(projectile.rotation);
			int dustIndex = Dust.NewDust(
				projectile.Center + dustOffset + new Vector2(-5f, -5f),
				10, 10, dustType,
				-projectile.velocity.X * 0.5f, projectile.velocity.Y * 0.5f,
				0, default, 1.3f);

			if (dustIndex >= 0 && dustIndex < Main.maxDust) {
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X *= 0.2f;
				dust.velocity.Y = dust.velocity.Y * 0.2f - 0.2f;
				dust.noGravity = true;
			}
		}

		// 进入近战：生成完整 16 帧的挥砍弹幕。
		private void EnterMeleeSwing(Projectile pet, Player owner, NPC target) {
			state = PenguinState.MeleeSwing;
			stateTimer = MeleeSwingTime;
			meleeHoldPosition = pet.position;

			if (Main.myPlayer == pet.owner) {
				meleeSwingIndex = SpawnSwing(pet, owner, target, MeleeSwingTime);
			}
		}

		// 进入远程前摇：生成 12 帧的挥鱼动画，但只等 6 帧（鱼转到正对目标）就丢出。
		private void EnterRangedWindup(Projectile pet, Player owner, NPC target) {
			state = PenguinState.RangedWindup;
			stateTimer = RangedThrowFrame;
			rangedBaseAngle = (target.Center - pet.Center).ToRotation();

			if (Main.myPlayer == pet.owner) {
				rangedSwingIndex = SpawnSwing(pet, owner, target, RangedSwingTime);
			}
		}

		// 生成挥砍/前摇弹幕；duration 决定完整动画长度（近战 16、远程 12）。
		private int SpawnSwing(Projectile pet, Player owner, NPC target, int duration) {
			int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(MeleeDamage);
			float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(MeleeKnockback);
			float baseAngle = (target.Center - pet.Center).ToRotation();

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

		// 投掷：从“鱼当前所在位置”（圆心前方 OrbitRadius）朝目标丢出抛物线鱼。
		private void ThrowFish(Projectile pet, Player owner, NPC target) {
			int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(RangedDamage);
			float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(RangedKnockback);

			Vector2 baseDirection = rangedBaseAngle.ToRotationVector2();
			Vector2 start = pet.Center + baseDirection * PenguinFishSwing.OrbitRadius;
			Vector2 velocity = CalculateLob(start, target.Center, ThrowGravity, ThrowFlightTime);

			Projectile.NewProjectile(pet.GetSource_FromAI(), start, velocity,
				ModContent.ProjectileType<ThrownFish>(), damage, knockback, pet.owner);
		}

		// 结束远程前摇的挥砍弹幕（不播完整 12 帧）。
		private void KillRangedSwing() {
			if (rangedSwingIndex < 0 || rangedSwingIndex >= Main.maxProjectiles) {
				return;
			}

			Projectile swing = Main.projectile[rangedSwingIndex];
			if (swing.active && swing.type == ModContent.ProjectileType<PenguinFishSwing>()) {
				swing.Kill();
			}

			rangedSwingIndex = -1;
		}

		// 朝目标平滑加速（惯性越大越平滑，越小越跟手）。
		private static void SteerTo(Projectile projectile, Vector2 targetCenter, float speed, float inertia) {
			Vector2 direction = (targetCenter - projectile.Center).SafeNormalize(-Vector2.UnitY) * speed;
			projectile.velocity = (projectile.velocity * (inertia - 1f) + direction) / inertia;
		}

		// 投掷前检查视线：中间有实体方块时不丢鱼（避免隔着墙丢）。
		private static bool CanHitTarget(Projectile projectile, NPC target) {
			return Collision.CanHitLine(projectile.position, projectile.width, projectile.height,
				target.position, target.width, target.height);
		}

		// 反推抛物线初速度：给定重力 g 与飞行时间 t，让弹幕在 t 帧后落到目标点。
		// 公式：x = vx*t；y = vy*t + 0.5*g*t²。
		private static Vector2 CalculateLob(Vector2 start, Vector2 end, float gravity, int time) {
			Vector2 delta = end - start;
			float vx = delta.X / time;
			float vy = (delta.Y - 0.5f * gravity * time * time) / time;
			return new Vector2(vx, vy);
		}

		// 找最近的敌人；优先玩家用鞭子/右键标记的目标（OwnerMinionAttackTargetNPC）。
		private static NPC FindNearestTarget(Projectile projectile, float range) {
			NPC marked = projectile.OwnerMinionAttackTargetNPC;
			if (marked != null) {
				return marked;
			}

			NPC result = null;
			float best = range;

			foreach (var npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy()) {
					continue;
				}

				float d = Vector2.Distance(npc.Center, projectile.Center);
				if (d < best) {
					best = d;
					result = npc;
				}
			}

			return result;
		}
	}
}
