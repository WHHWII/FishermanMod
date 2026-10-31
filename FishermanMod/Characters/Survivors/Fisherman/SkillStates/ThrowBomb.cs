using EntityStates;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman.Components;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class ThrowBomb : GenericProjectileBaseState
    {
        //TODO: Correct overide behavior so it isnt creating a new overide each time
        public static float BaseDuration = 0.65f;
        //delays for projectiles feel absolute ass so only do this if you know what you're doing, otherwise it's best to keep it at 0
        public static float BaseDelayDuration = 0.0f;

        public static float DamageCoefficient = Fisherman.FishermanStaticValues.hookbombDamageCoefficient;

        public override void OnEnter()
        {
            projectilePrefab = FishermanAssets.hookBombProjectilePrefab;
            // NETWORKING: this used to be written onto the projectile prefab, which only affected the machine that fired it
            // (a client's prefab edit never reaches the server, where the bomb is actually spawned).
            // The authority sends the count to the server before base.OnEnter fires the projectile; see SkillObjectTracker.pendingHookBombStocks.
            if (isAuthority)
            {
                int stocksConsumed = Mathf.Max(1, skillLocator.special.stock + 1);
                GetComponent<SkillObjectTracker>().SetPendingHookBombStocksAuthority(stocksConsumed);
            }
            skillLocator.special.DeductStock(skillLocator.special.stock);
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
                PlayAnimation("LeftArm, Override", "SpecialBomb", "SpecialBomb.playbackRate", this.duration);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            //PlayAnimation("LeftArm, Override", "SpecialBomb", "SpecialBomb.playbackRate", 0.65f);
            base.skillLocator.special.SetSkillOverride(gameObject, FishermanSurvivor.specialRecallHookBomb, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
            base.skillLocator.special.DeductStock(1);
        }
    }
}