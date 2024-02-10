using RoR2;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanBuffs
    {
        // armor buff gained during roll
        public static BuffDef armorBuff;
        public static BuffDef hookImmunityBuff;
        public static BuffDef hookTauntDebuff;

        public static void Init(AssetBundle assetBundle)
        {
            armorBuff = Modules.Content.CreateAndAddBuff("HenryArmorBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.white,
                false,
                false);

            hookImmunityBuff = Modules.Content.CreateAndAddBuff("HookImmunityBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.white,
                false,
                false);
            hookTauntDebuff = Modules.Content.CreateAndAddBuff("HookTauntDebuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.red,
                false,
                true);
        }
    }
}
