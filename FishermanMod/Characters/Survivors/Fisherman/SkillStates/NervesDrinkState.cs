using EntityStates;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman;
using R2API;
using RoR2;
using RoR2.Audio;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    //TODO: it would be really funny if you could hold the button to drink up to your max stocks, then the throw state would throw a bottle for each
    internal class NervesDrinkState : BaseSkillState
    {

        protected string uppercutHitbox = "UppercutGroup";
        protected DamageType damageType = DamageType.Generic;
        
        protected float damageCoefficient = FishermanStaticValues.bottleUppercutDamageCoefficient;
        protected float procCoefficient = 1f;
        protected float pushForce = 300f;
        protected Vector3 bonusForce = Vector3.zero;
        protected float baseDuration = 1f;

        protected float attackStartPercentTime = 0.1f;
        protected float attackEndPercentTime = 0.4f;

        protected float earlyExitPercentTime = 2f;

        protected float hitStopDuration = 0.012f;
        protected float attackRecoil = 0.75f;
        protected float hitHopVelocity = 4f;

        protected string swingSoundString = "";
        protected string hitSoundString = "";
        protected string muzzleString = "SwingCenter";
        protected string playbackRateParam = "SpecialDrink.playbackRate";
        protected GameObject swingEffectPrefab;
        protected GameObject hitEffectPrefab;
        protected NetworkSoundEventIndex impactSound;

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
        public static float duration = 0.5f;

        public static string dodgeSoundString = "HenryRoll";



        public override void OnEnter()
        {
            base.OnEnter();
            swingEffectPrefab = FishermanAssets.uppercutEffect;

            duration = baseDuration / attackSpeedStat;
            animator = GetModelAnimator();
            StartAimMode(0.5f + duration, false);

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
            attack.isCrit = RollCrit();
            attack.impactSound = impactSound;
            attack.hitBoxGroup = FindHitBoxGroup(uppercutHitbox);
            attack.AddModdedDamageType(DamageTypes.FishermanGrantSteady);
            attack.AddModdedDamageType(DamageTypes.FishermanUppercut);
            

            ChildLocator childLocator = characterBody.modelLocator.modelTransform.gameObject.GetComponent<ChildLocator>();
            childLocator.FindChild("Drink").gameObject.SetActive(true);
            PlayAnimation("Gesture, Override", "SpecialDrink", "SpecialDrink.playbackRate", duration);
            //Util.PlaySound(dodgeSoundString, gameObject);
            base.skillLocator.special.SetSkillOverride(gameObject, FishermanSurvivor.specialThrowFlask, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
        }


        //public override void FixedUpdate()
        //{
        //    base.FixedUpdate();
        //    if(fixedAge > duration * 3)
        //    {
        //        outer.SetNextState(new NervesThrowState());
        //    }
        //}

        //public override void OnExit()
        //{
        //    base.OnExit();

        //}

        private void AddBuffForeachDebuff()
        {
            //BuffDef[] allbuffs = BuffCatalog.buffDefs;
            //foreach(var buff in allbuffs)
            //{
            //    Log.Debug($"{buff.name}: {buff.buffIndex}");
            //}

            int debuffs = 1; //always gives one stack
            BuffIndex[] debuffBuffIndices = BuffCatalog.debuffBuffIndices;
            foreach (BuffIndex buffType in debuffBuffIndices)
            {
                if (characterBody.HasBuff(buffType))
                {
                    debuffs += characterBody.GetBuffCount(buffType);
                }
            }
            DotController dotController = DotController.FindDotController(gameObject);
            if (dotController)
            {
                for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                {
                    if (dotController.HasDotActive(dotIndex))
                    {
                        ++debuffs;
                    }
                }
            }
            if(characterBody.HasBuff(RoR2.RoR2Content.Buffs.VoidFogMild) || characterBody.HasBuff(RoR2.RoR2Content.Buffs.VoidFogStrong))
            {
                ++debuffs;
            }
            debuffs += characterBody.inventory.GetItemCount(RoR2.RoR2Content.Items.TonicAffliction); //lunar item debuffs?
            debuffs += characterBody.inventory.GetTotalItemCountOfTier(ItemTier.Lunar);
            debuffs -= characterBody.inventory.GetItemCount(RoR2.RoR2Content.Items.LunarTrinket);
            if (characterBody.HasBuff(RoR2.RoR2Content.Buffs.Nullified))
            {
                characterBody.RemoveBuff(RoR2.RoR2Content.Buffs.Nullified);
                if (debuffs >= 14)
                {
                    characterBody.AddBuff(RoR2.RoR2Content.Buffs.Slow50);
                }
                else if( debuffs >= 7)
                {
                    characterBody.AddBuff(RoR2.RoR2Content.Buffs.Slow60);
                }
                else
                {
                    characterBody.AddBuff(RoR2.RoR2Content.Buffs.Slow80);
                }
            }

            for (int i = 0; i < debuffs; i++) characterBody.AddTimedBuff(FishermanBuffs.steadyNervesBuff, FishermanStaticValues.bottleBuffDuration);
            characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.1f * duration);

            
        }



        public int swingIndex;

        protected static Transform hitBoxOrienter;

        /// //////////////////////////////////////////////////////////////////

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

            ApplyHitstop();
            base.characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, 0.5f);

        }
        // RoR2/Base/Merc/MercSwordUppercutSlash.prefab


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
                if (hitBoxOrienter == null) hitBoxOrienter = characterBody.modelLocator.modelTransform.gameObject.GetComponent<ChildLocator>().FindChild("SwingPivot");
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
                outer.SetNextState(new NervesThrowState());
                return;
            }
        }

        private void RemoveHitstop()
        {
            ConsumeHitStopCachedState(hitStopCachedState, characterMotor, animator);
            inHitPause = false;
            characterMotor.velocity = storedVelocity;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (stopwatch >= duration * earlyExitPercentTime)
            {
                return InterruptPriority.Any;
            }
            return InterruptPriority.Skill;
        }

        //public override void OnSerialize(NetworkWriter writer)
        //{
        //    base.OnSerialize(writer);
        //}

        //public override void OnDeserialize(NetworkReader reader)
        //{
        //    base.OnDeserialize(reader);
        //}


    }
}

