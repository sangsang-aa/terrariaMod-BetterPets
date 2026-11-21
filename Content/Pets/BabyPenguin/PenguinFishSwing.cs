using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// PenguinFishSwing —— 企鹅宝宝的挥鱼动作（近战挥砍 / 远程前摇共用）
	// ============================================================================
	// 【这是什么】
	//   由 BabyPenguinGlobalProjectile 生成的短命弹幕。贴图直接复用 ItemID.Fish 的
	//   物品图标，因此不需要新的 png。
	//
	// 【机制】
	//   ai[0] = 企鹅弹幕索引；ai[1] = 挥砍基准角度（朝目标）；ai[2] = 本次动画总帧数。
	//   每帧围绕“企鹅中心”做圆弧运动，同时把 rotation 设为圆弧方向，形成“挥鱼”的视觉。
	//   朝左挥砍（或朝左前摇）时把鱼贴图水平镜像，保持原版镜像的观感。
	//   弹幕本身 friendly，靠接触判定造成召唤伤害（局部无敌帧），近战与远程前摇都能打人。
	//   近战命中不再反弹；企鹅的慢速后撤只由 BabyPenguinGlobalProjectile 的
	//   Melee -> Ranged 模式切换处理。
	//
	//   注意：远程前摇只播放到“鱼正对目标”（进度 50%）就被投掷逻辑 Kill 掉，
	//   不会播完整段动画。
	//
	// 【涉及的原版 ID】
	//   ItemID.Fish / ProjectileID.Penguin
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   ModProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html
	//   基础弹幕指南:     https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	// ============================================================================
	public class PenguinFishSwing : ModProjectile
	{
		public const int MeleeLifetime = 16;    // 近战挥砍完整帧数
		public const int RangedLifetime = 12;   // 远程前摇完整帧数（只播一半就丢出）
		public const float OrbitRadius = 30f;   // 鱼围绕企鹅的半径（像素）

		// 局部命中冷却 > 最长挥砍帧数：保证同一弹幕对同一目标在整个生命周期内
		// 只结算一次命中，避免一次挥砍因 localNPCHitCooldown 过短而重复判定。
		private const int HitCooldown = MeleeLifetime + 4;

		private const float SwingStart = -1.2f; // 起始角（弧度）
		private const float SwingEnd = 1.2f;    // 结束角

		// 复用原版“鱼”(ItemID.Fish) 的物品图标
		public override string Texture => "Terraria/Images/Item_" + ItemID.Fish;

		// 登记为 MinionShot：让鞭子的召唤标记(tag)加成作用到挥砍伤害上。
		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionShot[Type] = true;
			ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 1f;
		}

		// SetDefaults：弹幕创建时调用；默认按近战长度初始化，远程会在生成后改成 RangedLifetime。
		public override void SetDefaults() {
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.aiStyle = 0;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = MeleeLifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = HitCooldown; // 一砍只中一次（冷却覆盖整个挥砍时长）
		}

		// AI：每帧围绕企鹅做圆弧运动并旋转贴图，形成“挥舞鱼”的轨迹。
		public override void AI() {
			int petIndex = (int)Projectile.ai[0];
			if (petIndex < 0 || petIndex >= Main.maxProjectiles) {
				Projectile.Kill();
				return;
			}

			Projectile pet = Main.projectile[petIndex];
			if (!pet.active || pet.type != ProjectileID.Penguin) {
				Projectile.Kill();
				return;
			}

			// 总帧数由生成方通过 ai[2] 传入（近战 16 / 远程 12）
			float total = Projectile.ai[2] > 0f ? Projectile.ai[2] : MeleeLifetime;
			float progress = MathHelper.Clamp(1f - Projectile.timeLeft / total, 0f, 1f);
			float baseAngle = Projectile.ai[1];

			// 根据目标在左/右决定挥砍方向，保证两边都是“从上往下”挥
			float wrapped = MathHelper.WrapAngle(baseAngle);
			bool faceLeft = wrapped > MathHelper.PiOver2 || wrapped < -MathHelper.PiOver2;
			float start = faceLeft ? SwingEnd : SwingStart;
			float end = faceLeft ? SwingStart : SwingEnd;
			float angle = baseAngle + MathHelper.Lerp(start, end, progress);

			// 支点在企鹅中心：鱼以自身一端为轴向外挥动（见下方 PreDraw）
			Projectile.Center = pet.Center;
			Projectile.rotation = angle;
		}

		// PreDraw：以鱼的“内侧一端”为旋转支点绘制，让挥动的圆心落在企鹅身上。
		// 默认绘制以贴图中心为原点，会看起来像在企鹅前方绕圈，所以这里手动绘制。
		public override bool PreDraw(ref Color lightColor) {
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 position = Projectile.Center - Main.screenPosition;

			float wrapped = MathHelper.WrapAngle(Projectile.ai[1]);
			bool faceLeft = wrapped > MathHelper.PiOver2 || wrapped < -MathHelper.PiOver2;

			// 正常朝右：左端（鱼尾）作支点；朝左镜像后，鱼尾跑到右端，
			// 所以支点也换到右端，保证两种方向都是“握鱼尾、鱼头朝外”。
			Vector2 origin = faceLeft
				? new Vector2(texture.Width, texture.Height * 0.5f)
				: new Vector2(0f, texture.Height * 0.5f);

			// 镜像后纹理会朝支点左侧延伸；rotation 再减 π 把它转回当前挥砍方向。
			SpriteEffects effects = faceLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float rotation = faceLeft ? Projectile.rotation - MathHelper.Pi : Projectile.rotation;

			Main.EntitySpriteDraw(texture, position, null, lightColor, rotation, origin, Projectile.scale, effects, 0f);
			return false;
		}

		// CanCutTiles：false = 不会割草/砸罐。
		public override bool? CanCutTiles() {
			return false;
		}
	}
}
