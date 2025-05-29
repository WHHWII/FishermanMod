using EntityStates;
using IL.RoR2;
using IL.RoR2.CharacterAI;
using UnityEngine;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Characters.Survivors.Fisherman.Content;
using On.RoR2;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman.Components;
using System.Collections.Generic;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    internal class DirectPlatform : BaseSkillState
    {
        float commandPointOffset = 3;
        float chargeDestroyThreshold = 3;
        SkillObjectTracker objTracker;
        GameObject enemyObj = null;

        float range = 50;

        public override void OnEnter()
        {
            objTracker = characterBody.GetComponent<SkillObjectTracker>();

            base.OnEnter();
        }
        public override void OnExit()
        {


            base.OnExit();
        }
        public override void FixedUpdate()
        {

            if (base.isAuthority)
            {
                if (base.fixedAge >= chargeDestroyThreshold)
                {
                    objTracker.DestroyAllPlatforms();
                    outer.SetNextStateToMain();

                }
                else if (!IsKeyDownAuthority())
                {
                    if (enemyObj)
                    {
                        outer.SetNextStateToMain();
                        return;
                    }
                
                    PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", 0.65f);
                    if (!objTracker.platformPosTargetIndicator)
                    {
                        objTracker.platformPosTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
                    }



                    Ray aimray = GetAimRay();
                    aimray.origin = transform.position + Vector3.up + aimray.direction;

                    RaycastHit[] hitInfos = Physics.RaycastAll(aimray, range, RoR2.LayerIndex.world.mask | ~RoR2.LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
                    List<RaycastHit> possibleMoveTargets = new List<RaycastHit>();

                    if (hitInfos.Length > 0)
                    {
                        possibleMoveTargets.AddRange(hitInfos);
                        possibleMoveTargets.RemoveAll((x) => PlatformMinionController.allDeployedPlatforms.Contains(x.transform.gameObject) || x.transform.name.Contains("Shanty"));
                        possibleMoveTargets.Sort((a, b) => a.distance.CompareTo(b.distance));
                    }
                    // if still popualted
                    if (possibleMoveTargets.Count > 0)
                    {
                        for (int i = 0; i < possibleMoveTargets.Count; i++)
                        {
                            RoR2.CharacterBody cb = possibleMoveTargets[i].collider.GetComponent<RoR2.CharacterBody>();

                            if (cb)
                            {
                                enemyObj = cb.gameObject;
                            }
                            else
                            {
                                RoR2.HurtBox hb = possibleMoveTargets[i].collider.GetComponent<RoR2.HurtBox>();
                                if (hb)
                                {
                                    enemyObj = hb.healthComponent.body.gameObject;
                                }
                            }
                            if (enemyObj)
                            {
                                objTracker.SetAllPlatformEnemy(enemyObj);
                                Log.Debug("Set Platform Enemy");
                                break;
                            }
                        }

                        Log.Debug($" Direct Skill Impact: {possibleMoveTargets[0].transform.name}");


                        Vector3 targ = Vector3.zero;
                        if (enemyObj)
                        {
                            Vector3 platformPos = objTracker.deployedPlatforms[0].transform.position;
                            targ = platformPos + (enemyObj.transform.position - platformPos).normalized * Mathf.Max(Vector3.Distance(platformPos, enemyObj.transform.position) - range * 0.5f, 5);
                        }
                        else
                        {
                            targ = possibleMoveTargets[0].point + possibleMoveTargets[0].normal * commandPointOffset;
                        }
                        objTracker.platformPosTargetIndicator.transform.position = targ;
                    }
                    else
                    {
                        objTracker.platformPosTargetIndicator.transform.position = aimray.GetPoint(range);
                    }

                    if (!objTracker.DirectAllPlatforms())
                    {
                        skillLocator.utility.UnsetSkillOverride(gameObject, FishermanSurvivor.utilityDirectPlatform, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                        skillLocator.utility.DeductStock(1);
                    }



                    outer.SetNextStateToMain();
                }


            }


            base.FixedUpdate();


        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }



    }
}
