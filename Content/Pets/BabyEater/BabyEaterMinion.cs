using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyEater
{
	// ============================================================================
	// BabyEaterMinion —— 低血量触发的“小吞噬怪”冲撞弹幕
	// ============================================================================
	// 【这是什么】
	//   这是一个“自定义弹幕”(ModProjectile)，由 BabyEaterGlobalProjectile 在主人低血量时
	//   生成。注意区别：上面那个是 Global 类（改写原版），这个是新增内容。
	//
	// 【外观】
	//   通过 Texture 属性直接复用原版 ProjectileID.BabyEater 的贴图，因此无需新的 png。
	//   参考: https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	//
	// 【行为】
	//   模仿“魔焰眼随从”的冲撞：平时追踪敌人；**接触目标后**才触发冲撞，
	//   沿当前飞行方向高速冲出去（穿过目标），而不是贴着目标持续追踪。
	//   首次命中后不再回到慢速接近阶段：每段冲撞结束会直接重新瞄准目标再次冲撞，
	//   直到 1 秒生命周期结束。
	//   接触伤害靠“局部无敌帧”每 10 帧结算一次；首次命中后开始计时 1 秒，到时消失。
	//
	// 【ai 数组约定】Projectile.ai 是原版留给弹幕存放自定义数值的 float[3] 槽位：
	//   ai[0] = 0 接近 / 1 冲撞；ai[1] = 冲撞计时；ai[2] = 是否已首次命中。
	//   由于 ai 会随弹幕一起同步，这里不需要额外写 SendExtraAI。
	//
	// 【涉及的原版 ID】
	//   ProjectileID.BabyEater —— 复用它的贴图与帧数
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   ModProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html
	//   随从/宠物指南:    https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide
	// ============================================================================
	public class BabyEaterMinion : ModProjectile
	{
		private const float HomingRange = 1500f;   // 索敌半径（像素）
		private const float ApproachSpeed = 10f;   // 接近速度（像素/帧）
		private const float ApproachInertia = 10f; // 接近惯性（越大越平滑）
		private const float DashSpeed = 20f;       // 接触目标后冲撞的爆发速度（像素/帧）
		private const int DashTime = 16;           // 冲撞持续帧数
		private const int AttackDuration = 60;     // 首次命中后持续 1 秒（60 帧）

		// 复用原版 ProjectileID.BabyEater 的贴图（"Terraria/Images/Projectile_<ID>" 是原版贴图约定路径）
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BabyEater;

		// SetStaticDefaults：内容注册阶段调用一次（不能访问具体弹幕实例）。
		// 这里沿用原版 BabyEater 的帧数，并把本弹幕登记为 MinionShot，
		// 以便吃到鞭子的召唤标记(tag)加成。
		public override void SetStaticDefaults() {
			// 沿用原版 BabyEater 的帧数，动画表现一致
			Main.projFrames[Type] = Main.projFrames[ProjectileID.BabyEater];
			ProjectileID.Sets.MinionShot[Type] = true; // 视为随从射击，可吃到鞭子标记加成
			ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 1f;
		}

		// SetDefaults：弹幕创建时调用，用来覆盖默认属性。
		// aiStyle=0 表示不跑任何原版 AI，全部逻辑在下面的 AI() 里实现。
		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.aiStyle = 0; // 0 = 不使用任何原版 AI，改为在下方 AI() 里自定义
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1; // 撞穿目标不消失（-1 = 无限穿透）
			Projectile.tileCollide = false; // 不碰地形，避免冲撞被墙挡住
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 180; // 未命中时的存在时间（帧）
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10; // 每 10 帧造成一次伤害
		}

		// AI：弹幕每帧调用（客户端与服务器都会执行），负责接近/冲撞状态机。
		// ai[0]: 0 = 接近, 1 = 冲撞；ai[1]: 冲撞计时；ai[2]: 是否已首次命中。
		// 注意：ai[] 由引擎随弹幕同步，因此这里的状态不需要额外写 SendExtraAI。
		public override void AI() {
			NPC target = FindNearestTarget();
			Vector2 direction = Vector2.Zero;
			if (target != null) {
				direction = (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY);
			}

			if (Projectile.ai[0] == 1f) {
				// 冲撞中：保持冲刺速度、不再逐帧追踪目标，避免贴着目标
				Projectile.velocity *= 0.995f;
				if (--Projectile.ai[1] <= 0f) {
					if (Projectile.ai[2] == 1f && target != null) {
						// 首次命中后不再回到接近阶段：直接重新瞄准目标再次冲撞
						Projectile.velocity = direction * DashSpeed;
						Projectile.ai[1] = DashTime;
						Projectile.netUpdate = true;
					}
					else {
						Projectile.ai[0] = 0f;
					}
				}
			}
			else if (target != null) {
				// 接近：只追踪目标；接触后由 OnHitNPC 触发冲撞
				Projectile.velocity = (Projectile.velocity * (ApproachInertia - 1f) + direction * ApproachSpeed) / ApproachInertia;
			}

			// 贴图天然头朝下，朝速度方向时需 -PiOver2（-90°）
			if (Projectile.velocity != Vector2.Zero) {
				Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
			}

			Animate();
		}

		// OnHitNPC：命中敌人时调用。首次命中后开始 1 秒倒计时；
		// 接触目标后触发一次冲撞（沿当前飞行方向冲出去），而不是粘在目标上。
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			// 首次命中后开始 1 秒计时
			if (Projectile.ai[2] == 0f) {
				Projectile.ai[2] = 1f;
				Projectile.timeLeft = AttackDuration;
			}

			// 接触到目标才开始冲撞：沿当前飞行方向冲出去（穿过目标），脱离接触
			if (Projectile.ai[0] != 1f) {
				Vector2 fallback = (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY);
				Vector2 dashDirection = Projectile.velocity.SafeNormalize(fallback);
				Projectile.velocity = dashDirection * DashSpeed;
				Projectile.ai[0] = 1f;
				Projectile.ai[1] = DashTime;
				Projectile.netUpdate = true;
			}
		}

		// CanCutTiles：false = 不会割草/砸罐。
		public override bool? CanCutTiles() {
			return false;
		}

		// 逐帧播放动画：每 5 帧切一帧，循环播放（帧数来自原版 BabyEater）。
		private void Animate() {
			int frames = Main.projFrames[Type];
			if (frames <= 1) {
				return;
			}

			if (++Projectile.frameCounter >= 5) {
				Projectile.frameCounter = 0;
				if (++Projectile.frame >= frames) {
					Projectile.frame = 0;
				}
			}
		}

		// 找范围内最近的、可被追击的敌人。
		// 优先玩家用鞭子/右键标记的目标（OwnerMinionAttackTargetNPC）。
		private NPC FindNearestTarget() {
			NPC marked = Projectile.OwnerMinionAttackTargetNPC;
			if (marked != null) {
				return marked;
			}

			NPC result = null;
			float best = HomingRange;

			foreach (var npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy()) {
					continue;
				}

				float d = Vector2.Distance(npc.Center, Projectile.Center);
				if (d < best) {
					best = d;
					result = npc;
				}
			}

			return result;
		}
	}
}
