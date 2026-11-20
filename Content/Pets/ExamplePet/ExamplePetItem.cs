using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.ExamplePet
{
	// 宠物召唤物：使用后获得宠物增益，从而召唤出宠物。
	public class ExamplePetItem : ModItem
	{
		// 名称与描述在 Localization 目录下的 .hjson 文件中定义。
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.ZephyrFish); // 直接复制原版“西风鱼”的默认属性

			Item.shoot = ModContent.ProjectileType<ExamplePetProjectile>(); // 使用物品时发射的宠物弹幕
			Item.buffType = ModContent.BuffType<ExamplePetBuff>(); // 使用时附加的宠物增益
		}

		public override bool? UseItem(Player player) {
			// 仅本地玩家添加增益，避免多人游戏中重复添加
			if (player.whoAmI == Main.myPlayer) {
				player.AddBuff(Item.buffType, 3600);
			}
			return true;
		}

		public override void AddRecipes() {
			// 示例配方：10 个泥土 + 工作台（可替换为你自己的材料/工作台）
			CreateRecipe()
				.AddIngredient(ItemID.DirtBlock, 10)
				.AddTile(TileID.WorkBenches)
				.Register();
		}
	}
}
