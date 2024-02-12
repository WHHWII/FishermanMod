using System;
using System.Collections.Generic;
using System.Text;
using IL.RoR2.Orbs;
using R2API;
using Rewired.UI.ControlMapper;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.Components
{
    public class HookBombController : MonoBehaviour
    {
        
        public ProjectileController controller;
        public ProjectileDamage damageComponent;
        public ProjectileProximityBeamController beamController;
        public GameObject owner;
        public ProjectileImpactExplosion explosionComponent;
        //public ProjectileExplosion explosionComponent;

        void Start()
        {
            FishermanSurvivor.SetDeployedHookBomb(this);
            owner = controller.owner;
        }

        void FixedUpdate()
        {
            foreach (var target in beamController.previousTargets)
            {
                
            }
        }

        public void HookAllTethers()
        {
            
            foreach (var target in beamController.previousTargets)
            {
                FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(
                    owner, gameObject,
                    damageComponent.damage,
                    transform.position,
                    target.body.mainHurtBox
                );
                if (target.body.HasBuff(FishermanBuffs.hookTetherDebuff))
                {
                    Log.Debug("Removing Tether");
                    target.body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                }
            }
            //do explosion

            explosionComponent.lifetime = 1.5f;
            explosionComponent.lifetimeAfterImpact = 1f;
            explosionComponent.destroyOnEnemy = false;
            
            

            //Destroy(gameObject);

        }

        void OnDestroy()
        {
            foreach (var target in beamController.previousTargets)
            {
                if (target.body.HasBuff(FishermanBuffs.hookTetherDebuff))
                {
                    target.body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                }
            }
        }


    }
}
