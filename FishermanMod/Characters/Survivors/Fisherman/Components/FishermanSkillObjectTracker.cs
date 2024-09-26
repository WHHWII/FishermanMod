using FishermanMod.Characters.Survivors.Fisherman.Content;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FishermanMod.Characters.Survivors.Fisherman.Components
{
    public class FishermanSkillObjectTracker : MonoBehaviour
    {
        public List<FishHookController> deployedHooks;
        public List<HookBombController> deployedBombs;
        public List<MovingPlatformController> deployedPlatforms;
        public GameObject platformPosTargetIndicator;
        public GameObject platformAimTargetIndicator;
        public RoR2.CharacterBody characterBody;

        public void Start()
        {
            characterBody = GetComponent<RoR2.CharacterBody>();
        }

        public void RecallAllHooks()
        {
            foreach (FishHookController hook in deployedHooks) 
            {
                hook.FlyBack();
            }
            deployedHooks.Clear();
        }

        public void HookAllBombs()
        {
            foreach (HookBombController bomb in deployedBombs)
            {
                bomb.HookAllTethers();
            }
            deployedBombs.Clear();
        }

        public void DirectAllPlatforms()
        {
            List<MovingPlatformController> toRemove = new List<MovingPlatformController>();
            foreach (MovingPlatformController platform in deployedPlatforms)
            {
                if(platform == null)
                {
                    toRemove.Add(platform);
                }
                else
                {
                    //Custom state is to allow the platoform to move to the specified target, AND shoot indipenently with ai.

                    ShantyMoveShootState customState = new ShantyMoveShootState();
                    customState.commandTarget = platformPosTargetIndicator.transform.position;
                    customState.startPosition = platform.transform.position;
                    customState.commanderObj = gameObject;
                    customState.objTracker = this;

                    CharacterMaster master = platform.GetComponent<RoR2.CharacterBody>().master;
                    master.GetComponent<RoR2.EntityStateMachine>().SetState(customState);
                    platform.GetComponent<RoR2.CharacterBody>().inventory.CopyItemsFrom(characterBody.inventory);

                    BaseAI baseAi = master.GetComponent<RoR2.CharacterAI.BaseAI>();
                    CharacterMaster owner = baseAi.GetComponent<AIOwnership>()?.ownerMaster;
                    for (int i = 0; i < baseAi.skillDrivers.Length; i++)
                    {
                        baseAi.skillDrivers[i].ignoreNodeGraph = true;
                    }
                }


            }
            platformPosTargetIndicator.SetActive(true); // note that the target indicators will not work when there is more than one platform.
            deployedPlatforms.RemoveAll((x) => toRemove.Contains(x));
            toRemove.Clear();

        }
    }

}
