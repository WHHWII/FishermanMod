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
        public override void OnEnter()
        {
            base.OnEnter();
        }
        public override void OnExit()
        {


            base.OnExit();
        }
        public override void FixedUpdate()
        {
            SkillObjectTracker objTracker = characterBody.GetComponent<SkillObjectTracker>();

            if (base.isAuthority)
            {
                if (base.fixedAge >= chargeDestroyThreshold)
                {
                    objTracker.DestroyAllPlatforms();
                    outer.SetNextStateToMain();

                }
                else if (!IsKeyDownAuthority())
                {
                    PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", 0.65f);
                    if (!objTracker.platformPosTargetIndicator)
                    {
                        objTracker.platformPosTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
                    }

                    float range = 50;

                    Ray aimray = GetAimRay();
                    aimray.origin = transform.position + Vector3.up + aimray.direction;
                    //TODO make ping ignore platform

                    RaycastHit[] hitInfos = Physics.RaycastAll(aimray, range, RoR2.LayerIndex.world.mask | ~RoR2.LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
                    List<RaycastHit> tits = new List<RaycastHit>();
                    if (hitInfos.Length > 0)
                    {
                        tits.AddRange(hitInfos);
                        tits.RemoveAll((x) => PlatformMinionController.allDeployedPlatforms.Contains(x.transform.gameObject) || x.transform.name.Contains("Shanty"));
                        tits.Sort((a, b) => a.distance.CompareTo(b.distance));
                    }
                    // if still popualted
                    if (tits.Count > 0)
                    {
                        Log.Debug($" Direct Skill Impact: {tits[0].transform.name}");
                        objTracker.platformPosTargetIndicator.transform.position = tits[0].point + tits[0].normal * commandPointOffset;
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
