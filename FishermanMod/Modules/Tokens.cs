using UnityEngine;

namespace FishermanMod.Modules
{
    internal static class Tokens
    {
        public const string agilePrefix = "<style=cIsUtility>Agile.</style>";

        public static string HookText(string text)
        {
            return $"<color=#d299ff>{text}</color>";
        }

        public static string FishermanText(string text, int i)
        {
            i = Mathf.Clamp(i, 0, 4);
            string[] colors = new string[5] { "#367987", "#23677f", "#165475", "#124269", "#152f5b" };
            return $"<color={colors[i]}>{text}</color>";
        }
        public static string DamageText(string text)
        {
            return $"<style=cIsDamage>{text}</style>";
        }
        public static string DamageValueText(float value)
        {
            return $"<style=cIsDamage>{value * 100}% damage</style>";
        }
        public static string UtilityText(string text)
        {
            return $"<style=cIsUtility>{text}</style>";
        }
        public static string RedText(string text) => HealthText(text);
        public static string HealthText(string text)
        {
            return $"<style=cIsHealth>{text}</style>";
        }
        public static string KeywordText(string keyword, string sub)
        {
            return $"<style=cKeywordName>{keyword}</style><style=cSub>{sub}</style>";
        }
        public static string ScepterDescription(string desc)
        {
            return $"\n<color=#d299ff>SCEPTER: {desc}</color>";
        }

        public static string GetAchievementNameToken(string identifier)
        {
            return $"ACHIEVEMENT_{identifier.ToUpperInvariant()}_NAME";
        }
        public static string GetAchievementDescriptionToken(string identifier)
        {
            return $"ACHIEVEMENT_{identifier.ToUpperInvariant()}_DESCRIPTION";
        }
    }
}