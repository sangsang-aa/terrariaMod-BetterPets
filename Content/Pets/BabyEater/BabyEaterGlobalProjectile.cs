using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyEater
{
	// ============================================================================
	// BabyEaterGlobalProjectile —— 给原版“婴儿吞噬怪”宠物附加远程攻击
	// ============================================================================
	// 【这是什么】
	//   原版宠物“婴儿吞噬怪”由物品 ItemID.EatersBone（吞噬怪骨头）召唤，玩家身上挂
	//   BuffID.BabyEater，宠物本体是弹幕 ProjectileID.BabyEater。本类不改原版内容，
	//   而是在原版跟随 AI 之上“外挂”攻击行为。
	//
	// 【为什么用 GlobalProjectile】
	//   要改的是原版已有弹幕，用 GlobalProjectile + AppliesToEntity 精确锁定它，
	//   无需替换原版宠物，也不新增物品与贴图。
	//
	// 【相关文档】
	//   GlobalProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
	//   基础弹幕指南:        https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	//   随从/宠物指南:       https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide
	//
	// 【机制（写在 PostAI 里，保留原版跟随 AI）】
	//   1) 平时：每 BasicCooldown 帧朝最近的敌人发射一枚腐化弹幕 CorruptEaterShot；
	//   2) 低血：主人血量低于 LowHpThreshold 时，每 LowHpCooldown 帧发射一只会追踪
	//      并高速冲撞的“小吞噬怪” BabyEaterMinion（撞穿目标、不粘住）。
	//   只在“物主客户端”生成弹幕，避免多人游戏里重复生成。
	//
	// 【涉及的原版 ID】
	//   ItemID.EatersBone        召唤物品“吞噬怪骨头”
	//   BuffID.BabyEater         宠物增益
	//   ProjectileID.BabyEater   宠物本体弹幕
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	// ============================================================================
	public class BabyEaterGlobalProjectile : GlobalProjectile
	{
		// InstancePerEntity：默认所有弹幕共用一份实例；设为 true 后每个弹幕各持一份，
		// 才能给每只宠物单独保存计时器。
		public override bool InstancePerEntity => true;

		// 平时攻击
		private const int BasicCooldown = 45;      // 间隔（帧），45 = 0.75 秒
		private const int BasicDamage = 20;
		private const float BasicKnockback = 2f;
		// 低血量攻击
		private const int LowHpCooldown = 300;     // 间隔（帧），300 = 5 秒
		private const int LowHpDamage = 10;
		private const float LowHpKnockback = 3f;
		private const float LowHpThreshold = 0.5f; // 血量低于该比例触发

		private const float DetectRange = 700f;    // 索敌半径（像素）
		private const float ShotSpeed = 12f;       // 射出速度（像素/帧）

		// 两个计时器（单位：帧）。只在物主客户端使用，因此不需要额外网络同步。
		private int basicTimer;
		private int lowHpTimer;

		// 低血量/回血时的提示文本（本地化）
		public static LocalizedText EnragedText { get; private set; }
		public static LocalizedText CalmedText { get; private set; }
		private bool enraged;
		private int ringTimer;
		private int ringDustType;

		// SetStaticDefaults：内容注册阶段调用一次。
		public override void SetStaticDefaults() {
			EnragedText = Mod.GetLocalization($"{nameof(BabyEaterGlobalProjectile)}.Enraged");
			CalmedText = Mod.GetLocalization($"{nameof(BabyEaterGlobalProjectile)}.Calmed");
			ProjectileID.Sets.MinionTargettingFeature[ProjectileID.BabyEater] = true;
		}

		// AppliesToEntity：只对原版婴儿吞噬怪弹幕生效，其它弹幕不受影响。
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
			return entity.type == ProjectileID.BabyEater;
		}

		// PostAI：在原版 AI 每帧执行完之后调用（本地/服务器/远程客户端都会执行）。
		// 原版默认：宠物只跟随玩家、不会攻击；这里在它之后追加攻击逻辑。
		// 生成弹幕只在物主客户端执行（下面的 Main.myPlayer 判断），避免多人重复生成。
		// 参考: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
		public override void PostAI(Projectile projectile) {
			Player owner = Main.player[projectile.owner];
			if (!owner.active || owner.dead) {
				return; // 主人不在/已死则不攻击
			}

			// 低血量状态切换：进入/离开“可发动特殊攻击”的血量阈值时提示并喷一圈尘
			bool lowHp = owner.statLife < owner.statLifeMax2 * LowHpThreshold;
			if (lowHp != enraged) {
				enraged = lowHp;
				ringTimer = 45; // 让圆环持续更久（每帧补充尘土）
				// 愤怒=和风鱼同款紫色宝石尘；平静=同系列蓝色宝石尘
				ringDustType = lowHp ? DustID.GemAmethyst : DustID.GemSapphire;

				if (Main.myPlayer == projectile.owner) {
					if (lowHp) {
						Main.NewText(EnragedText.Value, (byte)190, (byte)90, (byte)255);
					}
					else {
						Main.NewText(CalmedText.Value, (byte)90, (byte)170, (byte)255);
					}
				}
			}

			// 持续喷出宝石尘，按固定角度组成一个更大的圆
			if (ringTimer > 0) {
				SpawnDustRingSegment(projectile, ringDustType);
				ringTimer--;
			}

			// 只在弹幕拥有者的客户端生成攻击，避免多人游戏里重复生成
			if (Main.myPlayer != projectile.owner) {
				return;
			}

			NPC target = FindNearestTarget(projectile, DetectRange);

			// 有目标时让宠物头部朝向目标。
			// 婴儿吞噬怪贴图天然“头朝下”（嘴/獠牙在下），故旋转需 -PiOver2（-90°）。
			if (target != null) {
				projectile.rotation = (target.Center - projectile.Center).ToRotation() - MathHelper.PiOver2;
			}

			// 平时攻击
			if (++basicTimer >= BasicCooldown && target != null) {
				basicTimer = 0;
				Spawn(projectile, ModContent.ProjectileType<CorruptEaterShot>(), BasicDamage, BasicKnockback, target);
			}

			// 低血量攻击
			if (lowHp) {
				if (++lowHpTimer >= LowHpCooldown && target != null) {
					lowHpTimer = 0;
					Spawn(projectile, ModContent.ProjectileType<BabyEaterMinion>(), LowHpDamage, LowHpKnockback, target);
				}
			}
			else {
				lowHpTimer = 0; // 回血后重新计时
			}
		}

		// 持续喷出宝石尘，按固定角度组成一个更大的圆。
		// 每帧只喷少量，持续 ringTimer 帧，整体观感比一次性爆发更久。
		private static void SpawnDustRingSegment(Projectile projectile, int dustType) {
			const int points = 24;   // 圆周上的固定采样点
			const int perFrame = 4;  // 每帧喷几个点
			const float radius = 56f; // 圆半径（像素）

			int step = (int)(Main.GameUpdateCount % points);
			for (int i = 0; i < perFrame; i++) {
				float angle = MathHelper.TwoPi * ((step + i * (points / perFrame)) % points) / points;
				Vector2 offset = angle.ToRotationVector2() * radius;
				Dust dust = Dust.NewDustPerfect(projectile.Center + offset, dustType,
					angle.ToRotationVector2() * 1.2f, 100, default, 1.3f);
				dust.noGravity = true;
			}
		}

		// 生成一枚弹幕：伤害/击退按玩家“召唤伤害/击退”加成缩放，速度指向目标。
		// 弹幕 ID 用 ModContent.ProjectileType<T>() 取得自定义弹幕的运行时类型；
		// GetSource_FromAI() 提供 IEntitySource，告诉游戏“这枚弹幕来自 AI/宠物”。
		// 参考: https://github.com/tModLoader/tModLoader/wiki/IEntitySource
		private static void Spawn(Projectile projectile, int type, int baseDamage, float knockback, NPC target) {
			Player owner = Main.player[projectile.owner];
			// 按玩家召唤伤害加成缩放弹幕伤害
			int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(baseDamage);
			// 击退也按召唤击退加成缩放
			float finalKnockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(knockback);
			Vector2 velocity = (target.Center - projectile.Center).SafeNormalize(-Vector2.UnitY) * ShotSpeed;
			Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, velocity, type, damage, finalKnockback, projectile.owner);
		}

		// 在范围内找最近的、可被追击的敌人（CanBeChasedBy 会排除友方/无敌等目标）。
		// 优先玩家用鞭子/右键标记的目标（OwnerMinionAttackTargetNPC）。
		private static NPC FindNearestTarget(Projectile projectile, float range) {
			NPC marked = projectile.OwnerMinionAttackTargetNPC;
			if (marked != null&&marked.CanBeChasedBy()) {
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
