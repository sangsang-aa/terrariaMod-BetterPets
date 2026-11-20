using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Common.GlobalProjectiles
{
	// 统一管理“宠物发射的星尘龙”的寿命，并确保它永不占用召唤栏。
	// 只处理 minion == false 的星尘龙（即由宠物发射出来的那几段；玩家自己召唤的星尘龙 minion == true，不受影响）。
	public class PetStardustDragonLifetime : GlobalProjectile
	{
		// 每个弹幕实体单独持有一份状态
		public override bool InstancePerEntity => true;

		// 寿命：5 秒（60 帧 = 1 秒）
		private const int Lifetime = 300;

		private bool initialized;
		private int life;

		public override void AI(Projectile projectile) {
			// 玩家自己召唤的星尘龙（minion == true）不处理
			if (projectile.minion) {
				return;
			}
			if (!IsStardustDragon(projectile.type)) {
				return;
			}

			// 首次遇到时初始化寿命
			if (!initialized) {
				initialized = true;
				life = Lifetime;
			}

			// 持续写回“不占召唤栏”的标记（防止其他逻辑把它改回去）
			projectile.minion = false;
			projectile.minionSlots = 0f;

			// 由我们接管存活时间：无论玩家身上有没有星尘龙 buff，都必须按时消失
			life--;
			projectile.timeLeft = 2;
			if (life <= 0) {
				projectile.Kill();
			}
		}

		private static bool IsStardustDragon(int type) {
			return type == ProjectileID.StardustDragon1
				|| type == ProjectileID.StardustDragon2
				|| type == ProjectileID.StardustDragon3
				|| type == ProjectileID.StardustDragon4;
		}
	}
}
