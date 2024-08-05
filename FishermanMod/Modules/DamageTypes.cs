using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using R2API;
using R2API.Networking;
using RoR2;
using UnityEngine;
using UnityEngine.XR;

namespace FishermanMod.Modules
{
    internal static class DamageTypes
    {
        public static R2API.DamageAPI.ModdedDamageType FishermanTether;
        public static R2API.DamageAPI.ModdedDamageType FishermanHookPassive;
        public static R2API.DamageAPI.ModdedDamageType FishermanKnockup;



        public static void RegisterDamageTypes()
        {
            Log.Debug("Damage registered");
            FishermanTether = DamageAPI.ReserveDamageType();
            FishermanHookPassive = DamageAPI.ReserveDamageType();
            SetHooks();
        }
        private static void SetHooks()
        {
            //On.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            
        }

        private static void GlobalEventManager_onServerDamageDealt(DamageReport damageReport)
        {
            //orig( damageReport);

            DamageInfo damageInfo = damageReport.damageInfo;
            HealthComponent victim = damageReport.victim;

            bool flag = damageInfo.procCoefficient >= Mathf.Epsilon;

            if (damageInfo.HasModdedDamageType(FishermanTether))
            {
                victim.body.AddBuff(FishermanBuffs.hookTetherDebuff);
                victim.gameObject.AddComponent<HookBombTetherVisual>();
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
            }

            if (damageInfo.HasModdedDamageType(FishermanKnockup))
            {
                //apply knockup scaled with mass if victim has rigidbody. Do not apply knockup if victim is airborne.
                damageInfo.force = damageReport.victimBody.characterMotor.isGrounded ? (damageReport.victimBody.rigidbody && damageReport.victimBody.rigidbody.mass < 700 ? damageReport.victimBody.rigidbody.mass : 0.1f)* damageInfo.force : Vector3.zero;
                Log.Info($"[DamageTypes][Knockup] Damageinfo force: {damageInfo.force}");
                damageReport.victim.TakeDamageForce(damageInfo.force);
            }
        }

        //private static void SetStateOnHurt_OnTakeDamageServer(On.RoR2.SetStateOnHurt.orig_OnTakeDamageServer orig, SetStateOnHurt self, DamageReport damageReport)
        //{
            

        //}
    }
}
