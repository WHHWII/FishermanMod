using FishermanMod.Modules;
using FishermanMod.Modules.BaseStates;
using R2API;
using RoR2;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class SlashCombo : BaseMeleeAttack
    {
        public float drunkDeviation = 0;
        public int drunkBuffCnt = 0;
        public override void OnEnter()
        {
            if (characterBody.HasBuff(FishermanBuffs.steadyNervesBuff))
            {
                drunkBuffCnt = characterBody.GetBuffCount(FishermanBuffs.steadyNervesBuff);
                drunkDeviation = Mathf.Clamp(UnityEngine.Random.Range(-drunkBuffCnt * 0.001f, drunkBuffCnt * 0.001f), -0.5f, 0.5f);
            }
            damageType = DamageType.Generic;
            damageCoefficient = FishermanStaticValues.swipeDamageCoefficient + drunkDeviation;
            procCoefficient = 1f + drunkDeviation;
            pushForce = 300f + drunkDeviation;
            bonusForce = Vector3.zero + drunkDeviation * Vector3.one;

            //0-1 multiplier of baseduration, used to time when the hitbox is out (usually based on the run time of the animation)
            //for example, if attackStartPercentTime is 0.5, the attack will start hitting halfway through the ability. if baseduration is 3 seconds, the attack will start happening at 1.5 seconds
            attackStartPercentTime = 0.2f;
            attackEndPercentTime = 0.4f;

            //this is the point at which the attack can be interrupted by itself, continuing a combo
            earlyExitPercentTime = 0.6f;




            hitStopDuration = 0.012f + drunkDeviation;
            attackRecoil = 0.5f + drunkDeviation;
            hitHopVelocity = 4f + drunkDeviation;

            swingSoundString = "HenrySwordSwing";
            hitSoundString = "";
            switch (swingIndex)
            {
                case 0:
                    baseDuration = 1.2f + drunkDeviation;

                    damageCoefficient = FishermanStaticValues.stabDamageCoefficient;
                    
                    attackStartPercentTime = 0.3f + drunkDeviation;
                    attackEndPercentTime = 0.5f + drunkDeviation;
                    earlyExitPercentTime = 0.6f + drunkDeviation;
                    muzzleString = "StabMuzzle";
                    swingEffectPrefab = FishermanAssets.swordStabEffect;
                    break;

                case 1:
                    baseDuration = 0.75f + drunkDeviation;

                    damageCoefficient = FishermanStaticValues.swipeDamageCoefficient;
                    muzzleString = "SwingLeftMuzzle";
                    attackStartPercentTime = 0.2f + drunkDeviation;
                    attackEndPercentTime = 0.4f + drunkDeviation;
                    earlyExitPercentTime = 0.6f + drunkDeviation;
                    swingEffectPrefab = FishermanAssets.swordSwingEffect;
                    break;
                case 2:
                    baseDuration = 0.75f + drunkDeviation;

                    damageCoefficient = FishermanStaticValues.swipeDamageCoefficient;
                    muzzleString = "SwingRightMuzzle";
                    attackStartPercentTime = 0.2f + drunkDeviation;
                    attackEndPercentTime = 0.4f + drunkDeviation;
                    earlyExitPercentTime = 0.6f + drunkDeviation;
                    swingEffectPrefab = FishermanAssets.swordSwingEffect;
                    break;
            }
            playbackRateParam = "PolePrimary.playbackRate";
            hitEffectPrefab = FishermanAssets.swordHitImpactEffect;

            impactSound = FishermanAssets.swordHitSoundEvent.index;

            base.OnEnter();
        }

        protected override void PlayAttackAnimation()
        {
            if (swingIndex == 0)
            {
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab, gameObject, muzzleString, false);
                PlayAnimation("Gesture, Override", "PolePrimary" + (3), playbackRateParam, 1.8f);
            }
            else if (swingIndex == 1)
            {
                //PlayAnimation("Gesture, Override", "PolePrimary" + (1), "PolePrimary.playbackRate", 1.8f);
                PlayCrossfade("Gesture, Override", "PolePrimary" + (1), playbackRateParam, duration, 0.1f * duration);
            }
            else
            {
                //PlayAnimation("Gesture, Override", "PolePrimary" + (2), "PolePrimary.playbackRate", 1.8f);
                PlayCrossfade("Gesture, Override", "PolePrimary" + (2), playbackRateParam, duration, 0.1f * duration);
            }

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