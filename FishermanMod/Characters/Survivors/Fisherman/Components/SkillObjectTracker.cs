using FishermanMod.Characters.Survivors.Fisherman.Content;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using On.RoR2.Skills;
using RoR2;
using RoR2.CharacterAI;
using RoR2.HudOverlay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.SoundAPI.Music.CustomMusicTrackDef;

namespace FishermanMod.Characters.Survivors.Fisherman.Components
{
    public class SkillObjectTracker : MonoBehaviour
    {
        public List<FishHookController> deployedHooks;
        public List<HookBombController> deployedBombs;
        public List<BombletController> deployedBomblets;
        public List<PlatformMinionController> deployedPlatforms;
        public GameObject platformPosTargetIndicator;
        public GameObject platformAimTargetIndicator;
        public RoR2.CharacterBody characterBody;
        public RoR2.CharacterMotor characterMotor;
        public Animator animator;
        public const string COMMAND_SKILL_DRIVER_NAME = "FollowCommand";
        public const string LEASH_SKILL_DRIVER_NAME = "LeashLeader";
        public const string STOP_SKILL_DRIVER_NAME = "StandStill";
        public Transform fishingPoleTip;
        OverlayController objectViewerOverlay;

        public void Start()
        {
            characterBody = GetComponent<RoR2.CharacterBody>();
            characterMotor = GetComponent<CharacterMotor>();
            animator = characterBody.modelLocator.modelTransform.GetComponent<Animator>();
            fishingPoleTip = characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("PoleEnd");

            objectViewerOverlay = HudOverlayManager.AddOverlay(this.gameObject, new OverlayCreationParams
            {
                prefab = FishermanAssets.objectViewerOverlay,
                childLocatorEntry = "ScopeContainer"
            });
        }

        public void ProccessPlatfromDeath()
        {
            platformAimTargetIndicator?.SetActive(false);
            characterBody.skillLocator.utility.UnsetSkillOverride(gameObject, FishermanSurvivor.utilityDirectPlatform, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
            characterBody.skillLocator.utility.DeductStock(1);
            deployedPlatforms.Clear();
        }

        public void RecallAllHooks()
        {
            foreach (FishHookController hook in deployedHooks)
            {
                if (hook) hook.StartCoroutine(hook.FlyBack());
            }

            deployedHooks.Clear();
        }

        public void HookAllBombs()
        {
            foreach (HookBombController bomb in deployedBombs)
            {
                if (bomb) bomb.HookAllTethers();
            }
            deployedBombs.Clear();
        }

        public bool DirectAllPlatforms()
        {
            List<PlatformMinionController> toRemove = new List<PlatformMinionController>();
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                //Debug.Log("Platform instance: " + platform);
                if (platform == null || !platform.characterBody.healthComponent.alive)
                {
                    toRemove.Add(platform);
                    Debug.Log("Platform was null. Removing");
                }
                else
                {
                    CharacterMaster master = platform.GetComponent<RoR2.CharacterBody>().master;
                    BaseAI ai = master.GetComponent<BaseAI>();
                    platformPosTargetIndicator.gameObject.SetActive(true);
                    ai.customTarget.gameObject = platformPosTargetIndicator;
                    ai.customTarget.lastKnownBullseyePosition = platformPosTargetIndicator.transform.position;
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.currentEnemy,
                        dominantSkillDriver = GetComponent<AISkillDriver>(),
                        separationSqrMagnitude = Vector3.Distance(ai.body.footPosition, ai.customTarget.gameObject.transform.position)
                    }); ;
                }

            }
            platformPosTargetIndicator.SetActive(true); // note that the target indicators will not work when there is more than one platform.
            deployedPlatforms.RemoveAll((x) => toRemove.Contains(x));
            if(deployedPlatforms.Count == 0)
            {
                return false;
            }
            toRemove.Clear();
            return true;
            
        }

        public void RegisterPlatform(PlatformMinionController newMinion)
        {
            if(deployedPlatforms.Count > 0)
            {
                DestroyAllPlatforms();
            }
            deployedPlatforms.Add(newMinion);
        }

        public void DestroyAllPlatforms()
        {
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                platform.characterBody.master.TrueKill();
            }
            ProccessPlatfromDeath();
        }

        public bool ModifyPlayformStock(int stocks)
        {
            if (deployedPlatforms.Count <= 0) return false;
            List<PlatformMinionController> toRemove = new List<PlatformMinionController>();
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                //Debug.Log("Platform instance: " + platform);
                if (platform == null || !platform.characterBody.healthComponent.alive)
                {
                    toRemove.Add(platform);
                    Debug.Log("Platform was null. Removing");
                }
                else
                {
                    
                    platform.characterBody.skillLocator.primary.SetBonusStockFromBody(stocks);
     
                }



                

            }
            deployedPlatforms.RemoveAll((x) => toRemove.Contains(x));
            if (deployedPlatforms.Count == 0)
            {
                return false;
            }
            toRemove.Clear();
            return true;
        }

        //pretend this list is populated somwhere in your setup
        List<RoR2.Skills.SkillDef> AllSpells = new List<RoR2.Skills.SkillDef>();
        //would convert this to your custom skilldef or wrapper later
        RoR2.Skills.SkillDef[] spellSlots = new RoR2.Skills.SkillDef[4];


       
        public void CleanupHookBombTethers(List<HealthComponent> previousTargets)
        {
            StartCoroutine(Cleanup());
            IEnumerator Cleanup()
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForFixedUpdate();
                for (int i = 0; i < previousTargets.Count; i++)
                {
                    if (previousTargets[i]?.body && previousTargets[i].body.HasBuff(FishermanBuffs.hookTetherDebuff))
                    {
                        previousTargets[i].body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                    }
                }
            }
        }

    }
}




