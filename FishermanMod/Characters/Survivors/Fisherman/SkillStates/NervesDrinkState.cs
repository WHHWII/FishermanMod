using EntityStates;
using FishermanMod.Survivors.Fisherman;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    //TODO: it would be really funny if you could hold the button to drink up to your max stocks, then the throw state would throw a bottle for each
    internal class NervesDrinkState : BaseSkillState
    {
        public static float duration = 0.5f;

        public static string dodgeSoundString = "HenryRoll";



        public override void OnEnter()
        {
            base.OnEnter();

            ChildLocator childLocator = characterBody.modelLocator.modelTransform.gameObject.GetComponent<ChildLocator>();
            childLocator.FindChild("Drink").gameObject.SetActive(true);
            PlayAnimation("Gesture, Override", "SpecialDrink", "SpecialDrink.playbackRate", duration);
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
            //BuffDef[] allbuffs = BuffCatalog.buffDefs;
            //foreach(var buff in allbuffs)
            //{
            //    Log.Debug($"{buff.name}: {buff.buffIndex}");
            //}

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
            if(characterBody.HasBuff(RoR2.RoR2Content.Buffs.VoidFogMild) || characterBody.HasBuff(RoR2.RoR2Content.Buffs.VoidFogStrong))
            {
                ++debuffs;
            }
            debuffs += characterBody.inventory.GetItemCount(RoR2.RoR2Content.Items.TonicAffliction); //lunar item debuffs?
            debuffs += characterBody.inventory.GetTotalItemCountOfTier(ItemTier.Lunar);
            debuffs -= characterBody.inventory.GetItemCount(RoR2.RoR2Content.Items.LunarTrinket);
            if (characterBody.HasBuff(RoR2.RoR2Content.Buffs.Nullified))
            {
                characterBody.RemoveBuff(RoR2.RoR2Content.Buffs.Nullified);
                if (debuffs >= 14)
                {
                    characterBody.AddBuff(RoR2.RoR2Content.Buffs.Slow50);
                }
                else if( debuffs >= 7)
                {
                    characterBody.AddBuff(RoR2.RoR2Content.Buffs.Slow60);
                }
                else
                {
                    characterBody.AddBuff(RoR2.RoR2Content.Buffs.Slow80);
                }
            }

            for (int i = 0; i < debuffs; i++) characterBody.AddTimedBuff(FishermanBuffs.steadyNervesBuff, 20f * duration);
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

