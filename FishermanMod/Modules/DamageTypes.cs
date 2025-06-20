﻿using FishermanMod.Survivors.Fisherman;
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
        public static R2API.DamageAPI.ModdedDamageType FishermanWhaleFog;
        public static R2API.DamageAPI.ModdedDamageType FishermanUppercut;
        public static R2API.DamageAPI.ModdedDamageType FishermanGrantSteady;

        public static void RegisterDamageTypes()
        {
            //Log.Debug("Damage registered");
            FishermanTether = DamageAPI.ReserveDamageType();
            FishermanHookPassive = DamageAPI.ReserveDamageType();
            FishermanKnockup = DamageAPI.ReserveDamageType();
            FishermanWhaleFog = DamageAPI.ReserveDamageType();
            FishermanUppercut = DamageAPI.ReserveDamageType();
            FishermanGrantSteady = DamageAPI.ReserveDamageType();
            SetHooks();
        }
        private static void SetHooks()
        {
            //On.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;

        }

        private static void GlobalEventManager_onServerDamageDealt(DamageReport damageReport)
        {

            DamageInfo damageInfo = damageReport.damageInfo;
            HealthComponent victim = damageReport.victim;

            bool canProc = damageInfo.procCoefficient >= Mathf.Epsilon;

            if (damageInfo.HasModdedDamageType(FishermanTether))
            {
                //Log.Info("TetherDamageTypeInvoked");
                if (!victim.body || !damageReport.damageInfo.inflictor) return;
                victim.body.AddBuff(FishermanBuffs.hookTetherDebuff);
                HookBombTetherVisual tetherVisual = victim.gameObject.AddComponent<HookBombTetherVisual>();
                tetherVisual.lineTerminationObject = damageReport.damageInfo.inflictor;
            }

            if (damageInfo.HasModdedDamageType(FishermanHookPassive))
            {
                // Log.Info("HookDamageTypeInvoked");
                if (!victim.body) return;
                FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(
                    damageReport.attacker,
                    damageInfo.inflictor,
                    damageReport.attackerBody.footPosition,
                    damageReport.victimBody.mainHurtBox
                );
            }

            if (damageInfo.HasModdedDamageType(FishermanKnockup))
            {
                //Log.Info("KnockupDamageTypeInvoked");
                if (!victim.body) return;
                //apply knockup scaled with mass if victim has rigidbody. Do not apply knockup if victim is airborne.
                if (damageReport.victimBody && damageReport.victimBody.characterMotor && damageReport.victimBody.characterMotor.isGrounded)
                {
                    damageInfo.force = (damageReport.victimBody.rigidbody && damageReport.victimBody.rigidbody.mass < FishermanStaticValues.hookMaxMass ? damageReport.victimBody.rigidbody.mass : 0.1f) * damageInfo.force;
                    FishermanSurvivor.TryFlinch(damageReport.victimBody, FishermanStaticValues.shantyCannonFlinchChance);
                }
                else
                {
                    damageInfo.force = Vector3.zero;
                }
                //damageInfo.force = (bool)damageReport.victimBody.characterMotor?.isGrounded ? (damageReport.victimBody.rigidbody && damageReport.victimBody.rigidbody.mass < 700 ? damageReport.victimBody.rigidbody.mass : 0.1f)* damageInfo.force : Vector3.zero;
                //Log.Info($"[DamageTypes][Knockup] Damageinfo force: {damageInfo.force}");
                bool isHookImmune = victim.body.HasBuff(FishermanBuffs.hookImmunityBuff);
                if (!isHookImmune) damageReport.victim?.TakeDamageForce(damageInfo.force);
            }

            if (damageInfo.HasModdedDamageType(FishermanWhaleFog))
            {
                damageReport.victimBody.healthComponent.ApplyDot(damageReport.attacker, FishermanBuffs.fishermanWhaleFogDot);
            }
            if (damageInfo.HasModdedDamageType(FishermanUppercut))
            {
                DamageInfo diTemp = damageInfo;
                //counteract current velocity
                if (damageReport.victimBody.characterMotor) damageReport.victimBody.characterMotor.velocity = Vector3.zero;
                diTemp.damageType = DamageType.Generic;
                diTemp.damage = 0;
                diTemp.force = new Vector3(0, FishermanStaticValues.bottleUppercutForceYZ[0], 0) + damageReport.attackerBody.inputBank.aimDirection * FishermanStaticValues.bottleUppercutForceYZ[1];
                //diTemp.canRejectForce = false;

                float massScalar = 0;
                if (damageReport.victimBody.rigidbody)
                {
                    if (damageReport.victimBody.rigidbody.mass < FishermanStaticValues.hookMaxMass)
                    {
                        massScalar = damageReport.victimBody.rigidbody.mass;
                    }
                    else
                    {
                        massScalar = damageReport.victimBody.rigidbody.mass * Mathf.Min(damageReport.attackerBody.level / 40, 1);
                    }
                }
                diTemp.force *= massScalar;
                damageReport.victimBody.healthComponent.TakeDamageForce(diTemp);
            }
            if (damageInfo.HasModdedDamageType(FishermanGrantSteady))
            {

                CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
                if (body)
                {
                    if (!body.HasBuff(RoR2Content.Buffs.CrocoRegen.buffIndex) ||
                        (body.GetBuffCount(RoR2Content.Buffs.CrocoRegen.buffIndex) < 2 &&  damageInfo.inflictor.name.Contains(FishermanAssets.bottleProjectilePrefab.name))
                       )
                    {
                        body.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, 0.5f);
                    }

                }
                //if (NetworkServer.active)
                //{
                //    AddBuffForeachDebuff();
                //}
            }
        }

        //private static void SetStateOnHurt_OnTakeDamageServer(On.RoR2.SetStateOnHurt.orig_OnTakeDamageServer orig, SetStateOnHurt self, DamageReport damageReport)
        //{


        //}
    }
}
