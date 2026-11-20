using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarGlobalBuff : GlobalBuff
    {
        public static LocalizedText Description { get; private set; }

        public override void SetStaticDefaults()
        {
            Description = Mod.GetLocalization($"{nameof(NectarGlobalBuff)}.Description");
        }

        // 构造增益提示时调用；buffName/tip/rare 都是 ref，可原地替换。
        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.BabyHornet)
            {
                tip = Description.Value;
            }
        }
    }
}