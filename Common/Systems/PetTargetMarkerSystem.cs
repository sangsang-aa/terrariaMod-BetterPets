using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Common.Systems
{
	// ============================================================================
	// PetTargetMarkerSystem —— 给“右键/鞭子标记的目标”加一个可见提示
	// ============================================================================
	// 【为什么需要】
	//   原版右键标记只会把敌人写进 Player.MinionAttackTargetNPC，本身没有任何
	//   视觉特效；鞭子的标记也只有一个 debuff 图标。这里在目标头顶补一圈脉冲尘，
	//   让玩家能立刻确认“右键确实标记到了”。
	//
	// 【触发条件】
	//   1) 只在本地客户端执行（Main.myPlayer），并且玩家当前拥有本模组重写宠物之一；
	//   2) Player.MinionAttackTargetNPC 指向一个仍然存活、可被追击的敌人；
	//   3) 每 8 帧生成一圈尘，避免每帧刷屏。
	//
	// 【涉及的原版 ID】
	//   ProjectileID.ZephyrFish = 380  和风鱼宠物本体
	//   ProjectileID.BabyEater  = 175  婴儿吞噬怪宠物本体
	//   ProjectileID.Penguin    = 112  企鹅宝宝宠物本体
	//   ProjectileID.BabyHornet = 蜜蜂宝宝（Nectar 宠物）本体
	//   DustID.GemTopaz         = 大颗黄色宝石尘
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【多人】
	//   Player.MinionAttackTargetNPC 是本地玩家状态；尘只在本地生成，不需要同步。
	//
	// 【参考】
	//   ModSystem: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_system.html
	//   Dust:      https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_dust.html
	// ============================================================================
	public class PetTargetMarkerSystem : ModSystem
	{
		// PostUpdateEverything：每帧所有世界/玩家/弹幕更新完之后调用。
		// 在这里补尘，尘会在本帧绘制并持续若干帧。
		public override void PostUpdateEverything() {
			if (Main.dedServ || Main.gameMenu) {
				return;
			}

			Player player = Main.player[Main.myPlayer];
			if (player == null || !player.active || player.dead) {
				return;
			}

			// 只有“本模组重写的宠物”在场时才显示，避免影响纯原版/其它模组。
			if (!HasTutorialPet(player)) {
				return;
			}

			int npcIndex = player.MinionAttackTargetNPC;
			if (npcIndex < 0 || npcIndex >= Main.maxNPCs) {
				return;
			}

			NPC target = Main.npc[npcIndex];
			if (target == null || !target.active || !target.CanBeChasedBy()) {
				return;
			}

			// 每 8 帧补一圈，既明显又不会把尘刷爆。
			if (Main.GameUpdateCount % 8 != 0) {
				return;
			}

			// 目标头顶：高度一半再往上 18 像素。
			Vector2 center = target.Center - Vector2.UnitY * (target.height * 0.5f + 18f);

			// 呼吸式半径 + 缓慢旋转，让标记看起来像“锁定圈”。
			float radius = 13f + (float)Math.Sin(Main.GameUpdateCount * 0.12) * 2f;
			float rotation = Main.GameUpdateCount * 0.06f;

			for (int i = 0; i < 8; i++) {
				float angle = rotation + MathHelper.TwoPi * i / 8f;
				Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
				SpawnMarkerDust(center + offset);
			}
		}

		// 生成一颗不落、不参与光照衰减的金色尘。
		private static void SpawnMarkerDust(Vector2 position) {
			Dust dust = Dust.NewDustPerfect(position, DustID.GemTopaz, Vector2.Zero, 0, default, 1.15f);
			dust.noGravity = true;
			dust.noLight = true;
			dust.velocity = Vector2.Zero;
		}

		// 玩家是否拥有本模组重写宠物中的任意一只。
		private static bool HasTutorialPet(Player player) {
			int owner = player.whoAmI;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile projectile = Main.projectile[i];
				if (!projectile.active || projectile.owner != owner) {
					continue;
				}

				if (projectile.type == ProjectileID.ZephyrFish
					|| projectile.type == ProjectileID.BabyEater
					|| projectile.type == ProjectileID.Penguin
					|| projectile.type == ProjectileID.BabyHornet) {
					return true;
				}
			}

			return false;
		}
	}
}
