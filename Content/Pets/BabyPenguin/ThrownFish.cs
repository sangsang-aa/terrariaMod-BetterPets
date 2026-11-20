using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// ThrownFish —— 企鹅宝宝的远程投掷（把“鱼”抛物线丢出去）
	// ============================================================================
	// 【这是什么】
	//   由 BabyPenguinGlobalProjectile 生成的远程弹幕，贴图复用 ItemID.Fish 的物品图标。
	//
	// 【机制】
	//   生成时已由 CalculateLob 反推出初速度；这里每帧叠加一点重力形成抛物线，
	//   同时旋转贴图，看起来像被丢出去的鱼。命中一个目标后消失（penetrate = 1）。
	//
	// 【涉及的原版 ID】
	//   ItemID.Fish（贴图）
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   ModProjectile API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html
	//   基础弹幕指南:     https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile
	// ============================================================================
	public class ThrownFish : ModProjectile
	{
		private const float Gravity = 0.3f;   // 下落加速度（像素/帧²），与生成时反推用的一致
		private const float SpinSpeed = 0.25f; // 旋转速度（弧度/帧）

		// 复用原版“鱼”(ItemID.Fish) 的物品图标
		public override string Texture => "Terraria/Images/Item_" + ItemID.Fish;

		// 登记为 MinionShot：让鞭子的召唤标记(tag)加成作用到投掷伤害上。
		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionShot[Type] = true;
			ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 1f;
		}

		// SetDefaults：弹幕创建时调用。
		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = 0;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true; // 会撞墙/撞地，不允许穿墙
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 180;
		}

		// AI：每帧加一点重力形成抛物线，并旋转贴图。
		public override void AI() {
			Projectile.velocity.Y += Gravity;
			Projectile.rotation += SpinSpeed * (Projectile.velocity.X >= 0f ? 1f : -1f);
		}

		// CanCutTiles：false = 不会割草/砸罐。
		public override bool? CanCutTiles() {
			return false;
		}
	}
}
