using System;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman.Achievements;
using R2API;

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

            string desc = "Fisherman is a utility focused survivor that forces enemies to fight on his terms.<color=#CCD3E0>"                                                    + Environment.NewLine + Environment.NewLine
             + "< ! > Debone can be used to snatch enemies out of the air and throw attackers out of position"                                                                   + Environment.NewLine + Environment.NewLine
             + "< ! > Cast can be used to grab faraway enemies, chests, drones and all sorts of things in order to clear stages quickly. It can even grab some of your abilties" + Environment.NewLine + Environment.NewLine
             + "< ! > The F153 Mobile Shanty plaform may not help you dodge attacks, but can help you navigate large gaps, sheer cliffs, and can provide aerial support."        + Environment.NewLine + Environment.NewLine
             + "< ! > Man o' War can be used to group up pesky flyers or throw enemies off stage while Steady the Nerves can be used to get krunked."                            + Environment.NewLine + Environment.NewLine;

            string outro = "..and so he left, with what little time he had.";
            string outroFailure = "..and so he vanished, lost at sea.";

            Language.Add(prefix + "NAME", "Fisherman");
            Language.Add(prefix + "DESCRIPTION", desc);
            Language.Add(prefix + "SUBTITLE", "Sickly Poacher");
            Language.Add(prefix + "LORE", "stinky");
            Language.Add(prefix + "OUTRO_FLAVOR", outro);
            Language.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Keywords
            LanguageAPI.Add(prefix + "KEYWORD_TREASURE", $" <style=cKeywordName>Treasure</style><style=cSub> Cast can {Tokens.HookText("Hook")} drones, items, small chests, and more...");
            LanguageAPI.Add(prefix + "KEYWORD_NONLETHAL", $" <style=cKeywordName>Non-Lethal</style><style=cSub> Cannot kill.");
            LanguageAPI.Add(prefix + "KEYWORD_TETHER", $" <style=cKeywordName>Tether</style><style=cSub> Reduces movement speed by <style=cIsDamage>30%.</style>");
            #endregion Keyowrds



            #region Skins
            Language.Add(prefix + "MASTERY_SKIN_NAME", "Alternate");
            #endregion

            #region Passive
            Language.Add(prefix + "PASSIVE_HOOK_EFFECT_NAME", Tokens.FishermanText("The Hooks!",0));
            Language.Add(prefix + "PASSIVE_HOOK_EFFECT_DESCRIPTION", $"The Fisherman can {Tokens.HookText("Hook")} fish, flinging them towards him. If a fish is too heavy to be {Tokens.HookText("Hooked")}, they are instead {Tokens.RedText("inflicted with bleed.")}");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_SLASH_NAME", Tokens.FishermanText("Debone",1));
            Language.Add(prefix + "PRIMARY_SLASH_DESCRIPTION", $"Thwack in front for <style=cIsDamage>{100f * FishermanStaticValues.swipeDamageCoefficient}% damage.</style> Every 3rd Attack is a long range stab that {Tokens.HookText("Hooks")} fish.");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_GUN_NAME", Tokens.FishermanText("Cast",2));
            Language.Add(prefix + "SECONDARY_GUN_DESCRIPTION", $"<style=cIsUtility>Non-Lethal.</style> Charge up and Cast out an arcing hook. Recall it to {Tokens.HookText("Hook")} fish for <style=cIsDamage>{100f * FishermanStaticValues.castDamageCoefficient}% damage.</style> Can {Tokens.HookText("Hook")} all kinds of <style=cIsUtility>Treasure...</style>");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_PLATFORM_NAME", Tokens.FishermanText("F153 Mobile Shanty Platform",3));
            Language.Add(prefix + "UTILITY_PLATFORM_DESCRIPTION", $"Deploy a drone that <style=cIsUtility>you can stand on.</style> It slowly fires explosive rounds for <style=cIsDamage>{100f * FishermanStaticValues.shantyCannonDamage}% damage.</style>");
            Language.Add(prefix + "UTILITY_MINION_NAME", "Chris");

            Language.Add(prefix + "UTILITY_WHALE_NAME", Tokens.FishermanText("Strange Friend", 3));
            Language.Add(prefix + "UTILITY_WHALE_DESCRIPTION", $"Summon a Baby Void Whale that continously damages enemies for <style=cIsDamage>{100f * FishermanStaticValues.whaleMissleDotDamage}% damage per second.</style> Kills increase its damage. <style=cIsUtility>Smack Him!.</style> ");
            Language.Add(prefix + "UTILITY_WHALE_MINION_NAME", "Son");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_BOMB_NAME", Tokens.FishermanText("Man o' War",4));
            Language.Add(prefix + "SPECIAL_BOMB_DESCRIPTION", $"Launch a sticky bomb that <style=cIsUtility>Tethers</style> fish. Re-Activate to {Tokens.HookText("Hook")} <style=cIsUtility>Tethered</style> fish inwards and detonate for <style=cIsDamage>{100f * FishermanStaticValues.hookbombDamageCoefficient}% damage.</style>");
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
