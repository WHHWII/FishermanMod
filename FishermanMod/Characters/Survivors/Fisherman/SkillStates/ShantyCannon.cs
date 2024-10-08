using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using EntityStates;
using static UnityEngine.ParticleSystem.PlaybackState;
using RoR2;
using RoR2.Projectile;
using RoR2.CharacterAI;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    internal class ShantyCannon : BaseState
    {
        public static float baseDuration = 0.65f;
        //delays for projectiles feel absolute ass so only do this if you know what you're doing, otherwise it's best to keep it at 0
        public static float BaseDelayDuration = 0.0f;

        public static float DamageCoefficient = Fisherman.FishermanStaticValues.shantyCannonDamage;
        public CharacterBody ownerBody;

        public override void OnEnter()
        {
            projectilePrefab = FishermanAssets.shantyCannonShotPrefab;
            //base.effectPrefab = Modules.Assets.SomeMuzzleEffect;
            //targetmuzzle = "muzzleThrow"
            attackSoundString = "HenryBombThrow";
            baseDelayBeforeFiringProjectile = BaseDelayDuration;

            damageCoefficient = DamageCoefficient;

            AIOwnership aiOwnership = characterBody.master.GetComponent<AIOwnership>();
            ownerBody = aiOwnership?.ownerMaster.GetBody();
            Log.Debug($"aiowner : {aiOwnership} OwnerMasteR: {aiOwnership?.ownerMaster} Body: {ownerBody}");
            //proc coefficient is set on the components of the projectile prefab
            //base.projectilePitchBonus = 0;
            //base.minSpread = 0;
            //base.maxSpread = 0;

            recoilAmplitude = 0.1f;
            base.OnEnter();
            stopwatch = 0f;
            duration = baseDuration / attackSpeedStat;
            delayBeforeFiringProjectile = baseDelayBeforeFiringProjectile / attackSpeedStat;
            if ((bool)base.characterBody)
            {
                base.characterBody.SetAimTimer(2f);
            }
            PlayAnimation(duration);

            //outer.SetNextStateToMain();
        }

        public void PlayAnimation(float duration)
        {
            //TODO correct anim for platform
            //if (GetModelAnimator())
            //{
            //    PlayAnimation("Gesture, Override", "ThrowBomb", "ThrowBomb.playbackRate", this.duration);
            //}
        }


        [SerializeField]
        public GameObject effectPrefab;

        [SerializeField]
        public GameObject projectilePrefab;

        [SerializeField]
        public float damageCoefficient;

        [SerializeField]
        public float force;

        [SerializeField]
        public float minSpread;

        [SerializeField]
        public float maxSpread;

        [SerializeField]
        public float recoilAmplitude = 1f;

        [SerializeField]
        public string attackSoundString;

        [SerializeField]
        public float projectilePitchBonus;

        [SerializeField]
        public float baseDelayBeforeFiringProjectile;

        [SerializeField]
        public string targetMuzzle;

        [SerializeField]
        public float bloom;

        protected float stopwatch;

        protected float duration;

        protected float delayBeforeFiringProjectile;

        protected bool firedProjectile;


        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += GetDeltaTime();
            if (stopwatch >= delayBeforeFiringProjectile && !firedProjectile)
            {
                firedProjectile = true;
                FireProjectile();
                DoFireEffects();
            }
            if (stopwatch >= duration && base.isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        protected virtual void FireProjectile()
        {
            if (base.isAuthority)
            {
                Ray aimRay = GetAimRay();
                aimRay = ModifyProjectileAimRay(aimRay);
                aimRay.direction = Util.ApplySpread(aimRay.direction, minSpread, maxSpread, 1f, 1f, 0f, projectilePitchBonus);
                ProjectileManager.instance.FireProjectile(
                    projectilePrefab,
                    aimRay.origin,
                    Util.QuaternionSafeLookRotation(aimRay.direction),
                    ownerBody ? ownerBody.gameObject : gameObject, // owner
                    (ownerBody ? ownerBody.damage : damageStat) * damageCoefficient,
                    force,
                    Util.CheckRoll(
                        ownerBody ?  ownerBody.crit : critStat,
                        ownerBody ? ownerBody.master : base.characterBody.master
                    )
                );
            }
        }

        protected virtual Ray ModifyProjectileAimRay(Ray aimRay)
        {
            return aimRay;
        }

        protected virtual void DoFireEffects()
        {
            Util.PlaySound(attackSoundString, base.gameObject);
            AddRecoil(-2f * recoilAmplitude, -3f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
            if ((bool)effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
            }
            base.characterBody.AddSpreadBloom(bloom);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
