using EntityStates;
using IL.RoR2;
using IL.RoR2.CharacterAI;
using UnityEngine;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Characters.Survivors.Fisherman.Content;
using On.RoR2;
using FishermanMod.Characters.Survivors.Fisherman.Components;

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
                    objTracker.platformAimTargetIndicator?.SetActive(false);
                    skillLocator.utility.UnsetSkillOverride(gameObject, FishermanSurvivor.utilityDirectPlatform, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                    skillLocator.utility.DeductStock(1);
                    outer.SetNextStateToMain();

                }
                else if (!IsKeyDownAuthority())
                {
                    PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", 0.65f);


                    RaycastHit hitInfo;
                    Ray aimray = GetAimRay();
                    aimray.origin = transform.position + Vector3.up + aimray.direction;
                    //TODO make ping ignore platform

                    bool result = Physics.Raycast(aimray, out hitInfo, 50, RoR2.LayerIndex.world.mask | ~RoR2.LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
                    if (!objTracker.platformPosTargetIndicator)
                    {
                        objTracker.platformPosTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
                    }
                    if (result)
                    {

                        objTracker.platformPosTargetIndicator.transform.position = hitInfo.point + hitInfo.normal * commandPointOffset;
                    }
                    else
                    {
                        objTracker.platformPosTargetIndicator.transform.position = aimray.GetPoint(50);
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
