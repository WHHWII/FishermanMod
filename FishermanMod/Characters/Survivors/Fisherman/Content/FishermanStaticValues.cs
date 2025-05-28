using System;
using System.Numerics;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanStaticValues
    {
        //passive
        public const float hookMaxMass = 700f;
        public const float hookBleedCoefficient = 0.5f;
        public const float hookFlinchChance = 0.75f;
        public const float baseHookHitStop = 0.04f;
        public static float hitStopMod = 1f;
        public static float CurHitStop
        {
            get { return baseHookHitStop * hitStopMod; }
        }

        //primary
        public const float swipeDamageCoefficient = 2.6f;
        public const float stabDamageCoefficient = 2.6f;

        //secondary
        public const float castDamageCoefficient = 3.85f;

        //utility
        public const float shantyCannonDamage = 3.5f;
        public const float shantyCannonFlinchChance = 0.2f;

        public const float whaleMissleDotDamage = 0.1f;
        public const float whaleMissleDotInterval = 0.1f;

        //special
        public const float hookbombDamageCoefficient = 5f;
        public const float bombletBaseLifetime = 1.5f;


        public const float bottleDamageCoefficient = 1.5f;
        public const float bottleDamageBuff = 0.25f;
        public const float bottleBuffDuration = 16f;
        public const float bottleUppercutDamageCoefficient = 4f;
        public static readonly float[] bottleUppercutForceYZ = { 20, 30 };
    }
}