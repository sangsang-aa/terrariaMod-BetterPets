using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyEater
{
	// ============================================================================
	// CorruptEaterShot —— 婴儿吞噬怪平时发射的腐化追踪弹
	// ============================================================================
	// 【这是什么】
	//   一个“自定义弹幕”(ModProjectile)，由 BabyEaterGlobalProjectile 在平时周期性生成。
	//
	// 【外观】
	//   通过 Texture 属性复用原版 ProjectileID.TinyEater 的贴图，因此无需新的 png。
	//   下列表现也刻意对齐原版 Tiny Eater：淡入、朝向、腐化尘拖尾、帧动画节奏。
	//   参考: https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	//
	// 【行为】
	//   朝最近的敌人轻微追踪，命中一个目标后消失（penetrate = 1）。
	//   提示：如需换外观，只要把 Texture 指向别的原版弹幕贴图即可。
	//
	// 【涉及的原版 ID】
	//   ProjectileID.TinyEater —— 复用它的贴图、帧数与拖尾尘土（Dust 184）
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   ModProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html
	//   基础弹幕指南:     https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	// ============================================================================
	public class CorruptEaterShot : ModProjectile
	{
		private const float HomingRange = 700f;   // 索敌半径（像素）
		private const float HomingSpeed = 14f;    // 追踪目标速度（像素/帧）
		private const float HomingLerp = 0.08f;   // 转向平滑系数：越小转向越慢
		// 原版 Tiny Eater 拖尾使用的尘土 ID（腐化尘土）
		private const int CorruptDust = 184;

		// 复用原版 ProjectileID.TinyEater 的贴图，无需新 png
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TinyEater;

		// SetStaticDefaults：内容注册阶段调用一次（不能访问具体弹幕实例）。
		// 沿用原版 TinyEater 的帧数，并登记为 MinionShot 以吃到鞭子的召唤标记加成。
		public override void SetStaticDefaults() {
			Main.projFrames[Type] = Main.projFrames[ProjectileID.TinyEater]; // 沿用原版帧数
			ProjectileID.Sets.MinionShot[Type] = true; // 视为随从射击，可吃到鞭子标记加成
			ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 1f;
		}

		// SetDefaults：弹幕创建时调用，用来覆盖默认属性。
		// aiStyle=0 表示不跑任何原版 AI，全部逻辑在下面的 AI() 里实现。
		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = 0; // 0 = 不使用原版 AI，改为在 AI() 里自定义
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1; // 命中一个目标后消失
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 180; // 未命中时的存在时间（帧）
			Projectile.light = 0.3f;
			Projectile.alpha = 255; // 初始全透明，随后在 AI() 里淡入（参考原版 Tiny Eater）
		}

		// AI：弹幕每帧调用（客户端与服务器都会执行），负责追踪与视觉表现。
		// 原版默认：本弹幕 aiStyle=0，没有内置 AI，所有行为都在这里实现。
		public override void AI() {
			NPC target = FindNearestTarget();
			if (target != null) {
				Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * HomingSpeed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, HomingLerp);
			}

			// 朝向与原版“腐化灾兵”撞击产生的 Tiny Eater 一致：
			// rotation = velocity.ToRotation() - PiOver2（贴图头朝向与飞行方向差 90°）
			Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

			// 淡入（参考原版 Tiny Eater：alpha 从 255 递减到 0）
			if (Projectile.alpha > 0) {
				Projectile.alpha -= 50;
				if (Projectile.alpha < 0) {
					Projectile.alpha = 0;
				}
			}

			// 腐化拖尾（参考原版 Tiny Eater 的尘土）
			SpawnTrail();

			Animate();
		}

		// CanCutTiles：false = 不会割草/砸罐。
		public override bool? CanCutTiles() {
			return false;
		}

		// 原版 Tiny Eater 的拖尾：沿速度方向等距放 3 个静止的腐化尘土。
		// 做法是先用 NewDust 申请一个尘土槽位，再手动改写它的位置/速度/大小。
		private void SpawnTrail() {
			for (int i = 0; i < 3; i++) {
				float offsetX = Projectile.velocity.X / 3f * i;
				float offsetY = Projectile.velocity.Y / 3f * i;
				int index = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, CorruptDust, 0f, 0f, 0, default, 1f);
				if (index >= Main.maxDust) {
					continue; // 尘土数组已满，跳过
				}

				Main.dust[index].position = Projectile.Center - new Vector2(offsetX, offsetY);
				Main.dust[index].velocity *= 0f;
				Main.dust[index].scale = 0.5f;
			}
		}

		// 逐帧播放动画：与原版 Tiny Eater 相同，每 6 帧切一帧。
		private void Animate() {
			int frames = Main.projFrames[Type];
			if (frames <= 1) {
				return;
			}

			// 与原版 Tiny Eater 相同：每 6 帧切一帧
			if (++Projectile.frameCounter >= 6) {
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
