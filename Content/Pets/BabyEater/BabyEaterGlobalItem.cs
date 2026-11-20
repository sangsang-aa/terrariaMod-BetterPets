using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.BabyEater
{
	// ============================================================================
	// BabyEaterGlobalItem —— 改写原版物品“吞噬怪骨头”的提示文本
	// ============================================================================
	// 【这是什么】
	//   替换原版召唤物品“吞噬怪骨头”(ItemID.EatersBone) 的 Tooltip，
	//   使其描述与改写后的攻击能力一致。只改提示文字，不改物品属性，也不改物品名称。
	//
	// 【为什么用 GlobalItem】
	//   原版物品无法直接改代码；GlobalItem 会挂到“所有物品”（含原版）上，
	//   再用 AppliesToEntity 精确限定到这一件物品。
	//   类文档: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_item.html
	//
	// 【涉及的原版 ID】
	//   ItemID.EatersBone —— 召唤“婴儿吞噬怪”宠物的物品
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   基础物品: https://github.com/tModLoader/tModLoader/wiki/Basic-Item
	//   本地化:   https://github.com/tModLoader/tModLoader/wiki/Localization
	// ============================================================================
	public class BabyEaterGlobalItem : GlobalItem
	{
		// 本地化文本缓存：注册期读取一次，之后直接复用。
		public static LocalizedText TooltipText { get; private set; }

		// SetStaticDefaults：内容注册阶段调用一次；此时还不能访问具体物品实例。
		// Mod.GetLocalization 的 key 对应 hjson 中的 Mods.TutorialMod.<key>。
		public override void SetStaticDefaults() {
			TooltipText = Mod.GetLocalization($"{nameof(BabyEaterGlobalItem)}.Tooltip");
		}

		// AppliesToEntity：在物品初始化时调用，决定这份 Global 是否作用于该物品。
		// lateInstantiation 为 true 表示在所有内容初始化完成后的“后注册”阶段调用。
		// 这里只认 ItemID.EatersBone（吞噬怪骨头），其它物品不受影响。
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
			return entity.type == ItemID.EatersBone;
		}

		// ModifyTooltips：构造物品提示时调用；优先替换已有的 "Tooltip" 行，没有才新增。
		// 只动说明文字，不碰名称等其它行。
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			TooltipLine line = tooltips.Find(x => x.Name.StartsWith("Tooltip"));
			if (line != null) {
				line.Text = TooltipText.Value;
			}
			else {
				tooltips.Add(new TooltipLine(Mod, "BabyEaterTooltip", TooltipText.Value));
			}
		}
	}
}
