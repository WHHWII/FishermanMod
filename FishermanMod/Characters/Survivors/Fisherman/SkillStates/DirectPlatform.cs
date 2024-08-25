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
        public override void OnEnter()
        {
            if (base.isAuthority)
            {
                FishermanSkillObjectTracker objTracker = characterBody.GetComponent<FishermanSkillObjectTracker>();

                PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", 0.65f);


                RaycastHit hitInfo;
                Ray aimray = GetAimRay();
                aimray.origin = transform.position + Vector3.up + aimray.direction;
                //TODO make ping ignore platform

                bool result = Physics.Raycast(aimray, out hitInfo, 50, RoR2.LayerIndex.world.mask | ~RoR2.LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
                if (!objTracker.platformTargetIndicator)
                {
                    objTracker.platformTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
                }
                if (result)
                {

                    objTracker.platformTargetIndicator.transform.position = hitInfo.point + hitInfo.normal * commandPointOffset;
                }
                else
                {
                    objTracker.platformTargetIndicator.transform.position = aimray.GetPoint(50);
                }
                objTracker.DirectAllPlatforms();

            }
            outer.SetNextStateToMain();
            base.OnEnter();
        }
        public override void OnExit()
        {
            base.OnExit();

        }
        public override void FixedUpdate()
        {

            base.FixedUpdate();


        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }



    }
}
