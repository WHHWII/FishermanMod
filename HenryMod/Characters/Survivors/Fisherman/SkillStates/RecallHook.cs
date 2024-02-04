using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Survivors.Fisherman;
using IL.RoR2;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.WSA;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class RecallHook : BaseSkillState
    {
        public override void OnEnter()
        {
            Log.Debug("on enetr");
            if (base.isAuthority)
            {
                base.skillLocator.secondary.SetSkillOverride(this, FishermanSurvivor.secondaryFireFishHook, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                base.skillLocator.secondary.DeductStock(1); // may change this to deduct all stocks if all hooks are fired at once.
                FishermanSurvivor.deployedHook.FlyBack();
            }
            outer.SetNextStateToMain();
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
