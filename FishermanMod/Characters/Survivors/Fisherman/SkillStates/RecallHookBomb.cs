using EntityStates;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Modules;
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
                base.skillLocator.special.UnsetSkillOverride(gameObject, FishermanSurvivor.specialRecallHookBomb, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                base.skillLocator.special.DeductStock(1); // may change this to deduct all stocks if all stocks are fired at once.
                gameObject.GetComponent<SkillObjectTracker>().HookAllBombs();
                
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
