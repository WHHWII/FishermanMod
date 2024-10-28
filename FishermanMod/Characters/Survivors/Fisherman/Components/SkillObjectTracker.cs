using FishermanMod.Characters.Survivors.Fisherman.Content;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using On.RoR2.Skills;
using RoR2;
using RoR2.CharacterAI;
using System;
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
        public List<PlatformMinionController> deployedPlatforms;
        public GameObject platformPosTargetIndicator;
        public GameObject platformAimTargetIndicator;
        public RoR2.CharacterBody characterBody;
        public const string COMMAND_SKILL_DRIVER_NAME = "FollowCommand";
        public const string LEASH_SKILL_DRIVER_NAME = "LeashLeader";
        public const string STOP_SKILL_DRIVER_NAME = "StandStill";
        public Transform fishingPoleTip;

        public void Start()
        {
            characterBody = GetComponent<RoR2.CharacterBody>();
            fishingPoleTip = characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("PoleEnd");
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
                    ////Custom state is to allow the platoform to move to the specified target, AND shoot indipenently with ai.

                    //ShantyMoveShootState customState = new ShantyMoveShootState();
                    //customState.commandTarget = platformPosTargetIndicator.transform.position;
                    //customState.startPosition = platform.transform.position;
                    //customState.commanderObj = gameObject;
                    //customState.objTracker = this;
                    //customState.followingCommand = true;
                    //customState.leashing = false;

                    //CharacterMaster master = platform.GetComponent<RoR2.CharacterBody>().master;
                    //master.GetComponent<RoR2.EntityStateMachine>().SetState(customState);
                    ////platform.GetComponent<RoR2.CharacterBody>().inventory.CopyItemsFrom(characterBody.inventory);

                    //BaseAI baseAi = master.GetComponent<RoR2.CharacterAI.BaseAI>();
                    //CharacterMaster owner = baseAi.GetComponent<AIOwnership>()?.ownerMaster;
                    //for (int i = 0; i < baseAi.skillDrivers.Length; i++)
                    //{
                    //    baseAi.skillDrivers[i].ignoreNodeGraph = true;
                    //}

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

        public void DestroyAllPlatforms()
        {
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                platform.characterBody.master.TrueKill();
            }
            deployedPlatforms.Clear();
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


       


    }
}




