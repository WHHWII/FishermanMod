﻿using EntityStates;
using FishermanMod.Survivors.Fisherman;
using On.RoR2.CharacterAI;
using R2API;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.UI.GridLayoutGroup;

namespace FishermanMod.Modules.BaseStates
{
    public abstract class BaseMeleeAttack : BaseSkillState, SteppedSkillDef.IStepSetter
    {
        public int swingIndex;

        protected static Transform hitBoxOrienter;



        protected string swipeGroupName = "SwipeGroup";
        protected string stabGroupName = "StabGroup";
        protected DamageType damageType = DamageType.Generic;
        protected float damageCoefficient = 3.5f;
        protected float procCoefficient = 1f;
        protected float pushForce = 300f;
        protected Vector3 bonusForce = Vector3.zero;
        protected float baseDuration = 1f;

        protected float attackStartPercentTime = 0.2f;
        protected float attackEndPercentTime = 0.4f;

        protected float earlyExitPercentTime = 0.4f;

        protected float hitStopDuration = 0.012f;
        protected float attackRecoil = 0.75f;
        protected float hitHopVelocity = 4f;

        protected string swingSoundString = "";
        protected string hitSoundString = "";
        protected string muzzleString = "SwingCenter";
        protected string playbackRateParam = "PolePrimary.playbackRate";
        protected GameObject swingEffectPrefab;
        protected GameObject hitEffectPrefab;
        protected NetworkSoundEventIndex impactSound;

        public float duration;
        private bool hasFired;
        private float hitPauseTimer;
        private OverlapAttack attack;
        protected bool inHitPause;
        private bool hasHopped;
        protected float stopwatch;
        protected Animator animator;
        private HitStopCachedState hitStopCachedState;
        private Vector3 storedVelocity;
        List<HurtBox> hitresults = new List<HurtBox>();

        public override void OnEnter()
        {
            base.OnEnter();



            duration = baseDuration / attackSpeedStat;
            animator = GetModelAnimator();
            StartAimMode(0.5f + duration, false);

            PlayAttackAnimation();

            attack = new OverlapAttack();
            attack.damageType = damageType;
            attack.attacker = gameObject;
            attack.inflictor = gameObject;
            attack.teamIndex = GetTeam();
            attack.damage = damageCoefficient * damageStat;
            attack.procCoefficient = procCoefficient;
            attack.hitEffectPrefab = hitEffectPrefab;
            attack.forceVector = bonusForce;
            attack.pushAwayForce = pushForce;
            if (swingIndex > 0)
            {
                attack.damageType = DamageTypeCombo.GenericPrimary;
                attack.hitBoxGroup = FindHitBoxGroup(swipeGroupName);
            }
            else
            {
                attack.hitBoxGroup = FindHitBoxGroup(stabGroupName);
                attack.damageType = DamageTypeCombo.GenericPrimary;
                hitStopDuration = FishermanStaticValues.CurHitStop;
                attack.AddModdedDamageType(DamageTypes.FishermanHookPassive);
            }
            attack.isCrit = RollCrit();
            attack.impactSound = impactSound;
        }

        protected virtual void PlayAttackAnimation()
        {
            //PlayCrossfade("Gesture, Override", "Slash" + (1 + Mathf.Clamp01(swingIndex)), playbackRateParam, duration, 0.05f);
        }

        public override void OnExit()
        {
            if (inHitPause)
            {
                RemoveHitstop();
            }
            base.OnExit();
        }

        protected virtual void PlaySwingEffect()
        {
            EffectManager.SimpleMuzzleFlash(swingEffectPrefab, gameObject, muzzleString, true);
        }
        protected virtual void OnHitEnemyAuthority()
        {
            Util.PlaySound(hitSoundString, gameObject);
            
            if (!hasHopped)
            {
                if (characterMotor && !characterMotor.isGrounded && hitHopVelocity > 0f)
                {
                    SmallHop(characterMotor, hitHopVelocity);
                }

                hasHopped = true;
            }
            //if(swingIndex == 0)
            //{
            //    foreach (HurtBox hitResult in hitresults)
            //    {
            //        FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(attack.attacker,attack.inflictor, attack.damage, gameObject.transform.position, hitResult);
            //    }
            //}
            
            ApplyHitstop();
        }

        

        protected void ApplyHitstop()
        {
            if (!inHitPause && hitStopDuration > 0f)
            {
                storedVelocity = characterMotor.velocity;
                hitStopCachedState = CreateHitStopCachedState(characterMotor, animator, playbackRateParam);
                hitPauseTimer = hitStopDuration / attackSpeedStat;
                inHitPause = true;
            }
        }

        private void FireAttack()
        {
            if (isAuthority)
            {
                if(hitBoxOrienter == null) hitBoxOrienter = characterBody.modelLocator.modelTransform.gameObject.GetComponent<ChildLocator>().FindChild("SwingPivot");
                Vector3 direction = GetAimRay().direction;
                direction.y = Mathf.Max(direction.y, direction.y * 0.5f);
                hitBoxOrienter.rotation = Util.QuaternionSafeLookRotation(direction);


                if (attack.Fire(hitresults))
                {
                    OnHitEnemyAuthority();
                }
            }
        }

        private void EnterAttack()
        {
            hasFired = true;
            Util.PlayAttackSpeedSound(swingSoundString, gameObject, attackSpeedStat);

            PlaySwingEffect();

            if (isAuthority)
            {
                AddRecoil(-1f * attackRecoil, -2f * attackRecoil, -0.5f * attackRecoil, 0.5f * attackRecoil);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            hitPauseTimer -= Time.fixedDeltaTime;

            if (hitPauseTimer <= 0f && inHitPause)
            {
                RemoveHitstop();
            }

            if (!inHitPause)
            {
                stopwatch += Time.fixedDeltaTime;
            }
            else
            {
                if (characterMotor) characterMotor.velocity = Vector3.zero;
                if (animator) animator.SetFloat(playbackRateParam, 0f);
            }

            bool fireStarted = stopwatch >= duration * attackStartPercentTime;
            bool fireEnded = stopwatch >= duration * attackEndPercentTime;

            //to guarantee attack comes out if at high attack speed the stopwatch skips past the firing duration between frames
            if (fireStarted && !fireEnded || fireStarted && fireEnded && !hasFired)
            {
                if (!hasFired)
                {
                    EnterAttack();
                }
                FireAttack();
            }

            if (stopwatch >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        private void RemoveHitstop()
        {
            ConsumeHitStopCachedState(hitStopCachedState, characterMotor, animator);
            inHitPause = false;
            characterMotor.velocity = storedVelocity;
            FishermanStaticValues.hitStopMod = 1;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (stopwatch >= duration * earlyExitPercentTime)
            {
                return InterruptPriority.Any;
            }
            return InterruptPriority.Skill;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(swingIndex);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            swingIndex = reader.ReadInt32();
        }

        public void SetStep(int i)
        {
            swingIndex = i;
        }
    }
}