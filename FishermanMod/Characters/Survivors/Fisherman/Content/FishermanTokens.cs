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
            AddFishermanTokens();

            ////uncomment this to spit out a lanuage file with all the above tokens that people can translate
            ////make sure you set Language.usingLanguageFolder and printingEnabled to true
            //Language.PrintOutput("Henry.txt");
            ////refer to guide on how to build and distribute your mod with the proper folders
            ///
        }

        public static void AddFishermanTokens()
        {
            string prefix = FishermanSurvivor.FISHERMAN_PREFIX;

            string desc = "Fisherman is a utility focused survivor that forces enemies to fight on his terms.<color=#CCD3E0>"                                                    + Environment.NewLine + Environment.NewLine
             + "< ! > Debone can be used to snatch enemies out of the air and throw attackers out of position"                                                                   + Environment.NewLine + Environment.NewLine
             + "< ! > Cast can be used to grab faraway enemies, chests, drones and all sorts of things in order to clear stages quickly. It can also grab some of your abilties" + Environment.NewLine + Environment.NewLine
             + "< ! > The F153 Mobile Shanty plaform may not help you dodge attacks, but can help you navigate large gaps, sheer cliffs, and can provide aerial support."        + Environment.NewLine + Environment.NewLine
             + "< ! > Man o' War can be used to group up pesky flyers or throw enemies off stage."                                                                               + Environment.NewLine + Environment.NewLine
             + "< ! > Steady the Nerves is good for comboing enemies and staying alive."                                                                               + Environment.NewLine + Environment.NewLine;

            string outro = "..and so he left, with what little time he had.";
            string outroFailure = "..and so he vanished, lost at sea.";

            Language.Add(prefix + "NAME", "Fisherman");
            Language.Add(prefix + "DESCRIPTION", desc);
            Language.Add(prefix + "SUBTITLE", "Sickly Poacher");
            Language.Add(prefix + "LORE", "UES Safe Travels Brig Logs\r\n \r\nIncident Report\r\n \r\nDefendant became belligerent after breakfast, confirmed reports of alcohol ingestion and intoxication.\r\nAttempted to initiate fistfight with eleven fellow crew members, simultaneously.\r\nSecurity was unable to restrain Defendant.\r\nDefendant collapsed from intoxication and was moved to brig for detoxification protocols.\r\nLoader suit was required to move Defendant due to excess weight.\r\n \r\nNotes\r\nWhose idea was it to put this lardass on the crew? I've seen the guy's records. Old, drunkard, divorced, lazy. Is this where \"seniority\" gets you? Who cares about his naval record, he's a loose cannon! We saw what happened when he was drunk, and it can only get worse if we tell him he can't have any. Just keep this wino locked away until we get to the planet. Maybe we can \"accidentally\" leave him there when this rescue mission's over.\r\n \r\n \r\nIncident Report\r\n \r\nDefendant broke brig containment.\r\nPresumed cause: leaning on cell door caused loss of structural integrity.\r\nDefendant found after brief search, asleep atop the broken cell door, half outside of holding cell.\r\nLoader suit again required to move Defendant to new cell. Instructed not to lean on door.\r\n \r\nNotes\r\nThis is ridiculous, what is this guy MADE of? I'm not even sure we could control him if he really lost his cool. I'm starting to be thankful he spends most of his days drinking and napping.\r\n \r\n \r\nIncident Report\r\n \r\nDefendant became belligerent inside holding cell.\r\nPresumed cause: Alcohol withdrawal.\r\nDefendant noted saying, \"I wanna make ma phone call! Get me on the horn with the gosh dern Cap'n. We's buds, he'll sort this out!\"\r\nCaptain briefed on incidents involving Defendant, noted to have heavily sighed and instructed officers to \"throw away the key\"\r\nDefendant remanded to brig until further notice.\r\n \r\nNotes\r\n \r\n \r\nIncident Report\r\n \r\nDefendant broke brig containment.\r\nSecurity Officer noted broken cell door and misplacement of Defendant.\r\nNote found in Defendant's cell, reading \"Gone Fishin'\".\r\nSecurity alerted to Defendant's escape.\r\nSearch conducted ship-wide for Defendant.\r\nSecurity footage showed Defendant moving to cargo hold.\r\nConfirmed misplacement of items in cargo hold:\r\n \r\n\t1 (one) F135 Mobile Survey Platform\r\n\t6 (six) Cargo pallets containing crew's alcohol rations for entire mission\r\n\t1 (one) Cargo pallet containing food rations for planet-side crews\r\n \r\nSecurity mainframe notes unauthorized drop pod bay activity.\r\nConfirmed misplacement of 1 (one) drop pod.\r\nDefendant has not been located.\r\n \r\nNotes\r\nI'm getting court martialed for this, aren't I?\r\n");
            Language.Add(prefix + "OUTRO_FLAVOR", outro);
            Language.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Keywords
            LanguageAPI.Add(prefix + "KEYWORD_TREASURE", $" <style=cKeywordName>Treasure</style><style=cSub> Cast can {Tokens.HookText("Hook")} drones, items, chests, and more...");
            LanguageAPI.Add(prefix + "KEYWORD_NONLETHAL", $" <style=cKeywordName>Non-Lethal</style><style=cSub> Cannot kill.");
            LanguageAPI.Add(prefix + "KEYWORD_TETHER", $" <style=cKeywordName>Tether</style><style=cSub> Reduces movement speed by <style=cIsDamage>30%.</style>");
            LanguageAPI.Add(prefix + "KEYWORD_UNFINISHED", $" <style=cKeywordName>UNFINISHED</style><style=cSub>Ability is still in development, meaning it probably functions poorly and is likley to change");
            LanguageAPI.Add(prefix + "KEYWORD_DAUNTLESS", $" <style=cKeywordName>Regenerative</style><style=cSub>Heal for 5% of your maximum life over 0.5s");
            LanguageAPI.Add(prefix + "KEYWORD_SMACK", $" <style=cKeywordName>Smackable</style><style=cSub>This minon can be damaged and pushed by your abilities.");
            LanguageAPI.Add(prefix + "KEYWORD_LINKED", $"<style=cKeywordName>Soulbound</style><style=cSub>This minion does not inherit your items, but all damage they deal " + $"is treated as your own.</style>");
            LanguageAPI.Add(prefix + "KEYWORD_DIRECT", $"<style=cKeywordName>Directable</style><style=cSub> Re-Activate this ability to direct the platform to a targeted location. Hold the ability to destroy it.");


            #endregion Keyowrds



            #region Skins
            Language.Add(prefix + "MASTERY_SKIN_NAME", "Alternate");
            #endregion

            #region Passive
            Language.Add(prefix + "PASSIVE_HOOK_EFFECT_NAME", Tokens.FishermanText("The Hooks!",0));
            Language.Add(prefix + "PASSIVE_HOOK_EFFECT_DESCRIPTION", $"Certain attacks {Tokens.HookText("Hook")} fish, pulling them close. If a fish is too heavy to be {Tokens.HookText("Hooked")},  {Tokens.RedText("bleed")} them for {Tokens.RedText("120% damage.")} ");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_SLASH_NAME", Tokens.FishermanText("Debone",1));
            Language.Add(prefix + "PRIMARY_SLASH_DESCRIPTION", $"Thwack in front for <style=cIsDamage>{100f * FishermanStaticValues.swipeDamageCoefficient}% damage.</style> The 1st attack is a long range stab that {Tokens.HookText("Hooks")} fish.");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_GUN_NAME", Tokens.FishermanText("Cast",2));
            Language.Add(prefix + "SECONDARY_GUN_DESCRIPTION", $"<style=cIsUtility>Non-Lethal.</style> Charge up and cast out an arcing hook. Recall it to {Tokens.HookText("Hook")} fish for <style=cIsDamage>{100f * FishermanStaticValues.castDamageCoefficient}% damage.</style> Can {Tokens.HookText("Hook")} all kinds of <style=cIsUtility>Treasure...</style>");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_PLATFORM_NAME", Tokens.FishermanText("F153 Mobile Shanty Platform",3));
            Language.Add(prefix + "UTILITY_PLATFORM_DESCRIPTION", $"<style=cIsUtility>Soulbound.</style> Deploy a <style=cIsUtility>Directable</style> drone that <style=cIsUtility>you can stand on.</style> It slowly fires explosive rounds for <style=cIsDamage>{100f * FishermanStaticValues.shantyCannonDamage}% damage.</style>");
            Language.Add(prefix + "UTILITY_MINION_NAME", "Junior");

            Language.Add(prefix + "UTILITY_DIRECT_NAME", Tokens.FishermanText("Direct Platform", 3));
            Language.Add(prefix + "UTILITY_DIRECT_DESCRIPTION", $"Tap to direct the platform to move to a targeted location. Hold to Destroy it.");

            Language.Add(prefix + "UTILITY_WHALE_NAME", Tokens.FishermanText("Strange Friend", 3));
            Language.Add(prefix + "UTILITY_WHALE_DESCRIPTION", $"Release a <style=cIsUtility>Smackable</style> whale that <style=cIsUtility>inherits all your items.</style> Emits smog that deals <style=cIsDamage>{100f * 4f * FishermanStaticValues.whaleMissleDotDamage}% damage per second</style> for each enemy killed.");
            Language.Add(prefix + "UTILITY_WHALE_MINION_NAME", "Fisherman's Friend");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_BOMB_NAME", Tokens.FishermanText("Man o' War",4));
            Language.Add(prefix + "SPECIAL_BOMB_DESCRIPTION", $"Launch a sticky bomb that <style=cIsUtility>Tethers</style> fish. Re-Activate to {Tokens.HookText("Hook")} <style=cIsUtility>Tethered</style> fish inwards and detonate for <style=cIsDamage>2x{100f * FishermanStaticValues.hookbombDamageCoefficient}% damage.</style>");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_DRINK_NAME", Tokens.FishermanText("Steady The Nerves", 4));
            Language.Add(prefix + "SPECIAL_DRINK_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. <style=cIsDamage>Regenerative.</style> Perform a forceful uppercut for <style=cIsDamage>{100f * FishermanStaticValues.bottleUppercutDamageCoefficient}% damage</style>. Then throw a bottle for <style=cIsDamage>{100f * FishermanStaticValues.bottleDamageCoefficient}% damage.</style>");
            #endregion

            #region Achievements
            Language.Add(Tokens.GetAchievementNameToken(FishermanMasteryAchievement.identifier), "Fisherman: Mastery");
            Language.Add(Tokens.GetAchievementDescriptionToken(FishermanMasteryAchievement.identifier), "As Fisherman, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}
