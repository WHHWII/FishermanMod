using System;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman.Achievements;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanTokens
    {
        public static void Init()
        {
            AddHenryTokens();

            ////uncomment this to spit out a lanuage file with all the above tokens that people can translate
            ////make sure you set Language.usingLanguageFolder and printingEnabled to true
            //Language.PrintOutput("Henry.txt");
            ////refer to guide on how to build and distribute your mod with the proper folders
        }

        public static void AddHenryTokens()
        {
            string prefix = FishermanSurvivor.FISHERMAN_PREFIX;

            string desc = "Fisherman is a skilled fish who makes use of a wide arsenal of fish to take down his fish.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine
             + "< ! > fish is a good all-rounder while Boxing Gloves are better for laying a beatdown on more powerful foes." + Environment.NewLine + Environment.NewLine
             + "< ! > fish is a powerful anti air, with its low cooldown and high damage." + Environment.NewLine + Environment.NewLine
             + "< ! > fish has a lingering armor buff that helps to use it aggressively." + Environment.NewLine + Environment.NewLine
             + "< ! > fish can be used to wipe crowds with ease." + Environment.NewLine + Environment.NewLine;

            string outro = "..and so he left, searching for a new fish.";
            string outroFailure = "..and so he vanished, forever a fish.";

            Language.Add(prefix + "NAME", "Fisherman");
            Language.Add(prefix + "DESCRIPTION", desc);
            Language.Add(prefix + "SUBTITLE", "Fish Guy");
            Language.Add(prefix + "LORE", "stinky");
            Language.Add(prefix + "OUTRO_FLAVOR", outro);
            Language.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Skins
            Language.Add(prefix + "MASTERY_SKIN_NAME", "Alternate");
            #endregion

            #region Passive
            Language.Add(prefix + "PASSIVE_HOOK_EFFECT_NAME", Tokens.FishermanText("The Hooks!",0));
            Language.Add(prefix + "PASSIVE_HOOK_EFFECT_DESCRIPTION", $"Fisherman's abilities can {Tokens.HookText("Hook")} fish, flinging them towards him. If a fish is too heavy to be hooked, they are instead {Tokens.RedText("inflicted with bleed")}");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_SLASH_NAME", Tokens.FishermanText("Debone",1));
            Language.Add(prefix + "PRIMARY_SLASH_DESCRIPTION", $"Thwack in front for <style=cIsDamage>{100f * FishermanStaticValues.swipeDamageCoefficient}% damage.</style> Every 3rd Attack is a long range stab that <color=#d299ff>Hooks</color> fish for <style=cIsDamage>{100f * FishermanStaticValues.stabDamageCoefficient}% damage.</style>");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_GUN_NAME", Tokens.FishermanText("Cast",2));
            Language.Add(prefix + "SECONDARY_GUN_DESCRIPTION", $"<style=cIsUtility>Non-Lethal.</style> Charge up and Cast out an arcing hook. Recall it to <color=#d299ff>Hook</color> fish for <style=cIsDamage>{100f * FishermanStaticValues.gunDamageCoefficient}% damage.</style> Can <color=#d299ff>Hook</color> all kinds of <style=cIsUtility>Treasure...</style>");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_PLATFORM_NAME", Tokens.FishermanText("F153 Mobile Shanty Platform",3));
            Language.Add(prefix + "UTILITY_PLATFORM_DESCRIPTION", $"Deploy a drone that <style=cIsUtility>you can stand on.</style> It slowly fires explosive rounds for <style=cIsDamage>{100f * FishermanStaticValues.shantyCannonDamage}% damage.</style>");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_BOMB_NAME", Tokens.FishermanText("Man o' War",4));
            Language.Add(prefix + "SPECIAL_BOMB_DESCRIPTION", $"Launch a sticky bomb that tethers fish and <style=cIsUtility>slows them</style>. Re-Activate to {Tokens.HookText("Hook")} tethered fish inwards and detonate for <style=cIsDamage>{100f * FishermanStaticValues.bombDamageCoefficient}% damage.</style>");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_DRINK_NAME", Tokens.FishermanText("Steady The Nerves", 4));
            Language.Add(prefix + "SPECIAL_DRINK_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. Take a drink. Resist most negative effects and gain <style=cIsDamage>{100 * FishermanStaticValues.bottleDamageBuff}%</style> damage for each one on you. Recast to throw the bottle for <style=cIsDamage>{100f * FishermanStaticValues.bottleDamageCoefficient}% damage.</style>");
            #endregion

            #region Achievements
            Language.Add(Tokens.GetAchievementNameToken(FishermanMasteryAchievement.identifier), "Fisherman: Mastery");
            Language.Add(Tokens.GetAchievementDescriptionToken(FishermanMasteryAchievement.identifier), "As Fisherman, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}
