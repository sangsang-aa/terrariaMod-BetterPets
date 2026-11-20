using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Items
{
	// This is a basic item template.
	// Please see tModLoader's ExampleMod for every other example:
	// https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod
	public class TutorialSword : ModItem
	{
		// The Display Name and Tooltip of this item can be edited in the 'Localization/en-US_Mods.TutorialMod.hjson' file.
		public override void SetDefaults()
		{
			Item.damage = 50000;
			Item.DamageType = DamageClass.Melee;
			Item.shoot = ProjectileID.EnchantedBeam; // 挥舞时发射的原版剑光弹幕（友好弹幕，不会伤害玩家与城镇NPC）
			Item.shootSpeed = 12f; // 弹幕速度（方向跟随鼠标）
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 10;
			Item.useAnimation = 10;
			Item.useStyle = ItemUseStyleID.Swing; // 改成挥砍动作
			Item.knockBack = 6;
			Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item4;
			Item.autoReuse = true;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DirtBlock, 10);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
