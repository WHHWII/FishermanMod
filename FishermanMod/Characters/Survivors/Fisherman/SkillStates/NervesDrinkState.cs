using EntityStates;
using FishermanMod.Survivors.Fisherman;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    internal class NervesDrinkState : BaseSkillState
    {
        public static float duration = 0.5f;

        public static string dodgeSoundString = "HenryRoll";

        public override void OnEnter()
        {
            base.OnEnter();

            //PlayAnimation("FullBody, Override", "Roll", "Roll.playbackRate", duration);
            //Util.PlaySound(dodgeSoundString, gameObject);

            if (NetworkServer.active)
            {
                AddBuffForeachDebuff();
            }
            base.skillLocator.special.SetSkillOverride(this, FishermanSurvivor.specialThrowFlask, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
            base.skillLocator.special.DeductStock(1);
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override void OnExit()
        {
            base.OnExit();

        }

        private void AddBuffForeachDebuff()
        {
            int debuffs = 1; //always gives one stack
            BuffIndex[] debuffBuffIndices = BuffCatalog.debuffBuffIndices;
            foreach (BuffIndex buffType in debuffBuffIndices)
            {
                if (characterBody.HasBuff(buffType))
                {
                    debuffs += characterBody.GetBuffCount(buffType);
                }
            }
            DotController dotController = DotController.FindDotController(gameObject);
            if (dotController)
            {
                for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                {
                    if (dotController.HasDotActive(dotIndex))
                    {
                        ++debuffs;
                    }
                }
            }
            //characterBody.inventory.GetItemCount() //lunar item debuffs?
            for (int i = 0; i < debuffs; i++) characterBody.AddTimedBuff(FishermanBuffs.SteadyNervesBuff, 20f * duration);
            characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.1f * duration);

            
        }

        void ResistForce(DamageReport damageReport)
        {

        }

        //public override void OnSerialize(NetworkWriter writer)
        //{
        //    base.OnSerialize(writer);
        //    //writer.Write(forwardDirection);
        //}

        //public override void OnDeserialize(NetworkReader reader)
        //{
        //    base.OnDeserialize(reader);
        //    //forwardDirection = reader.ReadVector3();
        //}




    }
}

