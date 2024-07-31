using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using R2API;
using R2API.Networking;
using RoR2;
using UnityEngine;

namespace FishermanMod.Modules
{
    internal static class DamageTypes
    {
        public static R2API.DamageAPI.ModdedDamageType TetherHook;
        public static R2API.DamageAPI.ModdedDamageType FishermanHookPassive;



        public static void RegisterDamageTypes()
        {
            Log.Debug("Damage registered");
            TetherHook = DamageAPI.ReserveDamageType();
            FishermanHookPassive = DamageAPI.ReserveDamageType();
            SetHooks();
        }
        private static void SetHooks()
        {
            On.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;

            
        }
        private static void SetStateOnHurt_OnTakeDamageServer(On.RoR2.SetStateOnHurt.orig_OnTakeDamageServer orig, SetStateOnHurt self, DamageReport damageReport)
        {
            orig(self, damageReport);

            DamageInfo damageInfo = damageReport.damageInfo;
            HealthComponent victim = damageReport.victim;

            bool flag = damageInfo.procCoefficient >= Mathf.Epsilon;

            if (damageInfo.HasModdedDamageType(TetherHook))
            {
                Log.Debug("Tether damage detected");
                victim.body.AddBuff(FishermanBuffs.hookTetherDebuff);
                victim.gameObject.AddComponent<HookBombTetherVisual>();
                //self.SetStun(10);
            }

            if (damageInfo.HasModdedDamageType(FishermanHookPassive))
            {
                FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(
                    damageReport.attacker, 
                    damageInfo.inflictor,
                    damageInfo.damage, 
                    damageReport.attacker.transform.position,
                    damageReport.victimBody.mainHurtBox
                );


                //int result = 
                //if (result == 0)
                //{
                //    victim.ApplyDot(damageReport.attacker, DotController.DotIndex.Bleed,8, FishermanStaticValues.castDamageCoefficient);
                //}
                //self.SetStun(10);
            }

        }
    }
}
