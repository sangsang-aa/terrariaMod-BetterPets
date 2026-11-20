using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyEater
{
	// ============================================================================
	// BabyEaterGlobalBuff —— 改写原版增益“婴儿吞噬怪”的说明文字
	// ============================================================================
	// 【这是什么】
	//   替换原版宠物增益“婴儿吞噬怪”(BuffID.BabyEater) 在增益栏中的说明文字。
	//   只改说明，不改增益效果本身。
	//
	// 【为什么用 GlobalBuff】
	//   GlobalBuff 会挂到“所有增益”（含原版）上；ModifyBuffText 在游戏构造提示文本时调用，
	//   可以按 type 精确判断并原地替换说明。
	//   类文档: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_buff.html
	//
	// 【涉及的原版 ID】
	//   BuffID.BabyEater —— “婴儿吞噬怪”宠物增益
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   本地化: https://github.com/tModLoader/tModLoader/wiki/Localization
	// ============================================================================
	public class BabyEaterGlobalBuff : GlobalBuff
	{
		// 本地化文本缓存：注册期读取一次，之后直接复用。
		public static LocalizedText Description { get; private set; }

		// SetStaticDefaults：内容注册阶段调用一次。
		// Mod.GetLocalization 的 key 对应 hjson 中的 Mods.TutorialMod.<key>。
		public override void SetStaticDefaults() {
			Description = Mod.GetLocalization($"{nameof(BabyEaterGlobalBuff)}.Description");
		}

		// ModifyBuffText：游戏显示某个增益时调用；type 是增益 ID。
		// 参数都是 ref，可原地替换：buffName=名称、tip=说明文字、rare=稀有度颜色。
		// 这里只改 tip，名称与稀有度保持原版。
		public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare) {
			if (type == BuffID.BabyEater) {
				tip = Description.Value;
			}
		}
	}
}
