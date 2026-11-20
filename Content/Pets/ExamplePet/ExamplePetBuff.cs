using Terraria;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.ExamplePet
{
	// 宠物增益：只要该增益存在，就会持续维持对应的宠物弹幕。
	public class ExamplePetBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true; // 不显示增益剩余时间
			Main.vanityPet[Type] = true; // 标记为装饰性（vanity）宠物，但弹幕本身仍可攻击
		}

		// 当该增益在玩家身上时，每帧都会调用此方法
		public override void Update(Player player, ref int buffIndex) {
			bool unused = false;
			// 若尚未生成宠物弹幕则生成一个，并刷新增益的持续时间
			player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<ExamplePetProjectile>());
		}
	}
}
