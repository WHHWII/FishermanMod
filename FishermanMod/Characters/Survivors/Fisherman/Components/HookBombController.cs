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
using UnityEngine.Networking;

namespace FishermanMod.Survivors.Fisherman.Components
{
    //TODO: Tether visual components are not properly getting cleaned up
    //TODO: Recall hook bomb is throwing an error on activation
    // NETWORKING: like the hook, the bomb is a server-simulated projectile. Tethering, reeling enemies in and the explosion are server-only;
    // clients get the bomb's transform from the projectile's network transform and the tether lines through RpcAddTetherVisual.
    public class HookBombController : NetworkBehaviour
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
        public SphereCollider[] bombColliders;
        public bool wasStuckByHook;
        SkillObjectTracker tracker;
        //public ProjectileExplosion explosionComponent;
        float origAntiGravCoef;
        float origDrag;
        bool triggerized;
        public int stocksConsumed = 1;


        void Start()
        {
            owner = controller.owner;
            if (!owner)
            {
                if (NetworkServer.active) Destroy(gameObject);
                enabled = false;
                return;
            }
            tracker = owner.GetComponent<SkillObjectTracker>();
            tracker.deployedBombs.Add(this);
            tracker.deployedBomblets.Clear();

            // NETWORKING: ThrowBomb used to write this onto the prefab, which only worked on the machine that fired it.
            // The authority now sends the count to the server first (SkillObjectTracker.SetPendingHookBombStocksAuthority),
            // and only the server needs it since HookAllTethers runs there.
            if (NetworkServer.active)
            {
                stocksConsumed = tracker.pendingHookBombStocks;
                tracker.pendingHookBombStocks = 1;
            }

            body = controller.rigidbody;
            origAntiGravCoef = antiGrav.antiGravityCoefficient;
            origDrag = body.drag;
            //stickComponent.stickEvent.AddListener(UnStickFromHook);
            stickComponent.stickEvent.AddListener(ResetDragAndGrav);
            stickComponent.stickEvent.AddListener(TriggerizeAllColliders);

            damageComponent.damageType.AddModdedDamageType(DamageTypes.FishermanTether);

            // NETWORKING: only the Fisherman who threw it needs the HUD indicator
            if (Util.HasEffectiveAuthority(owner))
            {
                GameObject hi = Instantiate(FishermanAssets.bombIndicator, transform);
                hi.GetComponent<PositionIndicator>().targetTransform = transform;
                hi.transform.position = Vector3.zero;
            }
        }

        // NETWORKING: the tether debuff is applied on the server (DamageTypes.FishermanTether), but the chain line is a purely local visual,
        // so the server tells every client to draw it. The host receives this too, as its own client.
        [Server]
        public void AddTetherVisual(GameObject victim)
        {
            RpcAddTetherVisual(victim);
        }

        [ClientRpc]
        void RpcAddTetherVisual(GameObject victim)
        {
            if (!victim) return;
            foreach (HookBombTetherVisual existing in victim.GetComponents<HookBombTetherVisual>())
            {
                if (existing.lineTerminationObject == gameObject) return;
            }
            HookBombTetherVisual tetherVisual = victim.AddComponent<HookBombTetherVisual>();
            tetherVisual.lineTerminationObject = gameObject;
        }

        public void DisableAllColliders()
        {
            foreach (var collider in bombColliders) collider.enabled = false;
        }
        public void TriggerizeAllColliders()
        {
            triggerized = true;
            foreach (var collider in bombColliders) collider.isTrigger = true;
        }
        public void DeTriggerizeAllColliders()
        {
            triggerized = false;
            foreach (var collider in bombColliders) collider.isTrigger = false;
        }
        public void EnableAllColliders()
        {
            foreach (var collider in bombColliders) collider.enabled = true;

        }

        public IEnumerator ResetPhysics()
        {
            yield return new WaitForSeconds(0.05f);
            EnableAllColliders();
            yield return new WaitForSeconds(0.1f);
            stickComponent.enabled = true;
            stickComponent.UpdateSticking();
            yield return new WaitForSeconds(1f);
            ResetDragAndGrav();


        }
        void ResetDragAndGrav()
        {

            antiGrav.enabled = true;
            antiGrav.antiGravityCoefficient = origAntiGravCoef;
            body.detectCollisions = true;
            body.drag = origDrag;
            body.angularDrag = 3;
            StartCoroutine(EnableRBcol());
        }

        IEnumerator EnableRBcol()
        {
            yield return new WaitForFixedUpdate();
            body.detectCollisions = true;
        }

        void FixedUpdate()
        {
            lastVelocity = body.velocity;
            if (!body.detectCollisions) body.detectCollisions = true; // TODO not this.
            if (triggerized && !stickComponent.stuck || !stickComponent.stuckTransform)
            {
                DeTriggerizeAllColliders();
            }
        }

        [Server]
        public void HookAllTethers()
        {
            damageComponent.damageType.RemoveModdedDamageType(DamageTypes.FishermanTether);

            foreach (var target in beamController.previousTargets)
            {
                if (!target || !target.body) continue;
                if (target.body.HasBuff(FishermanBuffs.hookTetherDebuff))
                {
                    FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(
                        owner, gameObject,
                        transform.position,
                        target.body.mainHurtBox,
                        tracker.characterBody.baseDamage * FishermanSurvivor.GetBodyMass(target.body) * FishermanStaticValues.thrownEnemyCoef,
                        false
                    );

                    target.body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                }
            }
            //do explosion
            //Log.Debug("Hooking");

            explosionComponent.blastDamageCoefficient = 1;
            explosionComponent.falloffModel = BlastAttack.FalloffModel.None;
            explosionComponent.lifetime = 2f;
            explosionComponent.timerAfterImpact = false;
            explosionComponent.childrenDamageCoefficient = 1;
            explosionComponent.childrenProjectilePrefab = FishermanAssets.floatingBombletPrefab;
            explosionComponent.childrenCount = stocksConsumed;
            if (stocksConsumed > 1)
            {
                explosionComponent.minPitchDegrees -= stocksConsumed * 10f;
                explosionComponent.rangePitchDegrees += stocksConsumed * 10f;
                explosionComponent.minRollDegrees -= stocksConsumed * 10f;
                explosionComponent.rangeRollDegrees += stocksConsumed * 10f;
                explosionComponent.minYawDegrees -= stocksConsumed * 10f;
                explosionComponent.rangeYawDegrees += stocksConsumed * 10f;
            }

            //explosionComponent.lifetimeAfterImpact = 1f;
            //explosionComponent.destroyOnEnemy = false;
            //RoR2.Projectile.ProjectileManager.instance.FireProjectile(FishermanAssets.floatingBombletPrefab, transform.position, transform.rotation, owner, 100, 0, false);

            //Destroy(gameObject);

        }

        void OnDestroy()
        {
            if (!tracker) return;
            tracker.deployedBombs.Remove(this);
            // NETWORKING: removing the tether debuff is server-only
            if (NetworkServer.active && beamController) tracker.CleanupHookBombTethers(beamController.previousTargets);
            //FishermanSurvivor.SetDeployedHookBomb(null);
        }


    }
}
