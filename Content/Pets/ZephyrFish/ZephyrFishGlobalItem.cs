using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.ZephyrFish
{
	// ============================================================================
	// ZephyrFishGlobalItem —— 改写原版物品“和风鱼”的提示文本
	// ============================================================================
	// 【这是什么】
	//   替换原版召唤物品“和风鱼”(ItemID.ZephyrFish) 的 Tooltip，让它描述改写后的冲撞能力。
	//   只改提示文字，不改物品属性，也不改物品名称。
	//
	// 【为什么用 GlobalItem】
	//   原版物品无法直接改代码；GlobalItem 会挂到“所有物品”（含原版）上，
	//   再用 AppliesToEntity 精确限定到这一件物品。
	//   类文档: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_item.html
	//
	// 【涉及的原版 ID】
	//   ItemID.ZephyrFish —— 召唤“和风鱼”宠物的物品
	//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
	//
	// 【相关文档】
	//   基础物品: https://github.com/tModLoader/tModLoader/wiki/Basic-Item
	//   本地化:   https://github.com/tModLoader/tModLoader/wiki/Localization
	// ============================================================================
	public class ZephyrFishGlobalItem : GlobalItem
	{
		// 本地化文本缓存：SetStaticDefaults 在注册期只调用一次，之后显示 Tooltip 时直接取缓存。
		public static LocalizedText TooltipText { get; private set; }

		// SetStaticDefaults：模组加载、内容注册阶段调用一次；此时还不能访问具体物品实例。
		// Mod.GetLocalization 按 key 取本地化文本；key 使用类名，方便与 hjson 对照。
		// 参考: https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_item.html
		public override void SetStaticDefaults() {
			TooltipText = Mod.GetLocalization($"{nameof(ZephyrFishGlobalItem)}.Tooltip");
		}

		// AppliesToEntity：在物品初始化时调用，决定这份 Global 是否作用于该物品。
		// lateInstantiation 为 true 表示在所有内容初始化完成后的“后注册”阶段调用。
		// 这里只认 ItemID.ZephyrFish，其它物品完全不受影响。
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
			return entity.type == ItemID.ZephyrFish;
		}

		// ModifyTooltips：构造物品提示时调用，参数 tooltips 是本次要显示的“行”列表，可增删改。
		// 原版物品本来就有以 "Tooltip" 开头的说明行；这里优先替换它，没有才新增，
		// 这样不会覆盖名称行(ItemName)或其它行。
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			TooltipLine line = tooltips.Find(x => x.Name.StartsWith("Tooltip"));
			if (line != null) {
				line.Text = TooltipText.Value;
			}
			else {
				tooltips.Add(new TooltipLine(Mod, "ZephyrFishTooltip", TooltipText.Value));
			}
		}
	}
}
