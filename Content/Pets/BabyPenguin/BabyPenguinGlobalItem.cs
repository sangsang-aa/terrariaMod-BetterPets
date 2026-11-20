using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyPenguin
{
	// ============================================================================
	// BabyPenguinGlobalItem —— 改写原版物品“鱼”的提示文本
	// ============================================================================
	// 【这是什么】
	//   替换原版召唤物品“鱼”(ItemID.Fish, ID 669) 的 Tooltip，让它描述改写后的
	//   近战挥鱼 / 远程丢鱼能力。只改提示文字，不改物品属性，也不改物品名称。
	//
	// 【为什么用 GlobalItem】
	//   原版物品无法直接改代码；GlobalItem 挂到所有物品上，再用 AppliesToEntity
	//   精确限定到“鱼”这一件物品。
	//
	// 【涉及的原版 ID】
	//   ItemID.Fish —— 召唤“企鹅宝宝”的物品
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   GlobalItem API: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_item.html
	//   本地化:         https://github.com/tModLoader/tModLoader/wiki/Localization
	// ============================================================================
	public class BabyPenguinGlobalItem : GlobalItem
	{
		public static LocalizedText TooltipText { get; private set; }

		// SetStaticDefaults：内容注册阶段调用一次；此时还不能访问具体物品实例。
		public override void SetStaticDefaults() {
			TooltipText = Mod.GetLocalization($"{nameof(BabyPenguinGlobalItem)}.Tooltip");
		}

		// AppliesToEntity：物品初始化时调用；只认 ItemID.Fish。
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
			return entity.type == ItemID.Fish;
		}

		// ModifyTooltips：构造物品提示时调用；优先替换已有的 "Tooltip" 行，没有才新增。
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			TooltipLine line = tooltips.Find(x => x.Name.StartsWith("Tooltip"));
			if (line != null) {
				line.Text = TooltipText.Value;
			}
			else {
				tooltips.Add(new TooltipLine(Mod, "BabyPenguinTooltip", TooltipText.Value));
			}
		}
	}
}
