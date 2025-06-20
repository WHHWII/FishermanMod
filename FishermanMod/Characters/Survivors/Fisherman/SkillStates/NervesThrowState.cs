﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using EntityStates;
using static UnityEngine.ParticleSystem.PlaybackState;
using RoR2;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    internal class NervesThrowState : GenericProjectileBaseState
    {
        public static float BaseDuration = 0.65f;
        //delays for projectiles feel absolute ass so only do this if you know what you're doing, otherwise it's best to keep it at 0
        public static float BaseDelayDuration = 0.0f;

        public static float DamageCoefficient = Fisherman.FishermanStaticValues.bottleDamageCoefficient;

        public override void OnEnter()
        {
            projectilePrefab = FishermanAssets.bottleProjectilePrefab;
            //base.effectPrefab = Modules.Assets.SomeMuzzleEffect;
            //targetmuzzle = "muzzleThrow"
            attackSoundString = "HenryBombThrow";

            baseDuration = BaseDuration;
            baseDelayBeforeFiringProjectile = BaseDelayDuration;

            damageCoefficient = DamageCoefficient;

            //proc coefficient is set on the components of the projectile prefab
            force = 30f;

            //base.projectilePitchBonus = 0;
            //base.minSpread = 0;
            //base.maxSpread = 0;

            recoilAmplitude = 0.1f;
            bloom = 10;

            if (base.isAuthority)
            {
                ChildLocator childLocator = characterBody.modelLocator.modelTransform.gameObject.GetComponent<ChildLocator>();
                childLocator.FindChild("Drink").gameObject.SetActive(false);
                PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", -0.65f);
                base.skillLocator.special.UnsetSkillOverride(gameObject, FishermanSurvivor.specialThrowFlask, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                base.skillLocator.special.DeductStock(1); 
               

            }
            outer.SetNextStateToMain();
            base.OnEnter();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        public override void PlayAnimation(float duration)
        {

            if (GetModelAnimator())
            {
                PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", this.duration);
            }
        }

        public override void OnExit()
        {
            base.OnExit();  
        }
    }
}
