using EntityStates;
using FishermanMod.Survivors.Fisherman;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class DepricatedWhaleProjectile : GenericProjectileBaseState
    {

        public static float BaseDuration = 0.65f;
        //delays for projectiles feel absolute ass so only do this if you know what you're doing, otherwise it's best to keep it at 0
        public static float BaseDelayDuration = 0.0f;

        //public static float DamageCoefficient = Fisherman.FishermanStaticValues.whaleMissleDamage;

        public override void OnEnter()
        {
            projectilePrefab = FishermanAssets.whaleMisslePrefab;
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
                PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", duration);
            }
        }
        public override void OnExit() 
        {
            base.OnExit();
        }

    }
}
