using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarGlobalItem : GlobalItem
    {
        public static LocalizedText TooltipText { get; private set; }

        // 注册阶段调用一次：缓存本地化文本。
        public override void SetStaticDefaults()
        {
            TooltipText = Mod.GetLocalization($"{nameof(NectarGlobalItem)}.Tooltip");
        }

        // 物品初始化时调用：只认原版花蜜。
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Nectar;
        }

        // 构造物品提示时调用：替换已有的 Tooltip 行，没有就新增。
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            TooltipLine line = tooltips.Find(x => x.Name.StartsWith("Tooltip"));
            if (line != null)
            {
                line.Text = TooltipText.Value;
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "NectarTooltip", TooltipText.Value));
            }
        }
    }
}
