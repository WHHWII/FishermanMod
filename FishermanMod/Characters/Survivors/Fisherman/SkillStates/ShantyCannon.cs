using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using EntityStates;
using static UnityEngine.ParticleSystem.PlaybackState;
using RoR2;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    internal class ShantyCannon : GenericProjectileBaseState
    {
        public static float BaseDuration = 0.65f;
        //delays for projectiles feel absolute ass so only do this if you know what you're doing, otherwise it's best to keep it at 0
        public static float BaseDelayDuration = 0.0f;

        public static float DamageCoefficient = Fisherman.FishermanStaticValues.shantyCannonDamage;

        public override void OnEnter()
        {
            projectilePrefab = FishermanAssets.shantyCannonShotPrefab;
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
            //outer.SetNextStateToMain();
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
            //TODO correct anim for platform
            if (GetModelAnimator())
            {
                PlayAnimation("Gesture, Override", "ThrowBomb", "ThrowBomb.playbackRate", this.duration);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
