using RoR2;
using FishermanMod.Modules.Achievements;

namespace FishermanMod.Survivors.Fisherman.Achievements
{
    //automatically creates language tokens "ACHIEVMENT_{identifier.ToUpper()}_NAME" and "ACHIEVMENT_{identifier.ToUpper()}_DESCRIPTION" 
    [RegisterAchievement(identifier, unlockableIdentifier, null, 0)]
    public class FishermanMasteryAchievement : BaseMasteryAchievement
    {
        public const string identifier = FishermanSurvivor.FISHERMAN_PREFIX + "masteryAchievement";
        public const string unlockableIdentifier = FishermanSurvivor.FISHERMAN_PREFIX + "masteryUnlockable";

        public override string RequiredCharacterBody => FishermanSurvivor.instance.bodyName;

        //difficulty coeff 3 is monsoon. 3.5 is typhoon for grandmastery skins
        public override float RequiredDifficultyCoefficient => 3;
    }
}