using EntityStates;
using IL.RoR2;
using IL.RoR2.CharacterAI;
using UnityEngine;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Characters.Survivors.Fisherman.Content;

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
                PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", 0.65f);


                RaycastHit hitInfo;
                Ray aimray = GetAimRay();
                bool result = Physics.Raycast(aimray,out hitInfo);
                if (!FishermanSurvivor.platformTarget)
                {
                    FishermanSurvivor.platformTarget = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }
                if (result)
                {
                    
                    FishermanSurvivor.platformTarget.transform.position = hitInfo.point + hitInfo.normal * commandPointOffset;
                }
                else
                {
                    FishermanSurvivor.platformTarget.transform.position = aimray.GetPoint(100);
                }
                //Custom state is to allow the platoform to move to the specified target, AND shoot indipenently with ai.
                ShantyMoveShootState customState = new ShantyMoveShootState();
                customState.CommandTarget = FishermanSurvivor.platformTarget.transform.position;
                FishermanSurvivor.deployedPlatform?.GetComponent<RoR2.CharacterBody>().master.GetComponent<RoR2.EntityStateMachine>().SetState(customState);
                FishermanSurvivor.deployedPlatform?.GetComponent<RoR2.CharacterBody>().inventory.CopyItemsFrom(characterBody.inventory);
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
