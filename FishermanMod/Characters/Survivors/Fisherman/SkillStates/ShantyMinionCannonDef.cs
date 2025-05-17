using System;
using System.Collections.Generic;
using System.Text;
using RoR2.Skills;
using RoR2;
using IL.RoR2.CharacterAI;
using UnityEngine;

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

            CharacterMaster master = skillSlot.characterBody.master;

            //master.GetComponent<MinionOwnership>().ownerMaster.GetBody().onInventoryChanged += instanceData.OnInventoryChanged;

            //skillSlot.characterBody.onInventoryChanged += instanceData.OnInventoryChanged;
            return instanceData;
        }

        public override void OnUnassigned(GenericSkill skillSlot)
        {
            //CharacterMaster master = skillSlot.characterBody.master;

            //master.GetComponent<MinionOwnership>().ownerMaster.GetBody().onInventoryChanged -= ((InstanceData)skillSlot.skillInstanceData).OnInventoryChanged;
        }

        public override int GetMaxStock(GenericSkill skillSlot)
        {
            CharacterMaster master = skillSlot?.characterBody?.master;
            if (master == null) return baseMaxStock;

            GenericSkill masterSkill = master.minionOwnership?.ownerMaster?.GetBody()?.skillLocator?.utility;
            if (masterSkill)
            {
                return masterSkill.maxStock;
            }

            return baseMaxStock;
        }

        public override float GetRechargeInterval(GenericSkill skillSlot)
        {
            CharacterMaster master = skillSlot.characterBody.master;
            if (master == null) return baseRechargeInterval;
            GenericSkill masterUtil = master.GetComponent<MinionOwnership>().ownerMaster.GetBody().skillLocator.utility;
            //Log.Debug($" master util fri {master.GetComponent<MinionOwnership>().ownerMaster.GetBody().skillLocator.utility.finalRechargeInterval}, bri {baseRechargeInterval}, final {MathF.Min(master.GetComponent<MinionOwnership>().ownerMaster.GetBody().skillLocator.utility.finalRechargeInterval * 2 * baseRechargeInterval, baseRechargeInterval)}  ");
            //hardcoded and stinky but the utilty will be the direct skill which has a base recharage interval of 0.5. so take that x2 to make 1 * our base of 5;
            return MathF.Min(Mathf.Max(0.5f, masterUtil.cooldownScale * baseRechargeInterval - masterUtil.flatCooldownReduction), baseRechargeInterval);
        }

        public override int GetRechargeStock(GenericSkill skillSlot)
        {
            int stocks = this.GetMaxStock(skillSlot)-skillSlot.stock;
            return stocks;
        }
    }
}
