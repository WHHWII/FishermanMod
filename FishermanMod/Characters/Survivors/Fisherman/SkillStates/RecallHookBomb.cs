using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    internal class RecallHookBomb : BaseSkillState
    {
        public override void OnEnter()
        {
            if (base.isAuthority)
            {
                PlayAnimation("LeftArm, Override", "UtilityPlatform", "UtilityPlatform.playbackRate", 0.65f);
                base.skillLocator.special.SetSkillOverride(this, FishermanSurvivor.specialThrowHookBomb, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                base.skillLocator.special.DeductStock(1); // may change this to deduct all stocks if all stocks are fired at once.
                if (FishermanSurvivor.deployedHookBomb != null) FishermanSurvivor.deployedHookBomb.HookAllTethers();
                
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
