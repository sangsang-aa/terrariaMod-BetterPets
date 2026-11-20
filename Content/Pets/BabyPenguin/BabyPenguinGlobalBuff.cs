using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// BabyPenguinGlobalBuff —— 改写原版增益“企鹅宝宝”的说明文字
	// ============================================================================
	// 【这是什么】
	//   替换原版宠物增益“企鹅宝宝”(BuffID.BabyPenguin, ID 41) 在增益栏中的说明文字。
	//   只改说明，不改增益效果本身。
	//
	// 【为什么用 GlobalBuff】
	//   GlobalBuff 挂到所有增益上；ModifyBuffText 在构造提示文本时调用，可以按 type
	//   精确判断并原地替换说明。
	//
	// 【涉及的原版 ID】
	//   BuffID.BabyPenguin —— “企鹅宝宝”宠物增益
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   GlobalBuff API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_buff.html
	//   本地化:         https://github.com/tModLoader/tModLoader/wiki/Localization
	// ============================================================================
	public class BabyPenguinGlobalBuff : GlobalBuff
	{
		public static LocalizedText Description { get; private set; }

		// SetStaticDefaults：内容注册阶段调用一次。
		public override void SetStaticDefaults() {
			Description = Mod.GetLocalization($"{nameof(BabyPenguinGlobalBuff)}.Description");
		}

		// ModifyBuffText：显示某个增益时调用；参数都是 ref，可原地替换。
		// 这里只改 tip，名称与稀有度保持原版。
		public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare) {
			if (type == BuffID.BabyPenguin) {
				tip = Description.Value;
			}
		}
	}
}
