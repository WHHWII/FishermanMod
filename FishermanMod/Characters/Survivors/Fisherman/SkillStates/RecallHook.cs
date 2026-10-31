using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman;
using IL.RoR2;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.WSA;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    public class RecallHook : BaseSkillState
    {
        public override void OnEnter()
        {
            // NETWORKING: animations aren't synced by themselves, every client plays them from the state, so this sits outside the authority check
            PlayAnimation("Gesture, Override", "SecondaryCastRecall", "SecondaryCast.playbackRate", 0.65f);
            if (base.isAuthority)
            {
                base.skillLocator.secondary.UnsetSkillOverride(gameObject, FishermanSurvivor.secondaryRecallFishHook, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                base.skillLocator.secondary.DeductStock(1); // may change this to deduct all stocks if all hooks are fired at once.
                SkillObjectTracker objt = characterBody.GetComponent<SkillObjectTracker>();
                if (objt)
                {
                    //objt.animator = base.GetModelAnimator();
                    // NETWORKING: hooks are simulated on the server, this forwards the recall there when we're a client
                    objt.RecallHooksAuthority();
                }


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
