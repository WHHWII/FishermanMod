using FishermanMod.Modules.BaseStates;
using R2API;
using RoR2;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class SlashCombo : BaseMeleeAttack
    {
        public override void OnEnter()
        {
            damageType = DamageType.Generic;
            damageCoefficient = FishermanStaticValues.swipeDamageCoefficient;
            procCoefficient = 1f;
            pushForce = 300f;
            bonusForce = Vector3.zero;
            baseDuration = 1f;

            //0-1 multiplier of baseduration, used to time when the hitbox is out (usually based on the run time of the animation)
            //for example, if attackStartPercentTime is 0.5, the attack will start hitting halfway through the ability. if baseduration is 3 seconds, the attack will start happening at 1.5 seconds
            attackStartPercentTime = 0.2f;
            attackEndPercentTime = 0.4f;

            //this is the point at which the attack can be interrupted by itself, continuing a combo
            earlyExitPercentTime = 0.6f;

            hitStopDuration = 0.012f;
            attackRecoil = 0.5f;
            hitHopVelocity = 4f;

            swingSoundString = "HenrySwordSwing";
            hitSoundString = "";
            switch (swingIndex)
            {
                case 0:
                    damageCoefficient = FishermanStaticValues.stabDamageCoefficient;
                    muzzleString = "Muzzle";
                    break;

                case 1:
                    damageCoefficient = FishermanStaticValues.swipeDamageCoefficient;
                    muzzleString = "SwingLeft";
                    break;
                case 2:
                    damageCoefficient = FishermanStaticValues.swipeDamageCoefficient;
                    muzzleString = "SwingRight";
                    break;
            }
            playbackRateParam = "Slash.playbackRate";
            swingEffectPrefab = FishermanAssets.swordSwingEffect;
            hitEffectPrefab = FishermanAssets.swordHitImpactEffect;

            impactSound = FishermanAssets.swordHitSoundEvent.index;

            base.OnEnter();
        }

        protected override void PlayAttackAnimation()
        {
            //if(swingIndex == 0)
            //{
            //    EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab, gameObject, muzzleString, false);
            //    PlayAnimation("LeftArm, Override", "ShootGun", "ShootGun.playbackRate", 1.8f);
            //}
            //else if(swingIndex == 1)
            //{
            //    PlayCrossfade("Gesture, Override", "Slash" + (1), playbackRateParam, duration, 0.1f * duration);
            //}
            //else
            //{
            //    PlayCrossfade("Gesture, Override", "Slash" + (2), playbackRateParam, duration, 0.1f * duration);
            //}
           
        }

        protected override void PlaySwingEffect()
        {
            base.PlaySwingEffect();
        }

        protected override void OnHitEnemyAuthority()
        {
            base.OnHitEnemyAuthority();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}