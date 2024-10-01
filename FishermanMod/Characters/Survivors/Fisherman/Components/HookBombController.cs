using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Modules;
using IL.RoR2.Orbs;
using R2API;
using Rewired.UI.ControlMapper;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.Components
{
    //TODO: Tether visual components are not properly getting cleaned up
    //TODO: Recall hook bomb is throwing an error on activation
    public class HookBombController : MonoBehaviour
    {
        
        public ProjectileController controller;
        public ProjectileDamage damageComponent;
        public ProjectileProximityBeamController beamController;
        public GameObject owner;
        public ProjectileImpactExplosion explosionComponent;
        public ProjectileStickOnImpact stickComponent;
        public Rigidbody body;
        public AntiGravityForce antiGrav;
        public Vector3 lastVelocity;
        public DamageAPI.ModdedDamageTypeHolderComponent moddedDamageComp;
        public SphereCollider[] bombColliders;
        public bool wasStuckByHook;
        //public ProjectileExplosion explosionComponent;
        float origAntiGravCoef;
        float origDrag;
        

        void Start()
        {
            owner = controller.owner;
            owner.GetComponent<FishermanSkillObjectTracker>().deployedBombs.Add(this);
            body = controller.rigidbody;
            origAntiGravCoef = antiGrav.antiGravityCoefficient;
            origDrag = body.drag;
            //stickComponent.stickEvent.AddListener(UnStickFromHook);
            stickComponent.stickEvent.AddListener(ResetDragAndGrav);
        }

        public void DisableAllColliders()
        {
            foreach (var collider in bombColliders)  collider.enabled = false; 
        }
        public void EnableAllColliders()
        {
            foreach(var collider in bombColliders) collider.enabled = true; 
        }

        public IEnumerator ResetStickyComponent()
        {
            yield return new WaitForSeconds(0.05f);
            EnableAllColliders();
            yield return new WaitForSeconds(0.1f);
            stickComponent.enabled = true;
            yield return new WaitForSeconds(1f);
            ResetDragAndGrav();
        }
        void ResetDragAndGrav()
        {
            antiGrav.antiGravityCoefficient = origAntiGravCoef;
            body.drag = origDrag;
            body.angularDrag = 3;
        }

        void FixedUpdate()
        {
            lastVelocity = body.velocity;
        }

        public void HookAllTethers()
        {
            moddedDamageComp.Remove(DamageTypes.FishermanTether);

            foreach (var target in beamController.previousTargets)
            {
                if(!target) continue;
                FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(
                    owner, gameObject,
                    damageComponent.damage,
                    transform.position,
                    target.body.mainHurtBox
                );
                if (target.body.HasBuff(FishermanBuffs.hookTetherDebuff))
                {
                    target.body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                }
            }
            //do explosion
            Log.Debug("Hooking");

            explosionComponent.blastDamageCoefficient = 1;
            explosionComponent.falloffModel = BlastAttack.FalloffModel.None;
            explosionComponent.explodeOnLifeTimeExpiration = false;
            explosionComponent.lifetime = 2f;
            explosionComponent.timerAfterImpact = false;
            explosionComponent.childrenDamageCoefficient = 1;
            explosionComponent.childrenProjectilePrefab = FishermanAssets.floatingBombletPrefab;
            explosionComponent.childrenCount = 1;

            //explosionComponent.lifetimeAfterImpact = 1f;
            //explosionComponent.destroyOnEnemy = false;
            //RoR2.Projectile.ProjectileManager.instance.FireProjectile(FishermanAssets.floatingBombletPrefab, transform.position, transform.rotation, owner, 100, 0, false);

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
            //FishermanSurvivor.SetDeployedHookBomb(null);
        }


    }
}
