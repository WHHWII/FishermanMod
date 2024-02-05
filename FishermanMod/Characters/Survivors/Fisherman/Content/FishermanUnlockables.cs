using FishermanMod.Survivors.Fisherman.Achievements;
using RoR2;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanUnlockables
    {
        public static UnlockableDef characterUnlockableDef = null;
        public static UnlockableDef masterySkinUnlockableDef = null;

        public static void Init()
        {
            masterySkinUnlockableDef = Modules.Content.CreateAndAddUnlockbleDef(
                FishermanMasteryAchievement.unlockableIdentifier,
                Modules.Tokens.GetAchievementNameToken(FishermanMasteryAchievement.identifier),
                FishermanSurvivor.instance.assetBundle.LoadAsset<Sprite>("texMasteryAchievement"));
        }
    }
}
