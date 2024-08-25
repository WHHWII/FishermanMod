using FishermanMod.Modules;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanBuffs
    {
        // armor buff gained during roll
        public static BuffDef armorBuff;
        public static BuffDef hookImmunityBuff;
        public static BuffDef hookTauntDebuff;
        public static BuffDef hookTetherDebuff;
        public static BuffDef steadyNervesBuff;

        public static BuffDef fishermanWhaleFogDebuff;
        public static DotController.DotIndex fishermanWhaleFogDot;


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
            //hookTauntDebuff = Modules.Content.CreateAndAddBuff("HookTauntDebuff",
            //    LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
            //    Color.red,
            //    false,
            //    true);
            hookTetherDebuff = Modules.Content.CreateAndAddBuff("HookTetherDebuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.gray,
                false,
                true);

            steadyNervesBuff = Modules.Content.CreateAndAddBuff("SteadyNervesBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.blue,
                true,
                false);
            fishermanWhaleFogDebuff = Modules.Content.CreateAndAddBuff("FishermanWhaleFogDebuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite, //Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/VoidRaidCrab/texVoidRaidcrabWardWipeFog.png").WaitForCompletion().iconSprite, 
                Color.magenta,// RoR2/DLC1/VoidRaidCrab/texVoidRaidcrabWardWipeFog.png
                true,
                true
                );
            fishermanWhaleFogDot =             
                DotAPI.RegisterDotDef(new DotController.DotDef
                {
                    interval = FishermanStaticValues.whaleMissleDotInterval,
                    damageCoefficient = FishermanStaticValues.whaleMissleDotDamage,
                    damageColorIndex = DamageColorIndex.Void,
                    associatedBuff = fishermanWhaleFogDebuff,
                    resetTimerOnAdd = true
                });

          }
    }
}
