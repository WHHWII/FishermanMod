using System;
using System.Collections.Generic;
using System.Text;
using RoR2.Skills;
using RoR2;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class ShantyMinionCannonDef : SkillDef
    {
        private class InstanceData : BaseSkillInstanceData
        {
            public GenericSkill skillSlot;

            public void OnInventoryChanged()
            {
                skillSlot.RecalculateValues();
            }
        }

        public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
        {
            InstanceData instanceData = new InstanceData();
            instanceData.skillSlot = skillSlot;
            skillSlot.characterBody.master.minionOwnership.ownerMaster.GetBody().onInventoryChanged += instanceData.OnInventoryChanged;
            return instanceData;
        }

        public override void OnUnassigned(GenericSkill skillSlot)
        {
            skillSlot.characterBody.master.minionOwnership.ownerMaster.GetBody().onInventoryChanged -= ((InstanceData)skillSlot.skillInstanceData).OnInventoryChanged;
        }


        public override int GetRechargeStock(GenericSkill skillSlot)
        {
            return GetMaxStock(skillSlot);
        }
    }
}
