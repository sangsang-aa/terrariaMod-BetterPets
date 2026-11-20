using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.ExamplePet
{
	// 宠物本体：会跟随玩家，并根据与敌人的距离切换攻击方式：
	// 距离 < 300 用接触伤害冲撞；距离 >= 300 召唤一条限时星尘龙（不占召唤栏）。
	// 注意：它自身保持宠物定位（Main.projPet），不设置 minion / minionSlots，因此不占用召唤栏。
	public class ExamplePetProjectile : ModProjectile
	{
		// 接触伤害数值（想改近战伤害就改这里）
		private const int ContactDamage = 10000;
		// 远程召唤出的星尘龙伤害
		private const int RangedDamage = 5000;
		// 距离小于该值时使用接触伤害，否则使用远程攻击
		private const float MeleeRange = 100f;
		// 远程模式下与敌人保持的悬停距离
		private const float RangedHoverDistance = 320f;
		// 召唤星尘龙的间隔（帧，60 帧 = 1 秒）= 2 秒
		private const int DragonCooldown = 120;
		// 星尘龙的初始存活时间（帧）= 5 秒（实际寿命由 PetStardustDragonLifetime 统一管理）
		private const int DragonLifetime = 300;

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 4; // 贴图为 4 帧的竖向动画
			Main.projPet[Type] = true; // 标记为宠物弹幕（不占召唤栏）

			// 以下代码用于自定义“选择角色”界面中宠物的展示动作，简述：
			// * 使用与 Recipe 类似的链式（fluent）写法
			// * 用 ProjectileID.Sets.SimpleLoop 指定起始帧、结束帧与播放速度，可选择是否播放到结尾后反向播放
			// * 若玩家未高亮/站立时要停止动画，可加 .WhenNotSelected(0, 0)（用法与 SimpleLoop 相同）
			// * 用 .WithOffset(x, y) 与 .WithSpriteDirection(-1) 设置偏移和朝向
			// * 想进一步自定义动作时，可用 DelegateMethods.CharacterPreview 中的原版预设（通过 .WithCode()）
			ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type], 6)
				.WithOffset(-10, -20f)
				.WithSpriteDirection(-1)
				.WithCode(DelegateMethods.CharacterPreview.Float);
		}

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.ZephyrFish); // 复制原版“西风鱼”弹幕的基础属性

			Projectile.width = 20; // 命中判定框宽度
			Projectile.height = 20; // 命中判定框高度
			Projectile.aiStyle = 0; // 使用自定义 AI（不再套用西风鱼的 AI）
			Projectile.tileCollide = false; // 宠物可以穿墙
			Projectile.friendly = false; // 由 AI 根据攻击方式开关（避免待机时误伤训练假人/小动物）
			Projectile.hostile = false; // 不会伤害玩家
			Projectile.DamageType = DamageClass.Summon; // 伤害类型：召唤
			Projectile.damage = ContactDamage; // 接触伤害数值
			Projectile.knockBack = 4f; // 击退
			Projectile.penetrate = -1; // 命中敌人后不消失
			Projectile.minion = false; // 不是召唤物，不占用召唤栏
			Projectile.minionSlots = 0f; // 占用 0 个召唤栏
			Projectile.netImportant = true; // 多人游戏中同步给刚加入的玩家
		}

		// 关键：允许宠物通过“接触”对敌人造成伤害（由 tModLoader 的 Main.projPet 判定调用）
		public override bool MinionContactDamage() {
			return true;
		}

		// 宠物不会砍草/砍罐子
		public override bool? CanCutTiles() {
			return false;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];

			// 玩家死亡或没有宠物增益时，清除增益让宠物自然消失
			if (!owner.active || owner.dead) {
				owner.ClearBuff(ModContent.BuffType<ExamplePetBuff>());
				return;
			}

			// 只要宠物增益还在，就持续刷新存在时间，宠物不会消失
			if (owner.HasBuff(ModContent.BuffType<ExamplePetBuff>())) {
				Projectile.timeLeft = 2;
			}

			// 宠物弹幕由 buff 助手生成时伤害参数会被置为 0，这里持续写回伤害，确保接触伤害生效
			Projectile.damage = ContactDamage;

			// 索敌：寻找最近的可攻击敌人
			SearchForTargets(owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);

			if (!foundTarget) {
				// 无目标：待机跟随玩家，不攻击
				Projectile.friendly = false;
				Projectile.ai[0] = 0; // 重置召唤计时器
				IdleMovement(owner);
			}
			else if (distanceFromTarget < MeleeRange) {
				// 近距离：使用接触伤害，冲向敌人
				Projectile.friendly = true;
				Projectile.ai[0] = 0; // 重置召唤计时器
				ChaseMovement(distanceFromTarget, targetCenter);
			}
			else {
				// 远距离：保持距离并召唤星尘龙
				Projectile.friendly = false;
				RangedAttack(targetCenter);
			}

			// 动画与朝向
			Visuals();
		}

		// 寻找范围内最近的敌人
		private void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter) {
			distanceFromTarget = 700f; // 初始索敌半径
			targetCenter = Projectile.position;
			foundTarget = false;

			foreach (var npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy()) {
					continue; // 跳过不可攻击的目标（友方、无敌等）
				}

				float between = Vector2.Distance(npc.Center, Projectile.Center);
				bool closest = Vector2.Distance(Projectile.Center, targetCenter) > between;
				bool inRange = between < distanceFromTarget;
				bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height);
				bool closeThroughWall = between < 100f; // 很近时允许穿墙锁定

				if (((closest && inRange) || !foundTarget) && (lineOfSight || closeThroughWall)) {
					distanceFromTarget = between;
					targetCenter = npc.Center;
					foundTarget = true;
				}
			}
		}

		// 近战：朝敌人加速冲撞（造成接触伤害）
		private void ChaseMovement(float distanceFromTarget, Vector2 targetCenter) {
			float speed = 8f;
			float inertia = 20f;

			// 靠近到一定程度就不再加速，避免在敌人身上抖动
			if (distanceFromTarget > 40f) {
				Vector2 direction = targetCenter - Projectile.Center;
				direction.Normalize();
				direction *= speed;
				Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
			}
		}

		// 远程：与敌人保持悬停距离，并定期召唤星尘龙
		private void RangedAttack(Vector2 targetCenter) {
			// 悬停：目标位置 = 敌人位置沿“敌人→自己”方向外推 RangedHoverDistance
			Vector2 fromTarget = Projectile.Center - targetCenter;
			if (fromTarget == Vector2.Zero) {
				fromTarget = -Vector2.UnitY;
			}
			fromTarget.Normalize();
			Vector2 desiredPosition = targetCenter + fromTarget * RangedHoverDistance;
			Vector2 toDesired = desiredPosition - Projectile.Center;

			float distanceToDesired = toDesired.Length();
			float speed = 6f;
			float inertia = 25f;

			if (distanceToDesired > 20f) {
				toDesired.Normalize();
				toDesired *= speed;
				Projectile.velocity = (Projectile.velocity * (inertia - 1) + toDesired) / inertia;
			}

			// 召唤计时器
			Projectile.ai[0]++;
			if (Projectile.ai[0] >= DragonCooldown) {
				Projectile.ai[0] = 0;
				FireStardustDragon();
			}
		}

		// 生成一条完整的原版星尘龙（头 + 2 节身体 + 尾），并让其不占召唤栏
		private void FireStardustDragon() {
			// 只在弹幕拥有者的客户端生成，避免多人游戏中重复生成
			if (Main.myPlayer != Projectile.owner) {
				return;
			}

			// 场上已有我们发射的星尘龙时不再重复生成（用 minion == false 区分玩家自己召唤的龙）
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile existing = Main.projectile[i];
				if (existing.active && existing.owner == Projectile.owner && existing.type == ProjectileID.StardustDragon1 && !existing.minion) {
					return;
				}
			}

			Vector2 spawnPosition = Projectile.Center;
			Vector2 velocity = Vector2.Zero;

			// 按原版法杖的方式生成 4 段并链接：ai[0] 指向前一段的索引（NewProjectile 会自动换算成 UUID）
			int head = Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPosition, velocity, ProjectileID.StardustDragon1, RangedDamage, 2f, Projectile.owner, 0f, 0f);
			int body1 = Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPosition, velocity, ProjectileID.StardustDragon2, RangedDamage, 2f, Projectile.owner, head, 0f);
			int body2 = Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPosition, velocity, ProjectileID.StardustDragon3, RangedDamage, 2f, Projectile.owner, body1, 0f);
			int tail = Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawnPosition, velocity, ProjectileID.StardustDragon4, RangedDamage, 2f, Projectile.owner, body2, 0f);

			// 原版链接方式：每一段的 localAI[1] 指向它的下一段
			if (Main.projectile.IndexInRange(body1)) {
				Main.projectile[body1].localAI[1] = body2;
			}
			if (Main.projectile.IndexInRange(body2)) {
				Main.projectile[body2].localAI[1] = tail;
			}

			// 统一处理四段：不占召唤栏、设定初始寿命、记录原始伤害
			int[] segments = { head, body1, body2, tail };
			foreach (int index in segments) {
				if (!Main.projectile.IndexInRange(index)) {
					continue;
				}

				Projectile dragon = Main.projectile[index];
				dragon.minion = false; // 关键：不占用召唤栏
				dragon.minionSlots = 0f;
				dragon.timeLeft = DragonLifetime; // 初始寿命，实际由 PetStardustDragonLifetime 管理
				dragon.originalDamage = RangedDamage;
				dragon.netUpdate = true; // 同步给其他玩家
			}
		}

		// 待机：回到玩家身边悬浮
		private void IdleMovement(Player owner) {
			Vector2 idlePosition = owner.Center + new Vector2(-30f * owner.direction, -50f);
			Vector2 toIdle = idlePosition - Projectile.Center;
			float distanceToIdle = toIdle.Length();

			// 距离过远时直接传送回玩家身边（仅本地玩家处理，并标记网络同步）
			if (distanceToIdle > 2000f && Main.myPlayer == owner.whoAmI) {
				Projectile.position = idlePosition;
				Projectile.velocity *= 0.1f;
				Projectile.netUpdate = true;
				return;
			}

			float speed;
			float inertia;
			if (distanceToIdle > 600f) {
				speed = 12f; // 离得远，加快速度
				inertia = 60f;
			}
			else {
				speed = 4f; // 靠近玩家，放慢速度
				inertia = 80f;
			}

			if (distanceToIdle > 20f) {
				toIdle.Normalize();
				toIdle *= speed;
				Projectile.velocity = (Projectile.velocity * (inertia - 1) + toIdle) / inertia;
			}
			else if (Projectile.velocity == Vector2.Zero) {
				// 完全静止时给一点微小推动，避免卡死
				Projectile.velocity.X = -0.15f;
				Projectile.velocity.Y = -0.05f;
			}
		}

		// 播放动画并设置朝向/倾斜
		private void Visuals() {
			int frameSpeed = 5;
			Projectile.frameCounter++;
			if (Projectile.frameCounter >= frameSpeed) {
				Projectile.frameCounter = 0;
				Projectile.frame++;
				if (Projectile.frame >= Main.projFrames[Projectile.type]) {
					Projectile.frame = 0;
				}
			}

			if (Projectile.velocity.X > 0f) {
				Projectile.spriteDirection = 1;
			}
			else if (Projectile.velocity.X < 0f) {
				Projectile.spriteDirection = -1;
			}

			Projectile.rotation = Projectile.velocity.X * 0.05f; // 根据水平速度轻微倾斜
		}
	}
}
