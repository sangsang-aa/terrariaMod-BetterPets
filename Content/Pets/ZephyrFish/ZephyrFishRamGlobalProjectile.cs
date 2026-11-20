using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TutorialMod.Content.Pets.ZephyrFish
{
	// ============================================================================
	// ZephyrFishRamGlobalProjectile —— 给原版“和风鱼”宠物附加冲撞攻击
	// ============================================================================
	// 【这是什么】
	//   原版宠物“和风鱼”由物品 ItemID.ZephyrFish 召唤，玩家身上挂 BuffID.ZephyrFish，
	//   宠物本体是一个 ProjectileID.ZephyrFish 弹幕。本类不改原版内容，而是“外挂”一套
	//   冲撞攻击行为到这只宠物上。
	//
	// 【为什么用 GlobalProjectile，而不是新建 ModProjectile】
	//   GlobalProjectile 的实例会被挂到“每一个”Projectile 上（含原版弹幕）。我们用
	//   AppliesToEntity 把它限定为只对和风鱼生效，于是可以在不替换原版弹幕的前提下，
	//   改写它的默认值与每帧行为。
	//
	// 【相关文档】
	//   GlobalProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
	//   基础弹幕指南:        https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	//   随从/宠物指南:       https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide
	//
	// 【机制：一个写在 PostAI 里的四段状态机】
	//   原版游动 AI 照常运行（我们不拦截 PreAI，它默认返回 true），在它跑完之后用 PostAI
	//   覆盖速度，实现：
	//     Approach（接近） -> Windup（蓄力） -> Dash（冲刺） -> Recover（回气） -> Approach ...
	//   只有 Dash 阶段才把弹幕的 friendly 置为 true 去造成接触伤害；
	//   再配合 MinionContactDamage 返回 true，宠物才真正能“撞伤”敌人。
	//   命中目标后沿反方向弹开并转入 Recover，于是会反复发起冲刺。
	//
	// 【涉及的原版 ID】（可在 WIKI 的 Vanilla Content IDs 查询）
	//   ItemID.ZephyrFish        召唤物品“和风鱼”
	//   BuffID.ZephyrFish        宠物增益
	//   ProjectileID.ZephyrFish  宠物本体弹幕
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	// ============================================================================
	public class ZephyrFishRamGlobalProjectile : GlobalProjectile
	{
		// 冲刺循环的四个阶段（内部状态，不需要同步给游戏，只用来自主管理速度与伤害开关）
		private enum RamState
		{
			Approach, // 接近敌人，进入冲刺距离后蓄力
			Windup,   // 蓄力前摇（减速、转向目标）
			Dash,     // 高速冲撞（此时才有接触伤害）
			Recover   // 回气冷却
		}

		// 基础冲撞伤害（想改数值就改这里）。注意：宠物弹幕的伤害在生成时可能被清零，
		// 所以 PostAI 里每帧还会按主人的召唤伤害加成把它写回（见下方 ApplyTo(RamDamage)）。
		private const int RamDamage = 25;
		// 冲撞击退（越大把敌人推得越远）
		private const float RamKnockback = 6f;
		// 索敌半径（单位：像素。16 像素 = 1 格）
		private const float DetectRange = 600f;
		// 当与目标距离小于该值时，从“接近”切换到“蓄力准备冲刺”（单位：像素）
		private const float DashRange = 280f;
		// 接近阶段的速度（像素/帧）与惯性（越大转向越平滑、越小越跟手）
		private const float ApproachSpeed = 8f;
		private const float ApproachInertia = 12f;
		// 冲刺爆发速度（像素/帧）
		private const float DashSpeed = 17f;
		// 各阶段持续帧数（游戏每秒 60 帧，所以 60 帧 = 1 秒）
		private const int WindupTime = 14;
		private const int DashTime = 26;
		private const int RecoverTime = 30;
		// 每只宠物对同一敌人的命中间隔（帧），配合下面的“局部无敌帧”使用
		private const int HitCooldown = 10;

		// InstancePerEntity：默认情况下所有弹幕共用同一份 GlobalProjectile 实例；
		// 返回 true 表示“每个弹幕各有一份实例”，这样才能给每只宠物单独保存状态字段。
		public override bool InstancePerEntity => true;

		private RamState state = RamState.Approach;
		private int stateTimer;

		// AppliesToEntity：在弹幕初始化时调用，决定这份 Global 是否作用于该弹幕。
		// 这里只认原版和风鱼弹幕，避免把这套冲刺逻辑套给所有宠物/所有弹幕。
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
			return entity.type == ProjectileID.ZephyrFish;
		}

		// SetStaticDefaults：内容注册阶段调用一次（只读/写静态数据，不能访问具体实例）。
		// 这里把和风鱼登记为“随从射击(MinionShot)”，原因是原版鞭子的召唤标记(tag)伤害
		// 只作用于“随从相关”弹幕（minion / MinionShot / sentry / SentryShot）。
		// 打上标记后，和风鱼的冲撞才能吃到鞭子提供的额外伤害。
		// SummonTagDamageMultiplier 是标记伤害的缩放系数，1f 表示不额外衰减。
		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionShot[ProjectileID.ZephyrFish] = true;
			ProjectileID.Sets.SummonTagDamageMultiplier[ProjectileID.ZephyrFish] = 1f;
			ProjectileID.Sets.MinionTargettingFeature[ProjectileID.ZephyrFish] = true;
		}

		// SetDefaults：在 Projectile.SetDefaults 的末尾调用，用于覆盖这只弹幕的基础属性。
		public override void SetDefaults(Projectile projectile) {
			projectile.damage = RamDamage; // 初始值；PostAI 每帧会按主人召唤加成重算
			projectile.knockBack = RamKnockback; // 初始值；PostAI 每帧会按召唤击退加成重算
			projectile.penetrate = -1; // 撞到敌人不消失（-1 表示无限穿透）
			projectile.DamageType = DamageClass.Summon;
			projectile.friendly = false; // 待机时不开启接触伤害，避免误伤训练假人/小动物

			// 使用“局部无敌帧”：每只弹幕独立计算对同一个敌人的命中冷却，
			// 不会被其它伤害来源的无敌帧顶掉，也不会去占用别人的无敌帧。
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = HitCooldown;
		}

		// MinionContactDamage：是否允许该弹幕“靠接触”对敌人造成伤害。
		// 原版要求宠物/随从类弹幕显式返回 true 才会走接触伤害判定。
		public override bool MinionContactDamage(Projectile projectile) {
			return true;
		}

		// CanCutTiles：是否可割草/打碎罐子等。返回 false 让冲刺不会砍草、砸罐。
		public override bool? CanCutTiles(Projectile projectile) {
			return false;
		}

		// PostAI：在原版 AI 每帧执行完之后调用（无论 PreAI 返回什么都会执行）。
		// 调用端：本地客户端、服务器、远程客户端都会执行，所以速度/状态计算必须各端一致；
		// 状态切换通过 netUpdate 同步（见 EnterState）。
		// 原版默认：普通宠物只会游动/跟随、不会攻击；这里在其后追加冲刺逻辑。
		// 参考: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html
		public override void PostAI(Projectile projectile) {
			Player owner = Main.player[projectile.owner];

			// 主人死亡/离开时不攻击（宠物本体何时消失由增益负责回收）
			if (!owner.active || owner.dead) {
				projectile.friendly = false;
				ResetState(projectile);
				return;
			}

			// 宠物弹幕的伤害可能被生成流程清零，这里持续写回，确保接触伤害有效。
			// 同时用主人的“召唤伤害总加成”缩放基础值：GetTotalDamage 会把
			// 召唤师饰品/增益以及 Generic 等继承加成一起算进去，这样冲撞能吃到加成。
			projectile.damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(RamDamage);
			// 击退同样按“召唤击退”加成缩放（如甲虫/死灵卷轴等召唤师饰品对召唤物击退的提升）。
			// GetKnockback 与 GetTotalDamage 类似，返回该伤害类型的击退修正，配合 ApplyTo 使用。
			projectile.knockBack = owner.GetKnockback(DamageClass.Summon).ApplyTo(RamKnockback);

			bool foundTarget = SearchForTargets(projectile, out Vector2 targetCenter, out float distanceFromTarget);

			if (!foundTarget) {
				// 无目标：完全交回原版游动 AI，让它游回玩家身边
				projectile.friendly = false;
				ResetState(projectile);
				return;
			}

			switch (state) {
				case RamState.Approach:
					// 先普通速度贴近，进入冲刺距离后再蓄力
					projectile.friendly = false;
					SteerTo(projectile, targetCenter, ApproachSpeed, ApproachInertia);
					if (distanceFromTarget < DashRange) {
						EnterState(projectile, RamState.Windup, WindupTime);
					}
					break;

				case RamState.Windup:
					// 蓄力：减速并转向目标，制造冲刺前摇
					projectile.friendly = false;
					projectile.velocity *= 0.85f;
					FaceTarget(projectile, targetCenter);
					if (--stateTimer <= 0) {
						EnterState(projectile, RamState.Dash, DashTime);
					}
					break;

				case RamState.Dash:
					// 冲刺：速度拉满冲撞，并保持头部指向目标
					projectile.friendly = true;
					Vector2 dashDirection = GetDirection(projectile, targetCenter);
					projectile.velocity = Vector2.Lerp(projectile.velocity, dashDirection * DashSpeed, 0.2f);
					FaceTarget(projectile, targetCenter);
					SpawnDashDust(projectile);
					if (--stateTimer <= 0) {
						EnterState(projectile, RamState.Recover, RecoverTime);
					}
					break;

				case RamState.Recover:
					// 回气：不干预速度，交给原版 AI 游动，冷却结束后再次接近
					projectile.friendly = false;
					if (--stateTimer <= 0) {
						EnterState(projectile, RamState.Approach, 0);
					}
					break;
			}
		}

		// OnHitNPC：该弹幕命中敌人时调用（NPC.HitInfo 里带着本次命中的伤害、暴击、
		// 击退等信息）。原版默认只做伤害结算；这里额外让宠物朝“远离敌人并略微向上”
		// 的方向弹开，并立即转入回气，
		// 从而形成“撞一下 -> 弹开 -> 再来”的反复冲刺节奏。
		// 参考: https://github.com/tModLoader/tModLoader/wiki/Projectile-Class-Documentation
		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) {
			Vector2 away = projectile.Center - target.Center;
			if (away == Vector2.Zero) {
				away = -Vector2.UnitY;
			}
			away.Normalize();

			projectile.velocity = away * DashSpeed * 0.7f - Vector2.UnitY * 3f;
			EnterState(projectile, RamState.Recover, RecoverTime);
		}

		// SendExtraAI / ReceiveExtraAI：多人同步。我们自定义的 state / stateTimer
		// 不在原版弹幕字段里，必须自己写进网络包；改变后要设置 projectile.netUpdate = true
		// 来触发同步（见 EnterState）。发送方写、接收方读，顺序必须一致。
		// BitWriter/BitReader 用来压缩布尔等小数据；这里只有两个定长字段，
		// 直接用 BinaryWriter/BinaryReader 写入即可。
		// 参考: https://github.com/tModLoader/tModLoader/wiki/Basic-Netcode
		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) {
			binaryWriter.Write((byte)state);
			binaryWriter.Write((short)stateTimer);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) {
			state = (RamState)binaryReader.ReadByte();
			stateTimer = binaryReader.ReadInt16();
		}

		// 切换状态并标记网络同步（netUpdate = true 会让引擎把该弹幕同步给其它客户端）
		private void EnterState(Projectile projectile, RamState newState, int time) {
			state = newState;
			stateTimer = time;
			projectile.netUpdate = true;
		}

		private void ResetState(Projectile projectile) {
			if (state != RamState.Approach) {
				state = RamState.Approach;
				stateTimer = 0;
				projectile.netUpdate = true;
			}
		}

		// 朝目标方向加速（惯性越大越平滑，越小越急）。
		// 公式：(当前速度 * (惯性-1) + 目标方向速度) / 惯性 —— 常见的平滑追踪写法。
		private static void SteerTo(Projectile projectile, Vector2 targetCenter, float speed, float inertia) {
			Vector2 direction = GetDirection(projectile, targetCenter) * speed;
			projectile.velocity = (projectile.velocity * (inertia - 1f) + direction) / inertia;
		}

		// 取“弹幕中心 -> 目标中心”的单位方向；重合时退化为朝上，避免除零。
		private static Vector2 GetDirection(Projectile projectile, Vector2 targetCenter) {
			Vector2 direction = targetCenter - projectile.Center;
			if (direction == Vector2.Zero) {
				return -Vector2.UnitY;
			}
			direction.Normalize();
			return direction;
		}

		// 让鱼头指向目标：按方向设置 rotation 与 spriteDirection。
		// 和风鱼贴图的天然头朝向是“左边”，所以朝右时需要水平翻转(spriteDirection=-1)、
		// 朝左时不翻转；旋转角在朝左时减去 Pi（180°）以校正贴图朝向。
		private static void FaceTarget(Projectile projectile, Vector2 targetCenter) {
			Vector2 direction = targetCenter - projectile.Center;
			if (direction == Vector2.Zero) {
				return;
			}

			// 和风鱼贴图的天然头朝向是“左边”，所以朝右时需要水平翻转、朝左时不翻转。
			float angle = direction.ToRotation();
			if (direction.X < 0f) {
				projectile.spriteDirection = 1;
				projectile.rotation = angle - MathHelper.Pi;
			}
			else {
				projectile.spriteDirection = -1;
				projectile.rotation = angle;
			}
		}

		// 冲刺时的尾尘，增强速度感（纯客户端表现，不影响判定）。
		private static void SpawnDashDust(Projectile projectile) {
			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.GemAmethyst,
					-projectile.velocity.X * 0.15f, -projectile.velocity.Y * 0.15f, 100, default, 1.1f);
				dust.noGravity = true;
			}
		}

		// 寻找范围内最近的、可攻击的敌人。
		// 返回 false 表示没找到，调用方会把控制权交还原版游动 AI。
		private static bool SearchForTargets(Projectile projectile, out Vector2 targetCenter, out float distanceFromTarget) {
			// 优先攻击玩家“标记”的目标：用鞭子命中或被右键指定的敌人会写入 OwnerMinionAttackTargetNPC
			NPC markedTarget = projectile.OwnerMinionAttackTargetNPC;
			if (markedTarget != null) {
				targetCenter = markedTarget.Center;
				distanceFromTarget = Vector2.Distance(markedTarget.Center, projectile.Center);
				return true;
			}

			distanceFromTarget = DetectRange;
			targetCenter = projectile.position;
			bool foundTarget = false;

			foreach (var npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy()) {
					continue; // 跳过友方、无敌等不可攻击目标
				}

				float between = Vector2.Distance(npc.Center, projectile.Center);
				bool inRange = between < distanceFromTarget;
				bool lineOfSight = Collision.CanHitLine(projectile.position, projectile.width, projectile.height, npc.position, npc.width, npc.height);
				bool closeThroughWall = between < 100f; // 很近时允许穿墙锁定

				if (inRange && (lineOfSight || closeThroughWall)) {
					distanceFromTarget = between;
					targetCenter = npc.Center;
					foundTarget = true;
				}
			}

			return foundTarget;
		}
	}
}
