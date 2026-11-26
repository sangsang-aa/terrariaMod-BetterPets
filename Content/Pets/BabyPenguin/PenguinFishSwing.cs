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
	//   ai[0] = 企鹅弹幕索引；ai[1] = 挥砍基准角度（朝目标）；
	//   ai[2] = 本次动画总帧数（近战 28 / 远程 12）；蓄力帧数由此反推（28 → 12 / 12 → 0），不占用 ai[3]。
	//   每帧围绕“企鹅中心”做圆弧运动，同时把 rotation 设为圆弧方向，形成“挥鱼”的视觉。
	//   朝左挥砍（或朝左前摇）时把鱼贴图水平镜像，保持原版镜像的观感。
	//   弹幕本身 friendly，靠接触判定造成召唤伤害（局部无敌帧），近战与远程前摇都能打人。
	//
	// 【近战两段式动画】
	//   蓄力段（前 12 帧）：角度固定在起手位，鱼从 1 倍平滑放大到 2 倍。
	//   挥砍段（后 16 帧）：放大倍数回落到 1，角度从起手扫到结束。
	//   缩放只作用于绘制（drawScale），不改变 64x64 碰撞框，也不引入额外时间戳：
	//   每帧由 timeLeft 现算、蓄力帧数由 ai[2] 反推，不依赖额外同步字段，各客户端表现一致。
	//
	//   注意：远程前摇只播放到“鱼正对目标”（进度 50%）就被投掷逻辑 Kill 掉，
	//   且不带蓄力（duration=12 反推出 windup=0），不会播完整段动画。
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
		public const int MeleeLifetime = 16;    // 近战“挥砍段”帧数（不含蓄力）
		public const int MeleeWindup = 12;      // 近战“蓄力段”帧数：鱼放大到 WindupScale 倍、角度不动
		public const int MeleeTotalLifetime = MeleeWindup + MeleeLifetime; // 近战总时长 28 帧
		public const int RangedLifetime = 12;   // 远程前摇完整帧数（只播一半就丢出）
		public const float OrbitRadius = 30f;   // 鱼围绕企鹅的半径（像素）
		public const float WindupScale = 2f;    // 蓄力段放大倍数

		// 局部命中冷却 > 最长生命周期（近战 28 帧）：保证同一弹幕对同一目标在整个
		// 生命周期内只结算一次命中。蓄力段延长后必须同步提高，否则重现“一砍多次判定”。
		private const int HitCooldown = MeleeTotalLifetime + 4;

		private const float SwingStart = -1.2f; // 起始角（弧度）
		private const float SwingEnd = 1.2f;    // 结束角

		private float drawScale = 1f; // 本地绘制缩放：AI 每帧现算、PreDraw 读取，不参与网络同步

		// 复用原版“鱼”(ItemID.Fish) 的物品图标
		public override string Texture => "Terraria/Images/Item_" + ItemID.Fish;

		// 登记为 MinionShot：让鞭子的召唤标记(tag)加成作用到挥砍伤害上。
		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionShot[Type] = true;
			ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 1f;
		}

		// SetDefaults：弹幕创建时调用；默认按近战总长初始化，远程会在生成后改成 RangedLifetime。
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
			Projectile.timeLeft = MeleeTotalLifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = HitCooldown; // 一砍只中一次（冷却覆盖整个生命周期）
		}

		// AI：蓄力段放大鱼且角度不动；挥砍段缩放回落并把鱼从起始角扫到结束角。
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

			// 总帧数由生成方通过 ai[2] 传入；蓄力帧数按总帧数反推，绕开只有 3 个槽位的 ai 数组：
			// 近战 28 帧 → 12 帧蓄力；远程 12 帧 → 不蓄力。
			float total = Projectile.ai[2] > 0f ? Projectile.ai[2] : MeleeTotalLifetime;
			float windup = total >= MeleeTotalLifetime ? MeleeWindup : 0f;
			float elapsed = total - Projectile.timeLeft; // 已经过的帧数
			float baseAngle = Projectile.ai[1];

			// 根据目标在左/右决定挥砍方向，保证两边都是“从上往下”挥
			float wrapped = MathHelper.WrapAngle(baseAngle);
			bool faceLeft = wrapped > MathHelper.PiOver2 || wrapped < -MathHelper.PiOver2;
			float start = faceLeft ? SwingEnd : SwingStart;
			float end = faceLeft ? SwingStart : SwingEnd;

			if (windup > 0f && elapsed < windup) {
				// 蓄力段：鱼停在起手角，从 1 倍平滑放大到 2 倍
				drawScale = MathHelper.Lerp(1f, WindupScale, MathHelper.Clamp(elapsed / windup, 0f, 1f));
				Projectile.rotation = baseAngle + start;
			}
			else {
				// 挥砍段：缩放从"蓄力结束时的大小"回落到 1，角度从起手扫到结束。
				// 近战有蓄力段，起点是放大后的 2 倍；远程没有蓄力段（windup=0），
				// 起点必须是 1 倍，否则远程第一帧 swingProgress=0 会直接显示 2 倍放大的鱼。
				float swingTotal = MathHelper.Max(total - windup, 1f);
				float swingProgress = MathHelper.Clamp((elapsed - windup) / swingTotal, 0f, 1f);
				float startScale = windup > 0f ? WindupScale : 1f;
				drawScale = MathHelper.Lerp(startScale, 1f, swingProgress);
				Projectile.rotation = baseAngle + MathHelper.Lerp(start, end, swingProgress);
			}

			// 支点在企鹅中心：鱼以自身一端为轴向外挥动（见下方 PreDraw）
			Projectile.Center = pet.Center;
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

			Main.EntitySpriteDraw(texture, position, null, lightColor, rotation, origin, drawScale, effects, 0f);
			return false;
		}

		// CanCutTiles：false = 不会割草/砸罐。
		public override bool? CanCutTiles() {
			return false;
		}
	}
}
